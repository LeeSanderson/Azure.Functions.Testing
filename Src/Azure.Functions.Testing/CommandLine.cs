using System.Text.RegularExpressions;

namespace Azure.Functions.Testing;

public static class CommandLine
{
    public const string EndOfOptionsKey = "--";
    private static readonly Regex ArgParser = new(@"^(?<rawKey>((-|--|\/)(?<key>[^-\/\s:=\-+]+)))((?<bool>[+-])|((?<inline>([=:]))(?<value>\S+)))?$");

    public static Option[] Parse(string[] args)
    {
        if (args.Length == 0)
        {
            return Array.Empty<Option>();
        }

        return ParseToList(args).ToArray();
    }

    private static List<Option> ParseToList(IEnumerable<string> args)
    {
        var options = new List<Option>();
        OptionBuilder? current = null;
        foreach (var arg in args)
        {
            if (arg == EndOfOptionsKey)
            {
                options.AddIfNotNull(current);
                current = null;
                continue;
            }

            if (IsSwitchArg(arg))
            {
                // End of previous option... start a new one
                options.AddIfNotNull(current);
                current = null;
            }

            current = current == null ? ParseOption(arg) : current.WithOptionalValue(arg);
        }

        options.AddIfNotNull(current);
        return options;
    }

    public static string[] ReverseParse(params Option[] options)
    {
        if (options.Length == 0)
        {
            return Array.Empty<string>();
        }

        var cmdArgs = new List<string>();
        var lastIndex = options.Length - 1;
        for (var currentIndex = 0; currentIndex <= lastIndex; currentIndex++)
        {
            var option = options[currentIndex];
            var arg = option.RawKey;
            if (IsBoolKey(arg))
            {
                cmdArgs.Add(arg);
            }
            else if (IsInlineValueKey(arg))
            {
                arg += option.Value!;
                cmdArgs.Add(arg);
            }
            else
            {
                cmdArgs.Add(arg);
                foreach (var value in option.Values)
                {
                    cmdArgs.Add(value);
                }
            }

            if (currentIndex < lastIndex && !IsSwitchArg(arg))
            {
                cmdArgs.Add(EndOfOptionsKey);
            }
        }

        return cmdArgs.ToArray();
    }

    private static bool IsBoolKey(string arg) => arg.EndsWith("-") || arg.EndsWith("+");

    private static bool IsInlineValueKey(string arg) => arg.EndsWith(":") || arg.EndsWith("=");

    private static bool IsSwitchArg(string arg) => ArgParser.Match(arg).Success;

    private static void AddIfNotNull(this ICollection<Option> options, OptionBuilder? current)
    {
        if (current != null)
        {
            options.Add(current.ToOption());
        }
    }

    private static OptionBuilder ParseOption(string arg)
    {
        var argMatches = ArgParser.Match(arg);
        if (argMatches.Success)
        {
            var rawKey = argMatches.Groups["rawKey"].Value!;
            var key = argMatches.Groups["key"].Value!;
            var boolMarker = argMatches.Groups["bool"].Value!;
            var inlineValueMarker = argMatches.Groups["inline"].Value!;
            string? value = null;
            if (boolMarker.Length > 0)
            {
                value = boolMarker == "+" ? bool.TrueString : bool.FalseString;
                rawKey += boolMarker;
            }
            else if (inlineValueMarker.Length > 0)
            {
                value = argMatches.Groups["value"].Value!;
                rawKey += inlineValueMarker;
            }

            return new OptionBuilder(key, rawKey).WithOptionalValue(value);
        }

        // Assume arg is a command!
        return new OptionBuilder(arg, arg);
    }

    private class OptionBuilder
    {
        public OptionBuilder(string key, string rawKey)
        {
            RawKey = rawKey;
            Key = key;
        }

        private string RawKey { get; }
        private string Key { get; }
        private List<string> Values { get; } = new();

        public OptionBuilder WithOptionalValue(string? value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                Values.Add(value!);
            }

            return this;
        }

        public Option ToOption() => new(Key, RawKey, Values.ToArray());
    }
}

public class Option
{
    public Option(string key, string rawKey, params string[] values)
    {
        RawKey = rawKey;
        Key = key;
        Values = values;
    }

    public string RawKey { get; }
    public string Key { get; }
    public string[] Values { get; }
    public string? Value => Values.Length > 0 ? Values[0] : null;

    public bool IsExplicitBoolOption => Value == bool.TrueString || Value == bool.FalseString;
}
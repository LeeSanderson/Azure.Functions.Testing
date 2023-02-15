namespace Azure.Functions.Testing.Tests
{
    public static class StringExtensions
    {
        public static string[] AsCmdArgs(this string? args) => 
            args.ReplaceWithDoubleQuotes().SplitOnWhitespace().ToArray();

        private static string? ReplaceWithDoubleQuotes(this string? args) => 
            args?.Replace('\'', '"');

        public static IEnumerable<string> SplitOnWhitespace(this string? value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return Array.Empty<string>();
            }

            var paramChars = value.ToCharArray();
            var inDoubleQuotes = false;
            for (var index = 0; index < paramChars.Length; index++)
            {
                if (paramChars[index] == '"')
                    inDoubleQuotes = !inDoubleQuotes;

                if (!inDoubleQuotes && paramChars[index] == ' ')
                    paramChars[index] = '\n';
            }

            return (new string(paramChars)).Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
        }
    }
}

using Azure.Functions.Testing.Cli.Interfaces;
using Fclp;
using Fclp.Internals;

namespace Azure.Functions.Testing.Cli.Actions;

abstract class BaseAction : IAction
{
    protected FluentCommandLineParser Parser { get; }

    public IEnumerable<ICommandLineOption> MatchedOptions { get; private set; }

    public IDictionary<string, string> TelemetryCommandEvents { get; }

    protected BaseAction()
    {
        Parser = new FluentCommandLineParser();
        TelemetryCommandEvents = new Dictionary<string, string>();
        MatchedOptions = new List<ICommandLineOption>();
    }

    public virtual ICommandLineParserResult ParseArgs(string[] args)
    {
        var parserResult = Parser.Parse(args);
        MatchedOptions = Parser.Options.Except(parserResult.UnMatchedOptions);
        return parserResult;
    }

    public void SetFlag<T>(string longOption, string description, Action<T> callback, bool isRequired = false)
    {
        var flag = Parser.Setup<T>(longOption).WithDescription(description).Callback(callback);
        if (isRequired)
        {
            flag.Required();
        }
    }

    public abstract Task RunAsync();
}

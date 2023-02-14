using System.Collections.Generic;
using System.Threading.Tasks;
using Fclp;
using Fclp.Internals;

namespace Azure.Functions.Testing.Cli.Interfaces
{
    internal interface IAction
    {
        IEnumerable<ICommandLineOption> MatchedOptions { get; }
        IDictionary<string, string> TelemetryCommandEvents { get; }
        ICommandLineParserResult ParseArgs(string[] args);
        Task RunAsync();
    }
}

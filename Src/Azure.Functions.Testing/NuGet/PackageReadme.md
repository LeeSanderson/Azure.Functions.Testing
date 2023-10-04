# Azure.Functions.Testing

[Azure.Functions.Testing](https://github.com/LeeSanderson/Azure.Functions.Testing) is an integration testing helper library for Azure Functions in the style of 
[WebApplicationFactory](https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests).

The package leverages the [Azure Function Core Tools](https://learn.microsoft.com/en-us/azure/azure-functions/functions-run-local) to configure an launch an Azure Functions
project and allow a Test project to make HTTP requests to the Function HTTP Trigger endpoints.

When running the Function via Azure Function Core Tools, additional arguments can be specified in the constructor of the `FunctionApplicationFactory`.
The [func start](https://learn.microsoft.com/en-us/azure/azure-functions/functions-core-tools-reference?tabs=v2#func-start) documentation defines a list of valid arguments.

The Azure.Functions.Testing NuGet package is distributed under a [MIT Licence](https://github.com/LeeSanderson/Azure.Functions.Testing/blob/main/LICENSE) 
allowing the package to be freely used in commercial applications.

## Example: Testing a Function app with FunctionApplicationFactory

```csharp
// The FunctionApplicationFactory need to know where to find the code of 
// the Function project.
// 
// FunctionLocator.FromProject looks for a solution in the parent folders
// then works down till it finds a matching child folder.
// As long as the functionProjectFolder is unique (and you are using a solution)
// this should work.
const string functionProjectFolder = "[Folder containing the function project e.g. Dotnet.Function.Demo]";
var locator = FunctionLocator.FromProject(functionProjectFolder)

// As an alternative you can use FunctionLocator.FromPath and 
// specify the full path to the Function project.
// Or, implement IFunctionLocator yourself
// ----------------------------------------------------------------------


// Once we have the Function project code, we can create a factory.
// Tests will be more efficient if this is done once.
// There are different ways to do this depending on the testing framework you are using
// e.g. CollectionDefinition in xUnit (https://xunit.net/docs/shared-context),
// SetUpFixture in NUnit 
// (https://docs.nunit.org/articles/nunit/writing-tests/attributes/setupfixture.html), 
// AssemblyInitialize/AssemblyCleanup in MSTest etc.
using var factory = new FunctionApplicationFactory(locator)

// Or, FunctionApplicationFactory(locator, additionalArgs)
// to pass additional arguments to the `func start`
// ----------------------------------------------------------------------

// Adjust the start up delay, depending on build time of Function project
factory.StartupDelay = TimeSpan.FromSeconds(5); 
// ----------------------------------------------------------------------


// Create the pre-configured client from the factory...
using var client = await factory.CreateClient();

// The client is configured with the correct BaseAddress
// so requests only need a relative directory
const string uri = "/a/relative/api";
var response = await client.GetAsync(uri);
response.StatusCode.Should().Be(HttpStatusCode.OK);

```

## Example: Capturing output from Azure Function Core Tools 

The `func` tools write output to the console. 
It can be useful to capture this output during test runs. 
However, some testing frameworks do not capture/output console messages by default.
A [TestOutputConsoleAdapter](https://github.com/LeeSanderson/Azure.Functions.Testing/blob/main/Tests/Xunit.Shared/TestOutputConsoleAdapter.cs) can be used in xUnit to provide this
kind of logging capability.

This can then be configured via injection of the xUnit `ITestOutputHelper` and setting of the 
output and error streams

```csharp
public HelloFeatureThatSharesFixture(ITestOutputHelper output)
{
    var adapter = new TestOutputConsoleAdapter(output)
    Console.SetOut(adapter);
    Console.SetError(adapter);
}
```

To get more detailed ouput the `verbose` and `debug` flags can be passed.

```csharp
using var factory = new FunctionApplicationFactory(locator, "--verbose", "--debug")
```

If you are not using a `local.settings.json` file or you are running the tests in a CI pipeline you may need to pass a language
argument to the `FunctionApplicationFactory` so the function runtime can determine the project. 

```csharp
using var factory = new FunctionApplicationFactory(locator, "--verbose", "--debug", "--csharp")
```

If you are using Azure DevOps pipelines for your CI pipelines the [FuncToolsInstaller@0](https://learn.microsoft.com/en-us/azure/devops/pipelines/tasks/reference/func-tools-installer-v0?view=azure-pipelines)
task can be used to install dependencies.

## Version History

| Version | Major Changes                                                                          |  
|---------|----------------------------------------------------------------------------------------| 
| 0.1.6   | Fixed intermittent bug where Stop method would error as the process had already exited |  
| 0.1.5   | Optimizations and performance fixes                                                    |  
| 0.1.4   | Fixed bug when using health check and function is not receiving requests               |  
| 0.1.3   | Added support for health check on startup                                              |  
| 0.1.2   | Added support for running in CI pipelines as well as locally                           |  
| 0.1.1   | Expose Start and Stop methods to allow initialization                                  |  
| 0.1.0   | Initial version                                                                        |  

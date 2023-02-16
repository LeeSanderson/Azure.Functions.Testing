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


## Version History

| Version | Major Changes |  
| --- | --- | 
| 0.1.0 | Initial version |  

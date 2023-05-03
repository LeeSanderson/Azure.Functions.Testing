using FluentAssertions;

namespace Azure.Functions.Testing.Tests
{
    public class FunctionApplicationFactoryShould
    {
        [Fact]
        public void FailToStartUpIfProjectFolderIsNotFound()
        {
            // ReSharper disable once ObjectCreationAsStatement
            Action createFactory = () => new FunctionApplicationFactory(
                FunctionLocator.FromProject("Non.Existent.Project"), "--verbose", "--debug");

            createFactory.Should().Throw<DirectoryNotFoundException>();
        }

        [Fact]
        public async Task FailToStartUpIfProjectFolderIsNotAFunction()
        {
            using var factory = new FunctionApplicationFactory(
                FunctionLocator.FromProject("XUnit.Shared"), "--verbose", "--debug");

            await factory.Invoking(f => f.Start()).Should().ThrowAsync<FunctionApplicationFactoryException>();
        }

        [Fact]
        public async Task FailToStartUpIfFunctionPathDoesNotContainFuncExecutable()
        {
            using var factory = new FunctionApplicationFactory(
                FunctionLocator.FromProject("XUnit.Shared"), "--verbose", "--debug");
            factory.FuncExecutablePath = "../func.exe";

            await factory
                .Invoking(f => f.Start())
                .Should()
                .ThrowAsync<FunctionApplicationFactoryException>();
        }

    }
}

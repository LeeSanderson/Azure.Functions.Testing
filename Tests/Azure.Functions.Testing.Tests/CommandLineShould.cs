using FluentAssertions;

namespace Azure.Functions.Testing.Tests
{
    public class CommandLineShould
    {
        [Theory]
        [MemberData(nameof(GetParseTestCases))]
        public void Parse(string args, Option[] options)
        {
            CommandLine.Parse(args.AsCmdArgs()).Should().BeEquivalentTo(options);
        }

        [Theory]
        [MemberData(nameof(GetParseTestCases))]
        public void ReverseParse(string args, Option[] options)
        {
            CommandLine.ReverseParse(options).Should().BeEquivalentTo(args.AsCmdArgs());
        }


        public static IEnumerable<object[]> GetParseTestCases => new[]
        {
            NewParseTestCase("-s", new Option("s", "-s")),
            NewParseTestCase("--s", new Option("s", "--s")),
            NewParseTestCase("/s", new Option("s", "/s")),
            NewParseTestCase("-s+", new Option("s", "-s+", "True")),
            NewParseTestCase("-s-", new Option("s", "-s-", "False")),
            NewParseTestCase("-s:foo", new Option("s", "-s:", "foo")),
            NewParseTestCase("-s=foo", new Option("s", "-s=", "foo")),
            NewParseTestCase("-s foo", new Option("s", "-s", "foo")),

            NewParseTestCase(
                "--files a.txt b.txt c.txt",
                new Option("files", "--files", "a.txt", "b.txt", "c.txt")),

            NewParseTestCase(
                "files a.txt b.txt c.txt",
                new Option("files", "files", "a.txt", "b.txt", "c.txt")),

            NewParseTestCase(
                "--files a.txt b.txt --another",
                new Option("files", "--files", "a.txt", "b.txt"),
                new Option("another", "--another")),

            NewParseTestCase(
                "files a.txt b.txt -- another",
                new Option("files", "files", "a.txt", "b.txt"),
                new Option("another", "another"))
        };

        private static object[] NewParseTestCase(string args, params Option[] options) => new object[] {args, options};
    }
}
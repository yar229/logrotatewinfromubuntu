using FluentAssertions;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace logrotate.Tests.Integration.GarbageTests.Wrappers;

internal class XRunner
{
    private readonly INewWaveTests _container;

    public XRunner(INewWaveTests container)
    {
        _container = container;
    }

    public List<XBaseFile> Files;

    public XConfig Config;

    public XStateFile State = new XStateFile();

    public XRunner Run()
    {
        _container.Output.WriteLine($"Config:\r\n {File.ReadAllText(Config.Filepath)}");
        _container.Output.WriteLine($"Files before run:\r\n {ListDirectory(TestHelpersGarbage.TestDirMy)}");

        _container.RunLogRotate("-s", State, Config);

        _container.Output.WriteLine($"Files after run:\r\n {ListDirectory(TestHelpersGarbage.TestDirMy)}");
        return this;
    }
    public XRunner Check()
    {
        foreach (var file in Files)
        {
            foreach (var postfix in file.ShouldBeList)
                file.Exists(postfix).Should().BeTrue($"{file.Type} must exists: {file.Filename}{postfix}");
            foreach (var postfix in file.ShouldNotBeList)
                file.Exists(postfix).Should().BeFalse($"{file.Type} must NOT exists: {file.Filename}{postfix}");

            if (file.ShouldContainList.Any())
            {
                file.Exists().Should().BeTrue($"{file.Type} must exists: {file.Filename}");

                var content = File.ReadAllText(file.Filepath);
                foreach (var str in file.ShouldContainList)
                    content.Should().Contain(str, $"{file.Type} {file.Filename} must contain '{str}'");
                foreach (var str in file.ShouldNotContainList)
                    content.Should().NotContain(str, $"{file.Type} {file.Filename} must NOT contain '{str}'");
            }
        }

        return this;
    }

    public XRunner RunAndCheck()
    {
        Run();
        Check();
        return this;
    }


    private static string ListDirectory(string path, int indentLevel = 0)
    {
        var sb = new StringBuilder();
        string indent = new string('\t', indentLevel);

        if (indentLevel > 0)
            sb.AppendLine($"{indent}{Path.GetFileName(path)}");

        foreach (var file in Directory.GetFiles(path))
            sb.AppendLine($"{indent}\t{Path.GetFileName(file)}");

        foreach (var dir in Directory.GetDirectories(path))
            sb.Append(ListDirectory(dir, indentLevel + 1));

        return sb.ToString();
    }
}

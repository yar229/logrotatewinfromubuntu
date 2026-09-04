using logrotate.Tests.Integration.NewWave.Base;
using logrotate.Tests.Integration.NewWave.Wrappers;
using Xunit;
using Xunit.Abstractions;
using Op = LogRotate.Consts.ConfigSectionDirectives;

namespace logrotate.Tests.Integration.NewWave;

[Trait("Category", "Integration")]
public class FirstLastActionTests : NewWaveIntegrationTestBase
{
    public FirstLastActionTests(ITestOutputHelper output)
        : base(output)
    {
    }

    [Fact]
    public void SimpleLogFileWithGlobalActions_ShoudBeExecuted()
    {
        var markerFirst = Runner.NewFile("marker-first.txt");
        var markerLast = Runner.NewFile("marker-last.txt");

        Runner
            .WithLog("log-a.log", l => l.Create()
                .ShouldNotBe()
                .ShouldBe(Ext(".1")))
            .WithFiles([markerFirst, markerLast], f => f
                .ShouldBe())
            .WithConfig(c => c
                .WithGlobalSection(s => s
                    .WithEcho(Op.FirstAction, markerFirst)
                    .WithEcho(Op.LastAction, markerLast))
                .WithSection(XPattern.AllLogs, s => s
                    .With(Op.Rotate, 2)
                    .With(Op.Monthly))
                .Create() )
        .RunAndCheck();
    }

    [Fact]
    public void SimpleLogFileWithLocalActions_ShoudBeExecuted()
    {
        var markerFirst = Runner.NewFile("marker-first.txt");
        var markerLast = Runner.NewFile("marker-last.txt");

        Runner
            .WithLog("log-a.log", l => l.Create()
                .ShouldNotBe()
                .ShouldBe(Ext(".1")))
            .WithFiles([markerFirst, markerLast], f => f
                .ShouldBe())
            .WithConfig(c => c
                .WithGlobalSection(s => s
                    .WithEcho(Op.FirstAction, markerFirst)
                    .WithEcho(Op.LastAction, markerLast))
                .WithSection(XPattern.AllLogs, s => s
                    .With(Op.Rotate, 2)
                    .With(Op.Monthly)
                    .WithEcho(Op.FirstAction, markerFirst)
                    .WithEcho(Op.LastAction, markerLast))
                .Create())
        .RunAndCheck();
    }
}

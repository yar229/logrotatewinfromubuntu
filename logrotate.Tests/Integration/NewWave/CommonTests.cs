using logrotate.Tests.Integration.NewWave.Base;
using logrotate.Tests.Integration.NewWave.Wrappers;
using Xunit;
using Xunit.Abstractions;
using Op = LogRotate.Consts.ConfigSectionDirectives;

namespace logrotate.Tests.Integration.NewWave;

[Trait("Category", "Integration")]
public class CommonTests : NewWaveIntegrationTestBase
{
    public CommonTests(ITestOutputHelper output)
        : base(output)
    {
    }

    [Fact]
    public void SimpleLogFile_ShoudBeRotated()
    {
        Runner
            .WithLog("log-a.log", s => s.Create()
                .ShouldNotBe()
                .ShouldBe(Ext(".1")) )
            .WithConfig(c => c
                .WithSection(XPattern.AllLogs, s => s
                    .With(Op.Rotate, 2)
                    .With(Op.Monthly))
                .Create() )
            .RunAndCheck();
    }

    [Fact]
    public void OneShould_And_OtherOneNot_WithDupsInSameConfigSection_BeRotated()
    {
        var logA = Runner.NewLog("log-a.log").Create();
        var logB = Runner.NewLog("log-b.log").Create();

        Runner
            .WithLog(logA, l => l
                .ShouldNotBe()
                .ShouldBe(Ext(".1")) )
            .WithLog(logB, l => l
                .ShouldBe()
                .ShouldNotBe(Ext(".1")) )
            .WithState(s => s
                .WithProcessed(logB)
                .Create())
            .WithConfig(c => c
                .WithSection([logA, logB, logA, logB], s => s
                    .With(Op.Rotate, 2)
                    .With(Op.Monthly))
                .Create() )
            .RunAndCheck();
    }

    [Fact(DisplayName = "simple log by wildcard should be rotated")]
    public void SimpleLogRotateByMask_ShoudBeRotated()
    {
        Runner
            .WithLog("log-a.log", l => l.Create()
                .ShouldNotBe()
                .ShouldBe(Ext(".1")) )
            .WithConfig(c => c
                .WithSection(XPattern.AllLogs, s => s
                    .With(Op.Rotate, 2)
                    .With(Op.Monthly))
                .Create() )
            .RunAndCheck();
    }

    [Fact(DisplayName = "minage should prevents rotation of files younger than specified days")]
    public void RotateLog_WithMinAge1AndNewFile_ShouldNotRotate()
    {
        Runner
            .WithLog(l => l
                .WithContent("Fresh log content")
                .Create()
                .ShouldBe("original file should remain")
                .ShouldNotBe(Ext(".1"), $"file younger than {Op.MinAge} should not be rotated"))
            .WithConfig(c => c
                .WithSection(XPattern.AllLogs, s => s
                    .With(Op.MinAge, 1)
                    .With(Op.Rotate, 3)
                    .With(Op.Create))
                .Create())
            .RunAndCheck();
    }
}

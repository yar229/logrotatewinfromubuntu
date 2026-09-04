using FluentAssertions;
using logrotate.Tests.Integration.GarbageTests.Wrappers;
using logrotate.Tests.Integration.NewWave.Base;
using LogRotate;
using System.CodeDom;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using Op = logrotate.Config.ConfigSectionDirectives;

namespace logrotate.Tests.Integration.GarbageTests;

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
                .ShouldBe(".1") )
            .WithConfig(c => c
                .WithSection([XPattern.AllLogs], s => s
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
                .ShouldBe(".1") )
            .WithLog(logB, l => l
                .ShouldBe()
                .ShouldNotBe(".1") )
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

    [Fact]
    public void SimpleLogRotateByMask_ShoudBeRotated()
    {
        Runner
            .WithLog("log-a.log", l => l.Create()
                .ShouldNotBe()
                .ShouldBe(".1") )
            .WithConfig(c => c
                .WithSection([XPattern.AllLogs], s => s
                    .With(Op.Rotate, 2)
                    .With(Op.Monthly))
                .Create() )
            .RunAndCheck();
    }
}

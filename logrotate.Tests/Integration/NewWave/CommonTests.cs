using FluentAssertions;
using logrotate.Tests.Integration.GarbageTests.Wrappers;
using System.CodeDom;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using Directives = logrotate.Config.ConfigSectionDirectives;

namespace logrotate.Tests.Integration.GarbageTests;

[Trait("Category", "Integration")]
public class CommonTests : IntegrationTestBase, INewWaveTests
{
    public ITestOutputHelper Output { get; private set; }

    public CommonTests(ITestOutputHelper output)
    {
        TestHelpersGarbage.CleanupTestDir(false);
        Output = output;
    }
    public override void Dispose()
    {
        TestHelpersGarbage.CleanupTestDir(false); //TODO: make true after refactoring
    }

    [Fact]
    public void SimpleLogFile_ShoudBeRotated()
    {
        new XRunner(this)
        {
            Files = [
                new XLogFile("a.log").Create()
                    .ShouldNotBe()
                    .ShouldBe(".1")
            ],
            Config = new XConfig()
                .WithSection([XPattern.AllLogs], s => s
                    .With(Directives.Rotate, 2)
                    .With(Directives.Monthly))
                .Create()
        }
        .RunAndCheck();
    }

    [Fact]
    public void OneShould_And_OtherOneNot_WithDupsInSameConfigSection_BeRotated()
    {
        var logA = new XLogFile("log-a.log").Create();
        var logB = new XLogFile("log-b.log").Create();

        new XRunner(this)
        {
            Files = [
                logA.ShouldNotBe()
                    .ShouldBe(".1"),
                logB.ShouldBe()
                    .ShouldNotBe(".1"),
            ],
            State = new XStateFile()
                .WithProcessed(logB)
                .Create(),
            Config = new XConfig()
                .WithSection([logA, logB, logA, logB], s => s
                    .With(Directives.Rotate, 2)
                    .With(Directives.Monthly))
                .Create()
        }
        .RunAndCheck();
    }

    [Fact]
    public void SimpleLogRotateByMask_ShoudBeRotated()
    {
        var log = new XLogFile("a.log").Create();

        new XRunner(this)
        {
            Files = [
                log.ShouldNotBe()
                   .ShouldBe(".1")
            ],
            Config = new XConfig()
                .WithSection([XPattern.AllLogs], s => s
                    .With(Directives.Rotate, 2)
                    .With(Directives.Monthly) )
                .Create()
        }
        .RunAndCheck();
    }
}

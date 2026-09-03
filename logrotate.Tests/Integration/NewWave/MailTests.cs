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
public class MailTests : IntegrationTestBase, INewWaveTests
{
    public ITestOutputHelper Output { get; private set; }

    public MailTests(ITestOutputHelper output)
    {
        TestHelpersGarbage.CleanupTestDir(false);
        Output = output;
    }
    public override void Dispose()
    {
        TestHelpersGarbage.CleanupTestDir(false); //TODO: make true after refactoring
    }

    [Fact]
    public void SimpleMailTest_ShouldBePassed()
    {
        var log = new XLogFile("log-a.log").Create();
        var markerMail = new XFile("marker-mail.txt");

        new XRunner(this)
        {
            Files = [
                log
                   .ShouldNotBe()
                   .ShouldBe(".1"),
                markerMail
                    .ShouldBe()
                    .ShouldContain($"{log}.1")
                    .ShouldNotContain(XPattern.AllLogs)
            ],
            Config = new XConfig()
                .WithSection([XPattern.AllLogs], s => s
                    .With(Directives.Rotate, 1)
                    .With(Directives.MailFirst)
                    .With(Directives.Mail, "yar229@home.loc")
                    .WithScript(Directives.MailCmd, $"echo mail file %1 for %3 >> {markerMail}"))
                .Create()
        }
        .WithForce()
        .RunAndCheck();
    }
}

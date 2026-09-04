using FluentAssertions;
using logrotate.Tests.Integration.GarbageTests.Wrappers;
using logrotate.Tests.Integration.NewWave.Base;
using LogRotate;
using System.CodeDom;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using Op = LogRotate.Consts.ConfigSectionDirectives;

namespace logrotate.Tests.Integration.GarbageTests;

[Trait("Category", "Integration")]
public class MailTests : NewWaveIntegrationTestBase
{
    public MailTests(ITestOutputHelper output)
        : base(output)
    {
    }

    private const string DefaultEmail = "yar229@home.loc";

    [Fact]
    public void SimpleMailWithInplaceCmdParams_ShouldBePassed()
    {
        var log = Runner.NewLog("log-a.log").Create();
        var markerMail = Runner.NewFile("marker-mail.txt");

        Runner
            .WithLog(log, l => l
                .ShouldNotBe()
                .ShouldBe(".1"))
            .WithFile(markerMail, l => l
                .ShouldBe()
                .ShouldContain(DefaultEmail)
                .ShouldContain($"{log}.1")
                .ShouldNotContain(XPattern.AllLogs))
            .WithConfig(c => c
                .WithSection(XPattern.AllLogs, s => s
                    .With(Op.Rotate, 1)
                    .With(Op.MailFirst)
                    .With(Op.Mail, DefaultEmail)
                    .WithScript(Op.MailCmd, $"echo mail file %1 for %3 >> {markerMail}"))
                .Create())
            .RunAndCheck();
    }

    [Fact]
    public void SimpleMailWithEnviromentCmdParams_ShouldBePassed()
    {
        var log = Runner.NewLog("log-a.log").Create();
        var markerMail = Runner.NewFile("marker-mail.txt");

        Runner
            .WithLog(log, l => l
                .ShouldNotBe()
                .ShouldBe(".1"))
            .WithFile(markerMail, l => l
                .ShouldBe()
                .ShouldContain(DefaultEmail)
                .ShouldContain($"{log}.1")
                .ShouldNotContain(XPattern.AllLogs))
            .WithConfig(c => c
                .WithSection(XPattern.AllLogs, s => s
                    .With(Op.Rotate, 1)
                    .With(Op.MailFirst)
                    .With(Op.Mail, DefaultEmail)
                    .WithScript(Op.MailCmd, $"echo mail file %LOGROTATE_LOG% for %LOGROTATE_MAILTO% >> {markerMail}"))
                .Create())
            .RunAndCheck();
    }
}

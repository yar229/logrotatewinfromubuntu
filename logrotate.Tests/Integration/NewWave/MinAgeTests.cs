using logrotate.Tests.Integration.NewWave.Base;
using logrotate.Tests.Integration.NewWave.Wrappers;
using Xunit;
using Xunit.Abstractions;
using Op = LogRotate.Consts.ConfigSectionDirectives;

namespace logrotate.Tests.Integration.NewWave;

[Trait("Category", "Integration")]
public class MinAgeTests : NewWaveIntegrationTestBase
{
    public MinAgeTests(ITestOutputHelper output)
        : base(output)
    {
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

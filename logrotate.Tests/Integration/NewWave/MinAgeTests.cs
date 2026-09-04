using FluentAssertions;
using logrotate.Tests.Integration.NewWave.Base;
using logrotate.Tests.Integration.NewWave.Wrappers;
using System;
using System.IO;
using Xunit;
using Op = LogRotate.Consts.ConfigSectionDirectives;

namespace logrotate.Tests.Integration.NewWave;

[Trait("Category", "Integration")]
public class MinAgeTests : NewWaveIntegrationTestBase
{
    public MinAgeTests(ITestOutputHelper output)
        : base(output)
    {
    }

    /// <summary>
    /// 
    /// </summary>
    /// <remarks>
    /// Logrotate only evaluates minage after a log file already qualifies for rotation based on a schedule (daily, weekly, monthly) or size limit (size).
    /// </remarks>
    [Fact(DisplayName = $"{Op.MinAge} should prevents rotation of files younger than specified days")]
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
                    .With(Op.Rotate, 3)
                    .With(Op.Monthly) // required to minage works
                    .With(Op.MinAge, 1)
                    .With(Op.Create))
                .Create())
            .RunAndCheck();
    }



    [Fact(DisplayName = $"{Op.MinAge} allows rotation of files older than specified days")]
    public void RotateLog_WithMinAge1AndOldFile_ShouldRotate()
    {
        var dateCreate = DateTime.Now.AddDays(-3);
        var dateModif = dateCreate.AddMinutes(1);

        Runner
            .WithLog(l => l
                .WithContent("Old log content")
                .WithCreationTime(dateCreate)
                .WithLastWriteTime(dateModif)
                .Create()
                .ShouldNotBe()
                .ShouldBe(Ext(".1"), $"file older than {Op.MinAge} should be rotated"))
            .WithConfig(c => c
                .WithSection(XPattern.AllLogs, s => s
                    .With(Op.Rotate, 3)
                    .With(Op.Monthly) // required to minage works
                    .With(Op.MinAge, 1)
                    )
                .Create())
            .RunAndCheck();
    }

    [Fact(DisplayName = $"{Op.MinAge} should works together with {Op.MinSize} rotation")]
    public void RotateLog_WithMinAgeAndMinSize_ShouldRespectBothConditions()
    {
        Runner
            .WithLog(l => l
                .WithContent(new string('X', 10000))  // logfile is large enought
                .Create()   // logfile is fresh, just created
                .ShouldBe()
                .ShouldNotBe(Ext(".1"), $"file should not rotate when younger than {Op.MinAge}, even if {Op.MinSize} is exceeded"))
            .WithConfig(c => c
                .WithSection(XPattern.AllLogs, s => s
                    .With(Op.Rotate, 3)
                    .With(Op.Daily) // required to minage works
                    .With(Op.MinAge, 1)
                    .With(Op.MinSize, 1024))
                .Create())
            .RunAndCheck();
    }
}

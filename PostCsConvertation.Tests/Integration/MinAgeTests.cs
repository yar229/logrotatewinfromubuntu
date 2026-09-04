using FluentAssertions;
using PostCsConvertation.Tests.Integration.Base;
using PostCsConvertation.Tests.Integration.Wrappers;
using System;
using System.IO;
using Xunit;
using Op = LogRotate.Consts.ConfigSectionDirectives;

namespace PostCsConvertation.Tests.Integration;


/// <summary>
/// 'minage' config directive tests
/// </summary>
/// <remarks>
/// Logrotate only evaluates minage after a log file already qualifies for rotation based on a schedule (daily, weekly, monthly) or size limit (size).
/// </remarks>
[Trait("Category", "Integration")]
public class MinAgeTests : NewWaveIntegrationTestBase
{
    public MinAgeTests(ITestOutputHelper output)
        : base(output)
    {
    }

    [Fact(DisplayName = $"{Op.MinAge} should prevents rotation of files younger than specified days")]
    public void RotateLog_WithMinAge1AndNewFile_ShouldNotRotate()
    {
        Runner
            .WithLog(l => l
                .Create() // logfile is fresh, just created
                .ShouldBe()
                .ShouldNotBe(Ext(".1"), $"logfile younger than {Op.MinAge} should not be rotated"))
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
        Runner
            .WithLog(l => l
                .WithContent("Old log content")
                .WithLastWriteTime(DateTime.Now.AddDays(-3))
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
                .ShouldBe("")
                .ShouldNotBe(Ext(".1"), $"file should not be rotated if younger than {Op.MinAge}, even if {Op.MinSize} is exceeded"))
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

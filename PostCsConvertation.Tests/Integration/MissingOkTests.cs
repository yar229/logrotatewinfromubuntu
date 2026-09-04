using FluentAssertions;
using PostCsConvertation.Tests.Integration.Base;
using PostCsConvertation.Tests.Integration.Wrappers;
using System;
using System.IO;
using System.Text;
using Xunit;
using Op = LogRotate.Consts.ConfigSectionDirectives;

namespace PostCsConvertation.Tests.Integration;


/// <summary>
/// 'missingok' config directive tests
/// </summary>
/// <remarks>
/// missingok by default is false
/// </remarks>
[Trait("Category", "Integration")]
public class MissingOkTests : NewWaveIntegrationTestBase
{
    public MissingOkTests(ITestOutputHelper output)
        : base(output)
    {
    }

    [Fact]
    public void RotateLog_WithMissingOkDefault_ShouldError()
    {
        var log = Runner.NewLog(); // deliberately don't create the logfile

        Runner
            .WithLog(log, l => l
                .ShouldNotBe()
                .ShouldNotBe(Ext(".1")))
            .WithConfig(c => c
                .WithSection(log, s => s
                    .With(Op.Rotate, 3)
                    .With(Op.Create))
                .Create())
            .RunAndCheck()
            .Should()
            .NotBe(0, $"absent {Op.MissingOk} behavior should fail on missing files");
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

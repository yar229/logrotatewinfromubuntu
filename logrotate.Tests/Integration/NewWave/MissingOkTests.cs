using FluentAssertions;
using logrotate.Tests.Integration.NewWave.Base;
using logrotate.Tests.Integration.NewWave.Wrappers;
using System;
using System.IO;
using System.Text;
using Xunit;
using Op = LogRotate.Consts.ConfigSectionDirectives;

namespace logrotate.Tests.Integration.NewWave;


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
                .ShouldBe()
                .ShouldNotBe(Ext(".1")))
            .WithConfig(c => c
                .WithSection(log, s => s
                    .With(Op.Rotate, 3)
                    .With(Op.Create))
                .Create())
            .RunAndCheck()
            .Should()
            .NotBe(0, $"absent {Op.MissingOk} behavior should fail on missing files");



    //    // Tests default behavior - missing files don't cause errors

    //    // Arrange
    //    string logFile = Path.Combine(TestDir, "nonexistent.log");
    //    // Deliberately don't create the file

    //    string stateFile = Path.Combine(TestDir, "state.txt");
    //    string configContent = $@"
    //""{logFile}"" {{
    //    rotate 3
    //    create
    //}}
    //";
    //    string configFile = TestHelpers.CreateTempConfigFile(configContent);

    //    try
    //    {
    //        // Act
    //        var exitCode = RunLogRotate("-s", stateFile, "-f", configFile);

    //        // Assert - Default behavior should not error on missing files
    //        exitCode.Should().Be(0, "default missingok behavior should not error on missing files");
    //    }
    //    finally
    //    {
    //        TestHelpers.CleanupPath(configFile);
    //    }
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

using FluentAssertions;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Xunit;

namespace LogRotateWin.LegacyTests;

/// <summary>
/// Port of shell tests 41-45 from _logrotate-3.22.0/test.
/// Script bodies are adapted to cmd syntax (see ShellTestBase). The reference
/// shells wrap the shared-script output in literal '"' chars; with cmd's
/// set /p quoting those are not reproduced, so the wrapped quotes are dropped
/// from the expected scriptout (cosmetic deviation only).
/// </summary>
public class Tests0041_0045 : ShellTestBase
{
    /// <summary>
    /// Test 41: no sharedscripts - prerotate/postrotate run per rotated file
    /// only. test.log rotates (5 bytes >= size 5), test2.log ("x\n") does not.
    /// </summary>
    [Fact]
    public void Test0041_PrePostRotatePerFileOnlyRotatedOnes()
    {
        Preptest("test.log", 1);
        WriteFile("test2.log", "x\n");
        GenConfig("test-config.41", Config41);
        Run("test-config.41");
        ExitCode.Should().Be(0);

        CheckOutput(
            OutputExpectation.Content("test.log", ""),
            OutputExpectation.Content("test.log.1", "zero"),
            OutputExpectation.Content("test2.log", "x"),
            OutputExpectation.Content("scriptout", "test.log;test.log;"));
    }

    /// <summary>
    /// Test 42: sharedscripts - prerotate/postrotate run exactly once for the
    /// whole pattern, not per file. Both files rotate.
    /// DEVIATION: the port passes the pattern to the script as $1, but cmd's
    /// %~nx expansion cannot derive the basename from it, so "test*.log" is
    /// hardcoded in the body (the reference derives it from $1).
    /// </summary>
    [Fact]
    public void Test0042_SharedScriptsRunOnce()
    {
        Preptest("test.log", 1);
        WriteFile("test2.log", "number2\n");
        GenConfig("test-config.42", Config42);
        Run("test-config.42");
        ExitCode.Should().Be(0);

        CheckOutput(
            OutputExpectation.Content("test.log", ""),
            OutputExpectation.Content("test.log.1", "zero"),
            OutputExpectation.Content("test2.log", ""),
            OutputExpectation.Content("test2.log.1", "number2"),
            OutputExpectation.Content("scriptout", "test*.log;test*.log;"));
    }

    /// <summary>
    /// Test 43: no sharedscripts - scripts run twice (once per file) when both
    /// files rotate; relative order of the two files is unspecified.
    /// </summary>
    [Fact]
    public void Test0043_PrePostRotateTwiceTwoFiles()
    {
        Preptest("test.log", 1);
        WriteFile("test2.log", "number2\n");
        GenConfig("test-config.43", Config43);
        Run("test-config.43");
        ExitCode.Should().Be(0);

        CheckOutput(
            OutputExpectation.Content("test.log", ""),
            OutputExpectation.Content("test.log.1", "zero"),
            OutputExpectation.Content("test2.log", ""),
            OutputExpectation.Content("test2.log.1", "number2"));

        string scriptout = ReadScriptOut();
        bool firstThenSecond = scriptout == "test.log;test.log;test2.log;test2.log;";
        bool secondThenFirst = scriptout == "test2.log;test2.log;test.log;test.log;";
        (firstThenSecond || secondThenFirst).Should().BeTrue(
            $"scriptout should list both files twice in either order, was: [{scriptout}]");
    }

    /// <summary>
    /// Test 44: no sharedscripts, one file missing. Rotation of the present
    /// file succeeds (scripts still run for it), the missing file is reported
    /// on stderr and logrotate exits nonzero.
    /// </summary>
    [Fact]
    public void Test0044_PrePostRunWhenSiblingFails()
    {
        Preptest("test.log", 1);
        GenConfig("test-config.44", Config44);
        Run("test-config.44");

        ExitCode.Should().NotBe(0);
        StdErr.Should().Contain("error: stat of");
        CheckOutput(
            OutputExpectation.Content("test.log", ""),
            OutputExpectation.Content("test.log.1", "zero"),
            OutputExpectation.Content("scriptout", "test.log;test.log;"));
    }

    /// <summary>
    /// Test 45: sharedscripts, one file missing. Nothing at all is rotated and
    /// neither script runs (scriptout stays empty); logrotate exits nonzero.
    /// </summary>
    [Fact]
    public void Test0045_SharedScriptsNotRunWhenSiblingFails()
    {
        Preptest("test.log", 1);
        WriteFile("scriptout", "");
        GenConfig("test-config.45", Config45);
        Run("test-config.45");

        ExitCode.Should().NotBe(0);
        StdErr.Should().Contain("error: stat of");
        CheckOutput(
            OutputExpectation.Content("test.log", "zero"),
            OutputExpectation.Content("test.log.1", "first"),
            OutputExpectation.Content("scriptout", ""));
    }

    private string ReadScriptOut()
        => NormalizeForAssert(File.ReadAllText(P("scriptout")));

    private static string NormalizeForAssert(string s)
        => (s ?? string.Empty).Replace("\r\n", "\n").TrimEnd('\n');

    // =====================================================================
    // configs (ported from test-config.N.in)
    // =====================================================================

    /// <summary>prerotate/postrotate body appending "&lt;name&gt;;" to scriptout.</summary>
    private static string RotateBody(string namePart)
        => string.Join("\n",
            "    prerotate",
            $"<nul set /p=\"{namePart};\">>scriptout",
            "exit /b 0",
            "    endscript",
            "    postrotate",
            $"<nul set /p=\"{namePart};\">>scriptout",
            "exit /b 0",
            "    endscript") + "\n";

    private static readonly string Config41 = """
        create

        "&DIR&/test*.log" {
            size 5
            rotate 1
            nosharedscripts
        """ + RotateBody("%~nx1") + """
            }
        """;

    private static readonly string Config42 = """
        create

        "&DIR&/test*.log" {
            size 5
            rotate 1
            sharedscripts
        """ + RotateBody("test*.log") + """
            }
        """;

    private static readonly string Config43 = Config41;

    private static readonly string Config44 = """
        create

        "&DIR&/test.log" "&DIR&/test2.log" {
            size 5
            rotate 1
            nosharedscripts
        """ + RotateBody("%~nx1") + """
            }
        """;

    private static readonly string Config45 = """
        create

        "&DIR&/test.log" "&DIR&/test2.log" {
            size 5
            rotate 1
            sharedscripts
        """ + RotateBody("test.log") + """
            }
        """;
}
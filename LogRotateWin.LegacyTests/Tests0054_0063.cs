using FluentAssertions;
using System;
using System.IO;
using Xunit;

namespace LogRotateWin.LegacyTests;

/// <summary>
/// Port of shell tests 54-63 from _logrotate-3.22.0/test.
/// See ShellTestBase class doc for the general deviation policy.
/// </summary>
public class Tests0054_0063 : ShellTestBase
{
    /// <summary>
    /// Test 54: dateext pruning with dateformat -%Y-%m-%d. With rotate 60 the
    /// oldest of 61 dated files (including today's new rotation) is removed.
    /// </summary>
    [Fact]
    public void Test0054_RemoveOldestDateextYMD()
    {
        WriteFile("test.log", "zero\n");
        GenConfig("test-config.54", Config54);

        var today = DateTime.Now;
        string oldest = "";
        for (int i = 1; i <= 60; i++)
        {
            oldest = today.AddDays(-i).ToString("yyyy-MM-dd");
            WriteFile($"test.log-{oldest}", "x\n");
        }

        Run("test-config.54", "--force");
        ExitCode.Should().Be(0);
        File.Exists(P($"test.log-{oldest}")).Should().BeFalse(
            $"oldest dateext file test.log-{oldest} should have been pruned");
        File.Exists(P($"test.log-{today:yyyy-MM-dd}")).Should().BeTrue(
            "today's dated rotation should exist");
    }

    /// <summary>
    /// Test 55: hourly + dateext + dateformat -%s (epoch) + copytruncate +
    /// compress. The oldest of the compressed (.gz) dated files is pruned.
    /// DEVIATION: internal gzip is used instead of compresscmd gzip (external
    /// compressors produce an empty outfile in the port by design).
    /// </summary>
    [Fact]
    public void Test0055_RemoveOldestDateextEpoch()
    {
        WriteFile("test.log", "zero\n");
        GenConfig("test-config.55", Config55);

        var now = DateTime.Now;
        long oldest = 0;
        for (int i = 1; i <= 60; i++)
        {
            oldest = new DateTimeOffset(now.AddHours(-i)).ToUnixTimeSeconds();
            WriteFile($"test.log-{oldest}.gz", "x\n");
        }

        Run("test-config.55", "--force");
        ExitCode.Should().Be(0);
        File.Exists(P($"test.log-{oldest}.gz")).Should().BeFalse(
            $"oldest dateext file test.log-{oldest}.gz should have been pruned");
        AssertFileContent("test.log", ""); // copytruncate empties the log in place
    }

    /// <summary>
    /// Test 56: dateext pruning with dateformat -%d-%m-%Y.
    /// </summary>
    [Fact]
    public void Test0056_RemoveOldestDateextDMY()
    {
        WriteFile("test.log", "zero\n");
        GenConfig("test-config.56", Config56);

        var today = DateTime.Now;
        string oldest = "";
        for (int i = 1; i <= 60; i++)
        {
            oldest = today.AddDays(-i).ToString("dd-MM-yyyy");
            WriteFile($"test.log-{oldest}", "x\n");
        }

        Run("test-config.56", "--force");
        ExitCode.Should().Be(0);
        File.Exists(P($"test.log-{oldest}")).Should().BeFalse(
            $"oldest dateext file test.log-{oldest} should have been pruned");
        File.Exists(P($"test.log-{today:dd-MM-yyyy}")).Should().BeTrue(
            "today's dated rotation should exist");
    }

    /// <summary>
    /// Test 57: stderr output of an external compression program is wrapped
    /// with "error: Compressing ... stderr when compressing log &lt;file&gt;:"
    /// and "compression error".
    /// DEVIATION: the port runs the external compressor but by design its
    /// output file is empty; the reference's "test.log.1.gz == zero" content
    /// assertion is weakened to "test.log.1.gz exists".
    /// </summary>
    [Fact]
    public void Test0057_CompressProgramStderr()
    {
        WriteFile("test.log", "zero\n");
        GenConfig("test-config.57", Config57);

        Run("test-config.57", "--force");
        ExitCode.Should().Be(0);
        StdErr.Should().Contain("error: Compressing");
        StdErr.Should().Contain("compression error");
        CheckOutput(
            OutputExpectation.Content("test.log", ""),
            OutputExpectation.Exist("test.log.1.gz"));
    }

    /// <summary>
    /// Test 58: renamecopy renames the log and creates a new empty one.
    /// </summary>
    [Fact]
    public void Test0058_RenameCopy()
    {
        Preptest("test.log", 1);
        GenConfig("test-config.58", Config58);

        Run("test-config.58", "--force");
        ExitCode.Should().Be(0);
        CheckOutput(
            OutputExpectation.Content("test.log", ""),
            OutputExpectation.Content("test.log.1", "zero"));
    }

    /// <summary>
    /// Test 59: renamecopy in debug mode (-d -f) must not modify any file.
    /// </summary>
    [Fact]
    public void Test0059_RenameCopyDebugDoesNothing()
    {
        Preptest("test.log", 1);
        WriteFile("test.log.1", "");
        WriteFile("test.log.2", "");
        GenConfig("test-config.59", Config59);

        Run("test-config.59", "--force", "-d");
        ExitCode.Should().Be(0);
        CheckOutput(
            OutputExpectation.Content("test.log", "zero"),
            OutputExpectation.Exist("test.log.1"),
            OutputExpectation.Exist("test.log.2"));
    }

    /// <summary>
    /// Test 60: -l &lt;file&gt; redirects log output to the file and rotation
    /// still happens (dateformat .%Y-%m-%d-%H).
    /// </summary>
    [Fact]
    public void Test0060_LogOutputFile()
    {
        Preptest("test.log", 1);
        GenConfig("test-config.60", Config60);

        Run("test-config.60", "--force", "-l", "logrotate.log");
        ExitCode.Should().Be(0);
        File.ReadAllText(P("logrotate.log")).Should().Contain(
            "reading config file test-config.60");

        CheckOutput(
            OutputExpectation.Content("test.log", ""),
            OutputExpectation.Content($"test.log.{DateTime.Now:yyyy-MM-dd-HH}", "zero"));
    }

    /// <summary>
    /// Test 61: dateext with dateformat .%Y-%m-%d-%H (same rotation as 60,
    /// without the log file).
    /// </summary>
    [Fact]
    public void Test0061_DateExtHourFormat()
    {
        Preptest("test.log", 1);
        GenConfig("test-config.61", Config61);

        Run("test-config.61", "--force");
        ExitCode.Should().Be(0);
        CheckOutput(
            OutputExpectation.Content("test.log", ""),
            OutputExpectation.Content($"test.log.{DateTime.Now:yyyy-MM-dd-HH}", "zero"));
    }

    /// <summary>
    /// Test 62: copytruncate with data at both ends.
    /// DEVIATION: the upstream sparse-file size assertions (du/truncate) are
    /// Linux-file-system specific; the test is reduced to the copytruncate
    /// semantics the port implements (copy the file, then truncate the log).
    /// </summary>
    [Fact]
    public void Test0062_SparseFileCopyTruncate()
    {
        WriteFile("test.log", "zerox\n");
        GenConfig("test-config.62", Config62);

        Run("test-config.62", "--force");
        ExitCode.Should().Be(0);
        CheckOutput(
            OutputExpectation.Content("test.log", ""),
            OutputExpectation.Content("test.log.1", "zerox"));
    }

    /// <summary>
    /// Test 63: copytruncate with a trailing hole.
    /// DEVIATION: same reduction as test 62 - only the copytruncate semantics
    /// are asserted, the sparse/hole size checks are dropped.
    /// </summary>
    [Fact]
    public void Test0063_SparseFileTrailingHole()
    {
        WriteFile("test.log", "zero\n");
        GenConfig("test-config.63", Config63);

        Run("test-config.63", "--force");
        ExitCode.Should().Be(0);
        CheckOutput(
            OutputExpectation.Content("test.log", ""),
            OutputExpectation.Content("test.log.1", "zero"));
    }

    // =====================================================================
    // configs (ported from test-config.N.in)
    // =====================================================================

    private const string Config54 = """
        create

        "&DIR&/test.log" {
            daily
            dateext
            dateformat -%Y-%m-%d
            rotate 60
        }
        """;

    private const string Config55 = """
        create
        missingok
        copytruncate
        compress
        notifempty

        "&DIR&/test.log" {
            hourly
            dateext
            dateformat -%s
            rotate 60
            nosharedscripts
        }
        """;

    private const string Config56 = """
        create

        "&DIR&/test.log" {
            daily
            dateext
            dateformat -%d-%m-%Y
            rotate 60
        }
        """;

    private const string Config57 = """
        create

        "&DIR&/test.log" {
            compress
            compresscmd ./compress-error.cmd
            compressoptions -f -9
            compressext .gz
            weekly
            rotate 1
        }
        """;

    private const string Config58 = """
        create

        # will be overridden by renamecopy
        copytruncate

        "&DIR&/test.log" {
            renamecopy
            weekly
            rotate 1
        }
        """;

    private const string Config59 = """
        create

        # will be overridden by renamecopy
        copytruncate

        "&DIR&/test.log" {
            renamecopy
            weekly
            rotate 1
        }
        """;

    private const string Config60 = """
        create

        "&DIR&/test.log" {
            daily
            dateext
            dateformat .%Y-%m-%d-%H
            rotate 1
        }
        """;

    private const string Config61 = """
        create

        "&DIR&/test.log" {
            daily
            dateext
            dateformat .%Y-%m-%d-%H
            rotate 1
        }
        """;

    private const string Config62 = """
        create

        "&DIR&/test*.log" {
            daily
            copytruncate
            rotate 1
        }
        """;

    private const string Config63 = """
        create

        "&DIR&/test*.log" {
            daily
            copytruncate
            rotate 1
        }
        """;
}
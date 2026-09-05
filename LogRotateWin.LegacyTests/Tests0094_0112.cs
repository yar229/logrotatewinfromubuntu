using FluentAssertions;
using System;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;

namespace LogRotateWin.LegacyTests;

/// <summary>
/// Port of the last shell tests (94, 100-112) from _logrotate-3.22.0/test.
/// Tests 95-99 do not exist upstream. See ShellTestBase class doc for the
/// general deviation policy.
/// </summary>
public class Tests0094_0112 : ShellTestBase
{
    /// <summary>
    /// Test 94: 'createolddir' must not create the old directory with mode
    /// -1 (the reference greps the verbose log for the broken mode string;
    /// LogRotateWin never prints it).
    /// </summary>
    [Fact]
    public void Test0094_CreateOldDirHasSaneMode()
    {
        Preptest("test.log", 1);
        GenConfig("test-config.94", Config94);

        Run("test-config.94", "--force");
        ExitCode.Should().Be(0);
        Log.Should().NotContain("mode = 037777777777");
        File.Exists(P("testdir/test.log.1")).Should().BeTrue("the rotated log must land in the old dir");
        CheckOutput(
            OutputExpectation.Content("test.log", ""),
            OutputExpectation.Content("testdir/test.log.1", "zero"));
    }

    /// <summary>
    /// Test 100: 'addextension .newext' appends the extension after the
    /// rotation number.
    /// </summary>
    [Fact]
    public void Test0100_AddExtensionAppended()
    {
        Preptest("test.log", 1);
        GenConfig("test-config.100", Config100);

        Run("test-config.100", "--force");
        ExitCode.Should().Be(0);
        CheckOutput(
            OutputExpectation.Content("test.log", ""),
            OutputExpectation.Content("test.log.1.newext", "zero"));
    }

    /// <summary>
    /// Test 101: 'addextension .log' moves the extension after the number.
    /// </summary>
    [Fact]
    public void Test0101_AddExtensionMovesAfterNumber()
    {
        Preptest("test.log", 1);
        GenConfig("test-config.101", Config101);

        Run("test-config.101", "--force");
        ExitCode.Should().Be(0);
        CheckOutput(
            OutputExpectation.Content("test.log", ""),
            OutputExpectation.Content("test.1.log", "zero"));
    }

    /// <summary>
    /// Test 102: a config file with binary content must be rejected without
    /// rotating the log.
    /// </summary>
    [Fact]
    public void Test0102_BinaryConfigRejected()
    {
        Preptest("test.log", 1);

        string resolved = Config102.Replace("&DIR&", TestDir, StringComparison.Ordinal);
        string prefix = "\u007fELF\n\n";
        File.WriteAllBytes(P("test-config.102"), Encoding.UTF8.GetBytes(prefix + resolved));

        Run("test-config.102", "--force");
        ExitCode.Should().NotBe(0, "a config with binary content must be an error");
        CheckOutput(
            OutputExpectation.Content("test.log", "zero"),
            OutputExpectation.Content("test.log.1", "first"));
    }

    /// <summary>
    /// Test 103: a config full of garbage keywords must be rejected without
    /// rotating the log.
    /// </summary>
    [Fact]
    public void Test0103_GarbageConfigRejected()
    {
        Preptest("test.log", 1);
        GenConfig("test-config.103", Config103);

        Run("test-config.103", "--force");
        ExitCode.Should().NotBe(0, "a config with garbage must be an error");
        CheckOutput(
            OutputExpectation.Content("test.log", "zero"),
            OutputExpectation.Content("test.log.1", "first"));
    }

    /// <summary>
    /// Test 104: an unknown keyword inside a log block is ignored and the
    /// other logs still get rotated.
    /// DEVIATION: 'create' was added because LogRotateWin does not create a
    /// new log by default (the reference does); the reference asserts the
    /// re-created test.log exists.
    /// </summary>
    [Fact]
    public void Test0104_UnknownKeywordIgnored()
    {
        Preptest("test1.log", 1);
        Preptest("test2.log", 1);
        GenConfig("test-config.104", Config104);

        Run("test-config.104", "--force");
        ExitCode.Should().Be(0);
        CheckOutput(
            OutputExpectation.Content("test1.log", ""),
            OutputExpectation.Content("test1.log.1", "zero"),
            OutputExpectation.Content("test2.log", ""),
            OutputExpectation.Content("test2.log.1", "zero"));
    }

    /// <summary>
    /// Test 105: a garbage keyword makes logrotate bail out on that log but
    /// the remaining logs are still rotated.
    /// DEVIATION: 'create' was added (see test 104 doc).
    /// </summary>
    [Fact]
    public void Test0105_GarbageKeywordBailsOutForThatLog()
    {
        Preptest("test1.log", 1);
        Preptest("test2.log", 1);
        GenConfig("test-config.105", Config105);

        Run("test-config.105", "--force");
        ExitCode.Should().NotBe(0, "a garbage keyword must be an error");
        CheckOutput(
            OutputExpectation.Content("test1.log", "zero"),
            OutputExpectation.Content("test1.log.1", "first"),
            OutputExpectation.Content("test2.log", ""),
            OutputExpectation.Content("test2.log.1", "zero"));
    }

    /// <summary>
    /// Test 106: '~' in the include path and in 'olddir' is expanded to %HOME%.
    /// </summary>
    [Fact]
    public void Test0106_TildeInIncludeAndOldDir()
    {
        Preptest("test.log", 1);
        Directory.CreateDirectory(P("homedir/includedir"));
        string included = Config106Included.Replace("&DIR&", TestDir, StringComparison.Ordinal);
        File.WriteAllText(P("homedir/includedir/conf"), included);
        GenConfig("test-config.106", Config106);

        Environment.SetEnvironmentVariable("HOME", P("homedir"));
        try
        {
            Run("test-config.106", "--force");
        }
        finally
        {
            Environment.SetEnvironmentVariable("HOME", null);
        }

        ExitCode.Should().Be(0);
        File.Exists(P("homedir/old/test.log.1")).Should().BeTrue("olddir must expand ~ under HOME");
        CheckOutput(
            OutputExpectation.Content("test.log", ""),
            OutputExpectation.Content("homedir/old/test.log.1", "zero"));
    }

    /// <summary>
    /// Test 107: with 'ignoreduplicates' the log matched by two blocks is
    /// rotated only once.
    /// </summary>
    [Fact]
    public void Test0107_IgnoreDuplicatesRotatesOnce()
    {
        Preptest("test.log", 1);
        Preptest("zzzz.log", 1);
        GenConfig("test-config.107", Config107);

        Run("test-config.107", "--force");
        ExitCode.Should().Be(0);
        CheckOutput(
            OutputExpectation.Content("test.log.1", "zero"),
            OutputExpectation.Content("zzzz.log.1", "zero"));
    }

    /// <summary>
    /// Test 108: compressing a log with an old modification time must keep
    /// that timestamp on the compressed backup.
    /// DEVIATION: the reference checks the raw atime/mtime epoch inside the
    /// gzip; on Windows the equivalent assertion is that the .gz file keeps
    /// the source log's last-write time (the port copies it over).
    /// </summary>
    [Fact]
    public void Test0108_CompressKeepsSourceTimestamp()
    {
        Preptest("test.log", 0);
        GenConfig("test-config.108", Config108);

        var old = new DateTime(2000, 1, 1, 0, 0, 0);
        File.SetLastWriteTime(P("test.log"), old);

        Run("test-config.108", "--force");
        ExitCode.Should().Be(0);
        CheckOutput(
            OutputExpectation.Content("test.log", ""),
            OutputExpectation.Content("test.log.1.gz", "zero", compressed: true));
        File.GetLastWriteTime(P("test.log.1.gz")).Should().Be(old);
    }

    /// <summary>
    /// Test 109: script failures - prerotate / firstaction / shared prerotate
    /// failures abort the rotation, postrotate / shared postrotate / lastaction
    /// failures still rotate; exit code is non-zero.
    /// DEVIATION: the failing script is 'exit /b 1' instead of '/usr/bin/false'.
    /// </summary>
    [Fact]
    public void Test0109_ScriptFailures()
    {
        foreach (var name in new[] { "test-pre", "test-post", "test-shared-pre-A", "test-shared-pre-B", "test-shared-post-A", "test-shared-post-B", "test-first", "test-last" })
            Preptest(name + ".log", 3);

        CheckOutput(
            OutputExpectation.Content("test-pre.log", "zero"),
            OutputExpectation.Content("test-pre.log.1", "first"),
            OutputExpectation.Content("test-pre.log.2", "second"),
            OutputExpectation.Content("test-pre.log.3", "third"));

        GenConfig("test-config.109", Config109);

        Run("test-config.109", "-f");
        ExitCode.Should().NotBe(0, "the failing scripts must make the run report an error");

        CheckOutput(
            OutputExpectation.Content("test-pre.log", "zero"),
            OutputExpectation.Content("test-pre.log.1", "first"),
            OutputExpectation.Content("test-pre.log.2", "second"),
            OutputExpectation.Content("test-pre.log.3", "third"));
        File.Exists(P("test-pre.log.4")).Should().BeFalse();

        CheckOutput(
            OutputExpectation.Content("test-post.log", ""),
            OutputExpectation.Content("test-post.log.1", "zero"),
            OutputExpectation.Content("test-post.log.2", "first"),
            OutputExpectation.Content("test-post.log.3", "second"),
            OutputExpectation.Content("test-post.log.4", "third"));

        foreach (var prefix in new[] { "test-shared-pre-A", "test-shared-pre-B" })
        {
            CheckOutput(
                OutputExpectation.Content($"{prefix}.log", "zero"),
                OutputExpectation.Content($"{prefix}.log.1", "first"),
                OutputExpectation.Content($"{prefix}.log.2", "second"),
                OutputExpectation.Content($"{prefix}.log.3", "third"));
            File.Exists(P($"{prefix}.log.4")).Should().BeFalse();
        }

        foreach (var prefix in new[] { "test-shared-post-A", "test-shared-post-B" })
        {
            CheckOutput(
                OutputExpectation.Content($"{prefix}.log", ""),
                OutputExpectation.Content($"{prefix}.log.1", "zero"),
                OutputExpectation.Content($"{prefix}.log.2", "first"),
                OutputExpectation.Content($"{prefix}.log.3", "second"),
                OutputExpectation.Content($"{prefix}.log.4", "third"));
        }

        CheckOutput(
            OutputExpectation.Content("test-first.log", "zero"),
            OutputExpectation.Content("test-first.log.1", "first"),
            OutputExpectation.Content("test-first.log.2", "second"),
            OutputExpectation.Content("test-first.log.3", "third"));
        File.Exists(P("test-first.log.4")).Should().BeFalse();

        CheckOutput(
            OutputExpectation.Content("test-last.log", ""),
            OutputExpectation.Content("test-last.log.1", "zero"),
            OutputExpectation.Content("test-last.log.2", "first"),
            OutputExpectation.Content("test-last.log.3", "second"));
        File.Exists(P("test-last.log.4")).Should().BeFalse();
    }

    /// <summary>
    /// Test 110: 'create' argument parsing - "mode uid gid" in the 3-arg
    /// form, "uid gid" in the 2-arg form, and 'su "user" "group"' errors on an
    /// unknown user/group. The reference greps the verbose "creating new"
    /// lines which LogRotateWin also prints (minus the 'creating new' prefix
    /// the reference does not require).
    /// </summary>
    [Fact]
    public void Test0110_CreateModeArguments()
    {
        foreach (var f in new[] { "test1.log", "test2.log", "test3.log", "test4.log" })
            WriteFile(f, "zero\n");
        GenConfig("test-config.110", Config110);

        Run("test-config.110", "--force");
        ExitCode.Should().NotBe(0, "the su line with an unknown group must be an error");
        Log.Should().Contain("test1.log mode = 0755 uid = 1 gid = 2");
        Log.Should().Contain("test2.log mode = 0644 uid = 1 gid = 2");
        Log.Should().Contain("test3.log mode = 0700 uid = 0 gid = 0");
        Log.Should().Contain("unknown group 'bar baz'");
        Log.Should().Contain("unknown user 'foo bar'");
    }

    /// <summary>
    /// DEVIATION from the reference: 'create' accepts Windows account names
    /// for owner and group (instead of POSIX uid/gid lookups). They are
    /// resolved to SIDs and the SID's last sub-authority is used as the uid/
    /// gid number, so e.g. Everyone (S-1-1-0) maps to 0.
    /// </summary>
    [Fact]
    public void Test0110_WindowsUserGroupNamesInCreate()
    {
        WriteFile("test1.log", "zero\n");
        GenConfig("test-config.110win", Config110Win);

        Run("test-config.110win", "--force");
        ExitCode.Should().Be(0);
        Log.Should().Contain("resolved to S-1-1-0");
        Log.Should().Contain("test1.log mode = 0600 uid = 0 gid = 0");
    }

    /// <summary>
    /// Test 111: '%z' dateformat specifier (timezone offset) - the old
    /// dated log from a previous timezone epoch is pruned, the other stays.
    /// </summary>
    [Fact]
    public void Test0111_DateFormatTimezone()
    {
        Preptest("test.log", 0);
        WriteFile("test.log.2000-01-01+0100", "foo\n");
        WriteFile("test.log.2001-01-01-1200", "bar\n");
        GenConfig("test-config.111", Config111);

        Run("test-config.111", "--force");
        ExitCode.Should().Be(0);

        string z = DateTimeOffset.Now.ToString("zzz").Replace(":", "");
        string dated = $"test.log.{DateTime.Now:yyyy-MM-dd}{z}";
        File.Exists(P(dated)).Should().BeTrue();
        File.Exists(P("test.log.2000-01-01+0100")).Should().BeFalse("the old timed backup must be pruned");
        CheckOutput(
            OutputExpectation.Content("test.log", ""),
            OutputExpectation.Content(dated, "zero"),
            OutputExpectation.Content("test.log.2001-01-01-1200", "bar"));
    }

    /// <summary>
    /// Test 112: named pipes (FIFO) must not hang logrotate.
    /// DEVIATION: Windows has no first-class FIFOs that cmd.exe can create,
    /// so the FIFO scenarios (test_reg.log rotation with test_fifo.log in the
    /// same glob) cannot be reproduced; skipped.
    /// </summary>
    [Fact(Skip = "Named pipes (FIFO) are not supported on Windows - cannot reproduce the FIFO rotation scenario")]
    public void Test0112_NamedPipeDoesNotHang() { }

    private const string Config94 = """
        create

        "&DIR&/test.log" {
            monthly
            rotate 1
            olddir &DIR&/testdir
            createolddir
        }
        """;

    private const string Config100 = """
        create

        "&DIR&/test.log" {
            monthly
            rotate 1
            addextension .newext
        }
        """;

    private const string Config101 = """
        create

        "&DIR&/test.log" {
            monthly
            rotate 1
            addextension .log
        }
        """;

    private const string Config102 = """
        "&DIR&/test.log" {
         daily
         size=0

        firstaction
         /bin/sh -c "echo test123"
         endscript
        }
        """;

    private const string Config103 = """
        random noise
        a b c d
        a::x

        "&DIR&/test.log" {
         daily
         size=0

        firstaction
         /bin/sh -c "echo test123"
         endscript
        }
        """;

    private const string Config104 = """
        "&DIR&/test1.log" {
            newkeyword
            create
            rotate 1
        }

        "&DIR&/test2.log" {
            create
            rotate 1
        }
        """;

    private const string Config105 = """
        "&DIR&/test1.log" {
            g@rbag?[]+#*
            rotate 1
        }

        "&DIR&/test2.log" {
            create
            rotate 1
        }
        """;

    private const string Config106 = """
        include ~/includedir
        """;

    private const string Config106Included = """
        "&DIR&/test.log" {
          rotate 1
          create
          olddir ~/old
          createolddir 700
        }
        """;

    private const string Config107 = """
        "&DIR&/test.log" {
            rotate 1
            ignoreduplicates
        }

        "&DIR&/*.log" {
            rotate 1
        }
        """;

    private const string Config108 = """
        "&DIR&/test.log" {
            create
            compress
            daily
            rotate 1
        }
        """;

    private const string Config109 = """
        "&DIR&/test-pre.log" {
            create
            rotate 3
            prerotate
                exit /b 1
            endscript
        }

        "&DIR&/test-post.log" {
            create
            rotate 3
            postrotate
                exit /b 1
            endscript
        }

        "&DIR&/test-shared-pre*.log" {
            create
            rotate 3
            sharedscripts
            prerotate
                exit /b 1
            endscript
        }

        "&DIR&/test-shared-post*.log" {
            create
            rotate 3
            sharedscripts
            postrotate
                exit /b 1
            endscript
        }

        "&DIR&/test-first.log" {
            create
            rotate 3
            firstaction
                exit /b 1
            endscript
        }

        "&DIR&/test-last.log" {
            create
            rotate 3
            lastaction
                exit /b 1
            endscript
        }
        """;

    private const string Config110 = """
        "&DIR&/test1.log" {
            daily
            rotate 1
            create 0755 1 2
        }

        "&DIR&/test2.log" {
            daily
            rotate 1
            create 1 2
        }

        "&DIR&/test3.log" {
            daily
            rotate 1
            create 0700
        }

        "&DIR&/test4.log" {
            daily
            rotate 1
            su "foo bar" 'bar baz'
        }
        """;

    private const string Config110Win = """
        "&DIR&/test1.log" {
            daily
            rotate 1
            create 0600 Everyone Everyone
        }
        """;

    private const string Config111 = """
        create

        "&DIR&/test.log" {
            daily
            dateext
            dateformat .%Y-%m-%d%z
            rotate 2
        }
        """;
}
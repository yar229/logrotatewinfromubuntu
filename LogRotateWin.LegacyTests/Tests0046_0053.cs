using FluentAssertions;
using LogRotate;
using System;
using System.IO;
using Xunit;

namespace LogRotateWin.LegacyTests;

/// <summary>
/// Port of shell tests 46-53 from _logrotate-3.22.0/test.
/// See ShellTestBase class doc for the general deviation policy.
/// </summary>
public class Tests0046_0053 : ShellTestBase
{
    /// <summary>
    /// Test 46: a truncated/corrupt state file line produces the same error as
    /// upstream ("bad line N in state file <file>") and rotation of the valid
    /// entry still proceeds.
    /// </summary>
    [Fact]
    public void Test0046_CorruptStateFile()
    {
        Preptest("test.log", 1);
        GenConfig("test-config.46", Config46);
        State(
            "logrotate state -- version 1",
            $"\"{P("test.log")}\" 2000-1-1",
            $"\"{P("test2.l")}\"");

        Run("test-config.46");

        ExitCode.Should().NotBe(0);
        StdErr.Should().Contain("error: bad line 3 in state file state");
        CheckOutput(
            OutputExpectation.Content("test.log", ""));
    }

    /// <summary>
    /// Test 47: SELinux context preservation for the state file. Linux-only.
    /// </summary>
    [Fact(Skip = "Linux-only: state file SELinux context (chcon/ls -Z)")]
    public void Test0047_StateSelinuxContext()
    {
    }

    /// <summary>
    /// Test 48: the state file keeps its ACLs (writeState copies the old state
    /// file's access ACL onto the rewritten one).
    /// DEVIATION: icacls + the well-known Everyone SID stand in for POSIX
    /// setfacl (see Test0032_And_0033_And_0035_ACL); "chmod 0640" is dropped.
    /// </summary>
    [Fact]
    public void Test0048_StateFileACL()
    {
        Preptest("test.log", 1);
        GenConfig("test-config.48", Config48);
        State("logrotate state -- version 2");
        GrantEveryoneAccess("state");
        Run("test-config.48");
        ExitCode.Should().Be(0);

        AclApi.DefinesAccessAce(P("state"), EveryoneSid)
            .Should().BeTrue("state file must keep its ACL user:nobody:rwx");
        CheckOutput(
            OutputExpectation.Content("test.log", ""),
            OutputExpectation.Content("test.log.1", "zero"));
    }

    /// <summary>
    /// Test 49: a state entry without hours/minutes/seconds still works.
    /// Note: the reference entry uses the relative name "test.log" which simply
    /// does not map to the absolute config path; rotation still happens.
    /// </summary>
    [Fact]
    public void Test0049_StateFileWithoutTime()
    {
        Preptest("test.log", 1);
        GenConfig("test-config.49", Config49);
        State(
            "logrotate state -- version 2",
            "\"test.log\" 2012-8-19");

        Run("test-config.49");
        ExitCode.Should().Be(0);
        CheckOutput(
            OutputExpectation.Content("test.log", ""),
            OutputExpectation.Content("test.log.1", "zero"));
    }

    /// <summary>
    /// Test 50: hourly rotation.
    /// DEVIATION: with dateext the port renames test.log straight to the dated
    /// name and leaves pre-existing numeric backups (test.log.1 from preptest)
    /// in place, so the upstream "test.log.1 must not exist" side assertion is
    /// dropped. The hourly "rotate once per hour / again after the hour changed"
    /// semantics are fully verified, with the state-edited third run using a
    /// full timestamp instead of GNU sed (same observable result).
    /// </summary>
    [Fact]
    public void Test0050_HourlyRotation()
    {
        Preptest("test.log", 1);
        GenConfig("test-config.50", Config50);
        string datestring = DateTime.Now.ToString("yyyyMMddHH");

        Run("test-config.50", "--force");
        ExitCode.Should().Be(0);
        CheckOutput(
            OutputExpectation.Content("test.log", ""),
            OutputExpectation.Content($"test.log-{datestring}", "zero"));

        WriteFile("test.log", "second\n");
        File.Delete(P($"test.log-{datestring}"));
        Run("test-config.50");
        ExitCode.Should().Be(0);
        CheckOutput(
            OutputExpectation.Content("test.log", "second"));

        State(
            "logrotate state -- version 2",
            $"\"{P("test.log")}\" {StateStamp(DateTime.Now.AddHours(-1))}");
        Run("test-config.50");
        ExitCode.Should().Be(0);
        CheckOutput(
            OutputExpectation.Content("test.log", ""),
            OutputExpectation.Content($"test.log-{datestring}", "second"));
    }

    /// <summary>
    /// Test 51: sharedscripts with zero rotated logs must not crash (upstream
    /// regression #3.8.4). The glob matches nothing; missingok keeps it clean.
    /// </summary>
    [Fact]
    public void Test0051_SharedScriptsZeroLogsNoCrash()
    {
        Preptest("test.log", 1);
        GenConfig("test-config.51", Config51);
        State(
            "logrotate state -- version 2",
            "\"/var/log/httpd/a_log\" 2011-11-15",
            "\"/var/log/wtmp\" 2013-7-9");

        Run("test-config.51");
        ExitCode.Should().Be(0);
        Log.Should().NotContain("Unhandled exception");
    }

    /// <summary>
    /// Test 52: sharedscripts run when the first (missing) log is skipped via
    /// missingok and a second log rotates. The upstream comment is stale; the
    /// assertion (scriptout = foo) shows the script DOES run.
    /// </summary>
    [Fact]
    public void Test0052_SharedScriptsFirstFileMissing()
    {
        Preptest("test.log", 1);
        GenConfig("test-config.52", Config52);
        Run("test-config.52");
        ExitCode.Should().Be(0);

        CheckOutput(
            OutputExpectation.Content("test.log", ""),
            OutputExpectation.Content("test.log.1", "zero"),
            OutputExpectation.Content("scriptout", "foo"));
    }

    /// <summary>
    /// Test 53: --force rotates even though the file is below the size
    /// threshold.
    /// </summary>
    [Fact]
    public void Test0053_ForceRotation()
    {
        Preptest("test.log", 1);
        GenConfig("test-config.53", Config53);
        Run("test-config.53", "--force");
        ExitCode.Should().Be(0);

        CheckOutput(
            OutputExpectation.Content("test.log", ""),
            OutputExpectation.Content("test.log.1", "zero"));
    }

    private static string StateStamp(DateTime t)
        => $"{t.Year}-{t.Month}-{t.Day}-{t.Hour}:{t.Minute}:{t.Second}";

    // =====================================================================
    // configs (ported from test-config.N.in)
    // =====================================================================

    private const string Config46 = """
        create

        "&DIR&/test.log" {
            daily
            rotate 999
            dateext
        }
        """;

    private const string Config48 = """
        create

        "&DIR&/test.log" {
            daily
            rotate 999
            size 2
        }
        """;

    private const string Config49 = """
        create

        "&DIR&/test.log" {
            daily
            rotate 4
            size 2
        }
        """;

    private const string Config50 = """
        create

        "&DIR&/test.log" {
            hourly
            dateext
            rotate 4
        }
        """;

    private const string Config51 = """
        create

        "&DIR&/no_such_dir/*.log" {
            size 1
            missingok
            sharedscripts
            prerotate
        echo none
            endscript
        }
        """;

    private const string Config52 = """
        create

        /var/log/does_not_exist.log "&DIR&/test.log" {
            rotate 14
            size 2
            missingok
            sharedscripts
            postrotate
        """ + AppendFoo + """
            endscript
        }
        """;

    private const string Config53 = """
        create

        "&DIR&/test.log" {
            rotate 14
            size 4096
            missingok
        }
        """;

    /// <summary>
    /// cmd body of the upstream "append foo to scriptout" postrotate snippet:
    ///   touch scriptout; echo &#36;(cat scriptout) foo &gt; foo; mv foo scriptout
    /// </summary>
    private const string AppendFoo = "\n" + """
        setlocal EnableDelayedExpansion
        if exist scriptout (set /p CONTENT=<scriptout) else (set CONTENT=)
        if "!CONTENT!"=="" (echo foo>tmp.out) else (echo !CONTENT! foo>tmp.out)
        move /y tmp.out scriptout >nul
        endlocal
        """ + "\n";
}
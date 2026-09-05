using FluentAssertions;
using System;
using System.IO;
using Xunit;

namespace LogRotateWin.LegacyTests;

/// <summary>
/// Port of shell tests 31-40 from _logrotate-3.22.0/test.
/// See ShellTestBase class doc for the general deviation policy.
/// </summary>
public class Tests0031_0040 : ShellTestBase
{
    /// <summary>
    /// Test 31: mode in the 'create' directive.
    /// DEVIATION: the reference chmods the new file to 0600 and stat-checks it;
    /// Windows cannot apply POSIX modes, but the port still reports the intended
    /// mode in its verbose "creating new ... mode = 0600" message, so that is
    /// asserted instead.
    /// </summary>
    [Fact]
    public void Test0031_CreateWithMode()
    {
        Preptest("test.log", 1);
        GenConfig("test-config.31", Config31);
        Run("test-config.31", "--force");
        ExitCode.Should().Be(0);

        Log.Should().Contain("creating new ").And.Contain("mode = 0600 uid = 0 gid = 0");
        CheckOutput(
            OutputExpectation.Content("test.log", ""),
            OutputExpectation.Content("test.log.1", "zero"));
    }

    /// <summary>
    /// Tests 32/33/35: ACLs are Linux-only (setfacl/getfacl); LogRotateWin is
    /// built with "ACL support: no", like the reference without libacl.
    /// The upstream suite itself skips these with exit 77 when ACLs are absent.
    /// </summary>
    [Fact(Skip = "Linux-only: ACL tests use setfacl/getfacl; port built without ACL support")]
    public void Test0032_And_0033_And_0035_ACL()
    {
    }

    /// <summary>
    /// Test 34: create without mode but with user/group. As a non-root user the
    /// reference cannot chown, so it runs logrotate -d and greps the debug
    /// output "uid = 0 gid = 0". Matches the port's debug output verbatim.
    /// </summary>
    [Fact]
    public void Test0034_CreateUserGroupDebug()
    {
        Preptest("test.log", 1);
        GenConfig("test-config.34", Config34);
        Run("test-config.34", "-d", "-f");
        ExitCode.Should().Be(0);

        Log.Should().Contain("uid = 0 gid = 0");
        CheckOutput(
            OutputExpectation.Content("test.log", "zero"));
    }

    /// <summary>
    /// Test 36: "size 300x" - 'x' is an unknown unit, the config is skipped
    /// and the log is untouched.
    /// </summary>
    [Fact]
    public void Test0036_UnknownSizeUnit()
    {
        Preptest("test.log", 1);
        GenConfig("test-config.36", Config36);
        Run("test-config.36", "--force");

        ExitCode.Should().NotBe(0);
        StdErr.Should().Contain("unknown unit 'x'");
        CheckOutput(
            OutputExpectation.Content("test.log", "zero"));
    }

    /// <summary>
    /// Test 37: a config with a broken first section (unknown unit + a stray
    /// '}' inside a firstaction script) is skipped, but a following valid
    /// section for the same file still runs. scriptout must contain "second"
    /// (the second firstaction ran; the first one never did).
    /// Script bodies are adapted to cmd syntax (port runs scripts via cmd).
    /// </summary>
    [Fact]
    public void Test0037_SkipBrokenSectionFirstactionStillRuns()
    {
        Preptest("test.log", 1);
        GenConfig("test-config.37", Config37);
        Run("test-config.37", "--force");

        ExitCode.Should().NotBe(0);
        StdErr.Should().Contain("skipping");
        CheckOutput(
            OutputExpectation.Content("test.log", ""),
            OutputExpectation.Content("test.log.1", "zero"),
            OutputExpectation.Content("scriptout", "second"));
    }

    /// <summary>
    /// Tests 38/39: preremove scripts with "rotate 0".
    /// DEVIATION: LogRotateWin rejects 'rotate 0' ("bad rotation count '0'"),
    /// so these configs never parse (same deviation as test 0006/0009).
    /// </summary>
    [Fact(Skip = "Deviation: LogRotateWin rejects 'rotate 0'; preremove only runs when a rotated file is removed (rotate 0)")]
    public void Test0038_And_0039_Preremove()
    {
    }

    /// <summary>
    /// Test 40: tabooext/taboopat filtering during a directory include.
    /// DEVIATION: the port only supports single-file include, not directory
    /// includes ("cannot stat ...: No such file or directory"), so the taboo
    /// machinery is unreachable. Skipped.
    /// </summary>
    [Fact(Skip = "Deviation: LogRotateWin include supports single config files, not directories; taboo filtering is only used by directory includes")]
    public void Test0040_TabooextAndDirectoryInclude()
    {
    }

    // =====================================================================
    // configs (ported from test-config.N.in)
    // =====================================================================

    private const string Config31 = """
        create 0600 root root

        "&DIR&/test.log" {
            daily
            rotate 999
        }
        """;

    private const string Config34 = """
        create root root

        "&DIR&/test.log" {
            daily
            rotate 1
        }
        """;

    private const string Config36 = """
        "&DIR&/test.log" {
            daily
            size 300x
            rotate 1
        }
        """;

    private const string Config37 = """
        "&DIR&/test.log" {
            daily
            size 300x
            firstaction
                echo none>tmp.out
                move /y tmp.out scriptout >nul
        }
                echo x>foo }
                move /y foo scriptout >nul
            endscript
            rotate 1
        }

        "&DIR&/test.log" {
            create
            daily
            firstaction
        echo second>tmp.out
        move /y tmp.out scriptout >nul
            endscript
            rotate 1
        }
        """;
}
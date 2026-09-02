using FluentAssertions;
using logrotate.Tests.Integration.GarbageTests.Wrappers;
using System.CodeDom;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using Directives = logrotate.Config.ConfigSectionDirectives;

namespace logrotate.Tests.Integration.GarbageTests;

[Trait("Category", "Integration")]
public class PrePostRotateTests : IntegrationTestBase, INewWaveTests
{
    public ITestOutputHelper Output { get; private set; }

    public PrePostRotateTests(ITestOutputHelper output)
    {
        TestHelpersGarbage.CleanupTestDir(false);
        Output = output;
    }
    public override void Dispose()
    {
        TestHelpersGarbage.CleanupTestDir(false); //TODO: make true after refactoring
    }

    [Fact]
    public void SimpleLogFileWithPrePostRotate_ShoudBeRotated()
    {
        var markerPre = new XFile("marker-pre.txt");
        var markerPost = new XFile("marker-post.txt");

        new XRunner(this)
        {
            Files = [
                new XLogFile("a.log")
                    .Create()
                    .ShouldNotBe()
                    .ShouldBe(".1"),
                markerPre.ShouldBe(),
                markerPost.ShouldBe(),
            ],
            Config = new XConfig()
                .WithSection([XPattern.AllLogs], s => s
                    .With(Directives.Rotate, 2)
                    .With(Directives.Monthly)
                    .WithEcho(Directives.PreRotate, markerPre)
                    .WithEcho(Directives.PostRotate, markerPost) )
                .Create()
        }
        .RunAndCheck();
    }

    [Fact]
    public void PrePostRotate_PatternShouldBePassed()
    {
        var log = new XLogFile("a.log").Create();

        var markerPre = new XFile("marker-pre.txt");
        var markerPost = new XFile("marker-post.txt");

        new XRunner(this)
        {
            Files = [
                log.ShouldNotBe()
                   .ShouldBe(".1"),
                markerPre.ShouldBe()
                    .ShouldContain(XPattern.AllLogs)
                    .ShouldNotContain(log),
                markerPost.ShouldBe()
                    .ShouldContain(XPattern.AllLogs)
                    .ShouldNotContain(log)
            ],
            Config = new XConfig()
                .WithSection([XPattern.AllLogs], s => s
                    .With(Directives.Rotate, 2)
                    .With(Directives.Monthly)
                    .With(Directives.SharedScripts)
                    .WithScript(Directives.PreRotate, $"echo prerotate for %1 >> {markerPre}")
                    .WithScript(Directives.PostRotate, $"echo postrotate for %1 >> {markerPost}"))
                .Create()
        }
        .RunAndCheck();
    }

    [Fact]
    public void PrePostRotate_ShoudExecuteForEachLog()
    {
        var logA = new XLogFile("a.log").Create();
        var logB = new XLogFile("b.log").Create();

        var markerPre = new XFile("marker-pre.txt");
        var markerPost = new XFile("marker-post.txt");

        new XRunner(this)
        {
            Files = [
                logA.ShouldNotBe()
                    .ShouldBe(".1"),
                logB.ShouldNotBe()
                    .ShouldBe(".1"),
                markerPre
                    .ShouldContain(logA.Filepath)
                    .ShouldContain(logB.Filepath),
                markerPost
                    .ShouldContain(logA.Filepath)
                    .ShouldContain(logB.Filepath)
            ],
            Config = new XConfig()
                .WithSection([XPattern.AllLogs], s => s
                    .With(Directives.Rotate, 2)
                    .With(Directives.Monthly)
                    .WithScript(Directives.PreRotate, $"echo prerotate for %1 >> {markerPre}")
                    .WithScript(Directives.PostRotate, $"echo postrotate for %1 >> {markerPost}") )
                .Create()
        }
        .RunAndCheck();
    }

    [Fact]
    public void PrePostRotate_ShouldExecuteOnlyOnce()
    {
        var logA = new XLogFile("a.log").Create();
        var logB = new XLogFile("b.log").Create();

        var markerPre = new XFile("marker-pre.txt");
        var markerPost = new XFile("marker-post.txt");

        new XRunner(this)
        {
            Files = [
                logA.ShouldBe(".1"),
                logB.ShouldBe(".1"),
                markerPre
                    .ShouldContain(XPattern.AllLogs.Filepath)
                    .ShouldNotContain(logA.Filepath, logB.Filepath),
                markerPost
                    .ShouldContain(XPattern.AllLogs.Filepath)
                    .ShouldNotContain(logA.Filepath, logB.Filepath)
            ],
            Config = new XConfig()
                .WithSection([XPattern.AllLogs], s => s
                    .With(Directives.Rotate, 2)
                    .With(Directives.Monthly)
                    .With(Directives.SharedScripts)
                    .WithScript(Directives.PreRotate, $"echo prerotate for %1 >> {markerPre}")
                    .WithScript(Directives.PostRotate, $"echo postrotate for %1 >> {markerPost}") )
                .Create()
        }
        .RunAndCheck();
    }

    [Fact]
    public void PrePostRotateWithNoLogs_ShouldNotExecute()
    {
        var markerPre = new XFile("marker-pre.txt");
        var markerPost = new XFile("marker-post.txt");

        new XRunner(this)
        {
            Files = [
                markerPre.ShouldNotBe(),
                markerPost.ShouldNotBe()
            ],
            Config = new XConfig()
                .WithSection([XPattern.AllLogs], s => s
                    .With(Directives.Rotate, 2)
                    .With(Directives.Monthly)
                    .With(Directives.SharedScripts)
                    .WithScript(Directives.PreRotate, $"echo prerotate for %1 >> {markerPre}")
                    .WithScript(Directives.PostRotate, $"echo postrotate for %1 >> {markerPost}"))
                .Create()
        }
        .RunAndCheck();
    }

    [Fact]
    public void PrePostRotateWithNoRotatedLogs_ShouldNotExecute()
    {
        var logA = new XLogFile("a.log").Create();

        var markerPre = new XFile("marker-pre.txt");
        var markerPost = new XFile("marker-post.txt");

        new XRunner(this)
        {
            Files = [
                logA.ShouldBe()
                    .ShouldNotBe(".1"),
                markerPre.ShouldNotBe(),
                markerPost.ShouldNotBe()
            ],
            State = new XStateFile()
                .WithProcessed(logA)
                .Create(),
            Config = new XConfig()
                .WithSection([XPattern.AllLogs], s => s
                    .With(Directives.Rotate, 2)
                    .With(Directives.Monthly)
                    .With(Directives.SharedScripts)
                    .WithScript(Directives.PreRotate, $"echo prerotate for %1 >> {markerPre}")
                    .WithScript(Directives.PostRotate, $"echo postrotate for %1 >> {markerPost}"))
                .Create()
        }
        .RunAndCheck();
    }


    //TODO: runnig logs randomly fails, wtf

    //TODO: fails cause of a.log and b.log not deleted, just becomes empty, need to figure out wtf
    //[Fact]
    //public void SimpleLogFileWithLocalActions_ShoudBeExecuted()
    //{
    //    var log = new XLogFile("a.log").Create();

    //    var markerFirst = new XFile("marker-first.txt");
    //    var markerLast = new XFile("marker-last.txt");

    //    new XRunner(this)
    //    {
    //        Files = [
    //            log.ShouldNotBe()
    //               .ShouldBe(".1"),
    //            markerFirst.ShouldBe(),
    //            markerLast.ShouldBe(),
    //        ],
    //        Config = new XConfig()
    //            .WithSection([XPattern.AllLogs], s => s
    //                .With(Directives.Rotate, 2)
    //                .With(Directives.Monthly)
    //                .WithEcho(Directives.FirstAction, markerFirst)
    //                .WithEcho(Directives.LastAction, markerLast))
    //            .Create()
    //    }
    //    .RunAndCheck();
    //}


    //TODO: fails cause of a.log and b.log not deleted, just becomes empty, need to figure out wtf
    //[Fact]
    //public void PrePostRotate_ShoudExecuteOnlyOnce()
    //{
    //    var logA = new XLogFile("a.log").Create();
    //    var logB = new XLogFile("b.log").Create();
    //
    //    var markerPre = new XFile("marker-pre.txt");
    //    var markerPost = new XFile("marker-post.txt");
    //
    //    new XRunner(this)
    //    {
    //        Files = [
    //            logA .ShouldNotBe()
    //                .ShouldBe(".1"),
    //            logB .ShouldNotBe()
    //                .ShouldBe(".1"),
    //            markerPre.ShouldBe()
    //                .ShouldContain(XPattern.AllLogs.Filepath)
    //                .ShouldNotContain(logA.Filepath)
    //                .ShouldNotContain(logB.Filepath),
    //            markerPost.ShouldBe()
    //                .ShouldContain(XPattern.AllLogs.Filepath)
    //                .ShouldNotContain(logA.Filepath)
    //                .ShouldNotContain(logB.Filepath)
    //        ],
    //        Config = new XConfig()
    //            .WithSection([XPattern.AllLogs], s => s
    //                .With(Directives.Rotate, 2)
    //                .With(Directives.Monthly)
    //                .With(Directives.SharedScripts)
    //                .WithScript(Directives.PreRotate, $"echo prerotate for %1 >> {markerPre}")
    //                .WithScript(Directives.PostRotate, $"echo postrotate for %1 >> {markerPost}"))
    //            .Create()
    //    }
    //    .RunAndCheck();
    //}



    // just extended example
    //[Fact]
    //public void OneShouldBeRotated_And_OtherOneNot_WithDupsInSameConfigSection()
    //{
    //    var logA = new FooLogFile("log-a.log").Create();
    //    var logB = new FooLogFile("log-b.log").Create();

    //    var state = new FooStateFile().WithProcessed(logB).Create();

    //    var config = new FooConfig()
    //        .WithSection([logA, logA, logB, logB], s => s
    //            .With("rotate", 2)
    //            .With("monthly"))
    //        .Create();

    //    RunLogRotate("-s", state, config);

    //    logA.Exists().Should().BeFalse("should be deleted {0}", logA.Filename);
    //    logA.Exists(".1").Should().BeTrue("should be rotated {0}", logA.Filename);

    //    logB.Exists().Should().BeTrue("should NOT be deleted {0}", logB.Filename);
    //    logB.Exists(".1").Should().BeFalse("should NOT be rotated {0}", logB.Filename);
    //}
}

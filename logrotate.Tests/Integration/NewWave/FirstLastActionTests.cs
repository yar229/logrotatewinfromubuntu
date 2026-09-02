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
public class FirstLastActionTests : IntegrationTestBase, INewWaveTests
{
    public ITestOutputHelper Output { get; private set; }

    public FirstLastActionTests(ITestOutputHelper output)
    {
        TestHelpersGarbage.CleanupTestDir(false);
        Output = output;
    }
    public override void Dispose()
    {
        TestHelpersGarbage.CleanupTestDir(false); //TODO: make true after refactoring
    }

    [Fact]
    public void SimpleLogFileWithGlobalActions_ShoudBeExecuted()
    {
        var log = new XLogFile("a.log").Create();

        var markerFirst = new XFile("marker-first.txt");
        var markerLast = new XFile("marker-last.txt");

        new XRunner(this)
        {
            Files = [
                log.ShouldNotBe()
                   .ShouldBe(".1"),
                markerFirst.ShouldBe(),
                markerLast.ShouldBe(),
            ],
            Config = new XConfig()
                .WithSection([], s => s
                    .WithEcho(Directives.FirstAction, markerFirst)
                    .WithEcho(Directives.LastAction, markerLast))
                .WithSection([XPattern.AllLogs], s => s
                    .With(Directives.Rotate, 2)
                    .With(Directives.Monthly) )
                .Create()
        }
        .RunAndCheck();
    }

    [Fact]
    public void SimpleLogFileWithLocalActions_ShoudBeExecuted()
    {
        var log = new XLogFile("a.log").Create();

        var markerFirst = new XFile("marker-first.txt");
        var markerLast = new XFile("marker-last.txt");

        new XRunner(this)
        {
            Files = [
                log.ShouldBe(".1"),
                markerFirst.ShouldBe(),
                markerLast.ShouldBe(),
            ],
            Config = new XConfig()
                .WithSection([XPattern.AllLogs], s => s
                    .With(Directives.Rotate, 2)
                    .With(Directives.Monthly)
                    .WithEcho(Directives.FirstAction, markerFirst)
                    .WithEcho(Directives.LastAction, markerLast) ) 
                .Create()
        }
        .RunAndCheck();
    }

    //[Fact]
    //public void PrePostRotate_ShoudExecuteForEachLog()
    //{
    //    var logA = new XLogFile("a.log").Create();
    //    var logB = new XLogFile("b.log").Create();

    //    var markerPre = new XFile("marker-pre.txt");
    //    var markerPost = new XFile("marker-post.txt");

    //    new XRunner(this)
    //    {
    //        Files = [
    //            logA.ShouldNotBe()
    //                .ShouldBe(".1"),
    //            logB.ShouldNotBe()
    //                .ShouldBe(".1"),
    //            markerPre
    //                .ShouldContain(logA.Filepath)
    //                .ShouldContain(logB.Filepath),
    //            markerPost
    //                .ShouldContain(logA.Filepath)
    //                .ShouldContain(logB.Filepath)
    //        ],
    //        Config = new XConfig()
    //            .WithSection([XPattern.AllLogs], s => s
    //                .With(Directives.Rotate, 2)
    //                .With(Directives.Monthly)
    //                .WithScript(Directives.PreRotate, $"echo prerotate for %1 >> {markerPre}")
    //                .WithScript(Directives.PostRotate, $"echo postrotate for %1 >> {markerPost}"))
    //            .Create()
    //    }
    //    .RunAndCheck();
    //}

    //[Fact]
    //public void PrePostRotate_ShouldExecuteOnlyOnce()
    //{
    //    var logA = new XLogFile("a.log").Create();
    //    var logB = new XLogFile("b.log").Create();

    //    var markerPre = new XFile("marker-pre.txt");
    //    var markerPost = new XFile("marker-post.txt");

    //    new XRunner(this)
    //    {
    //        Files = [
    //            logA.ShouldBe(".1"),
    //            logB.ShouldBe(".1"),
    //            markerPre
    //                .ShouldContain(XPattern.AllLogs.Filepath)
    //                .ShouldNotContain(logA.Filepath, logB.Filepath),
    //            markerPost
    //                .ShouldContain(XPattern.AllLogs.Filepath)
    //                .ShouldNotContain(logA.Filepath, logB.Filepath)
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

    //[Fact]
    //public void PrePostRotateWithNoLogs_ShouldNotExecute()
    //{
    //    var markerPre = new XFile("marker-pre.txt");
    //    var markerPost = new XFile("marker-post.txt");

    //    new XRunner(this)
    //    {
    //        Files = [
    //            markerPre.ShouldNotBe(),
    //            markerPost.ShouldNotBe()
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

    //[Fact]
    //public void PrePostRotateWithNoRotatedLogs_ShouldNotExecute()
    //{
    //    var logA = new XLogFile("a.log").Create();

    //    var markerPre = new XFile("marker-pre.txt");
    //    var markerPost = new XFile("marker-post.txt");

    //    new XRunner(this)
    //    {
    //        Files = [
    //            logA.ShouldBe()
    //                .ShouldNotBe(".1"),
    //            markerPre.ShouldNotBe(),
    //            markerPost.ShouldNotBe()
    //        ],
    //        State = new XStateFile()
    //            .WithProcessed(logA)
    //            .Create(),
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


}

using FluentAssertions;
using PostCsConvertation.Tests.Integration.Base;
using PostCsConvertation.Tests.Integration.Wrappers;
using System;
using Xunit;
using Op = LogRotate.Consts.ConfigSectionDirectives;

namespace PostCsConvertation.Tests.Integration;

/// <summary>
/// 'start' config directive tests
/// </summary>
/// <remarks>
/// start by default is 1
/// start cannot be 0
/// </remarks>
[Trait("Category", "Integration")]
public class StartDirectiveTests : NewWaveIntegrationTestBase
{
    public StartDirectiveTests(ITestOutputHelper output)
        : base(output)
    {
    }

    [Fact(DisplayName = $"{Op.Start} with zero")]
    public void RotateLog_WithStart0_ShouldPass()
    {
        var log = Runner.NewLog().Create();

        Runner
            .WithLog(log, l => l.Create()
                .ShouldNotBe()
                .ShouldBe(Ext(".0")))
            .WithConfig(c => c
                .WithSection(log, s => s
                    .With(Op.Rotate, 3)
                    .With(Op.Start, 0))
                .Create())
            .RunAndCheck()
            .ExitCode.Should().Be(0, $"'{Op.Start}' directive can be zero");
    }
}

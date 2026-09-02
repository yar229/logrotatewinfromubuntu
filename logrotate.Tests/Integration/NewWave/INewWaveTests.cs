using Xunit.Abstractions;

namespace logrotate.Tests.Integration.GarbageTests;

public interface INewWaveTests
{
    ITestOutputHelper Output { get; }
    int RunLogRotate(params string[] args);
}

using Xunit.Abstractions;

namespace logrotate.Tests.Integration.NewWave.Base;

public interface INewWaveTests
{
    ITestOutputHelper Output { get; }
    
    int RunLogRotate(params string[] args);

    string TestDir { get; }
}

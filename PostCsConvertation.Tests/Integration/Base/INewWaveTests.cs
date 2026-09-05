using Xunit;

namespace PostCsConvertation.Tests.Integration.Base;

public interface INewWaveTests
{
    ITestOutputHelper Output { get; }
    
    int RunLogRotate(params string[] args);

    string TestDir { get; }

    string Log { get; }
}

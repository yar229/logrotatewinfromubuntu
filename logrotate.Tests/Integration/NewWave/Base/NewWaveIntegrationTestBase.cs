using logrotate.Tests.Integration.GarbageTests.Wrappers;
using Xunit.Abstractions;

namespace logrotate.Tests.Integration.NewWave.Base;

public abstract class NewWaveIntegrationTestBase : IntegrationTestBase, INewWaveTests
{
    public ITestOutputHelper Output { get; private set; }

    internal XRunner Runner { get; private set; }

    public NewWaveIntegrationTestBase(ITestOutputHelper output)
    {
        Output = output;
        Runner = new XRunner(this);
    }
}

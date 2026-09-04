using logrotate.Tests.Integration.NewWave.Wrappers;
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

    protected XExtension Ext(string ext) 
        => new XExtension(ext);
}

namespace KbStore.Tests.Api.Endpoints;

using MassTransit;
using MassTransit.Testing;
using NSubstitute;

#pragma warning disable CS8618
public abstract class Endpoint_Tests
{
    protected IServiceCollection Services;
    protected IServiceProvider? Provider;
    protected ITestHarness? Harness;

    public Endpoint_Tests()
    {
        Services = new ServiceCollection();
    }

    [OneTimeSetUp]
    public async Task InitializeOnce()
    {
        OnServicesCreating(Services);

        Services.AddMassTransitTestHarness(conf =>
        {
            OnHarnessCreating(conf);
        });
    }

    [SetUp]
    public async Task Setup()
    {
        await OnPreSetup();

        Mocks.Clear();

        Provider = Services.BuildServiceProvider(true);
        Harness = Provider.GetRequiredService<ITestHarness>();

        await Harness.Start();

        await OnPostSetup();
    }

    [TearDown]
    public async Task Teardown()
    {
        await OnPreTeardown();

        if (Harness is not null)
            await Harness.Stop();

        await OnPostTeardown();
    }
    
    [OneTimeTearDown]
    public async Task FinalizeOnce()
    {
        (Harness as IDisposable)?.Dispose();
        (Provider as IDisposable)?.Dispose();
    }

    protected virtual void OnServicesCreating(IServiceCollection services) { }
    protected virtual void OnHarnessCreating(IBusRegistrationConfigurator conf) { }

    protected virtual Task OnPreSetup() => Task.CompletedTask;
    protected virtual Task OnPostSetup() => Task.CompletedTask;
    protected virtual Task OnPreTeardown() => Task.CompletedTask;
    protected virtual Task OnPostTeardown() => Task.CompletedTask;

    protected List<object> Mocks = new();

    protected TService MockOf<TService>()
        where TService : class
    {
        var instance = Substitute.For<TService>();

        Mocks.Add(instance);
    }

    
}
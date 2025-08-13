
// ReSharper disable InconsistentNaming
// ReSharper disable MemberCanBePrivate.Global

namespace KbStore.Tests;

using MassTransit;
using MassTransit.Configuration;
using MassTransit.Saga;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

#pragma warning disable CS8618
public abstract class EventingTestBase
{
    protected IServiceCollection Services;
    protected IServiceProvider? RootProvider;
    protected IServiceScope ScopedProvider;
    
    protected ITestHarness Harness = null!;
    protected Exception? LastException;

    protected EventingTestBase()
    {
        Services = new ServiceCollection();
    }

    [OneTimeSetUp]
    public async Task InitializeOnce()
    {
        await Task.CompletedTask;

        OnServicesCreating(Services);

        Services.AddMassTransitTestHarness(conf =>
        {
            conf.SetTestTimeouts(
                testTimeout: TimeSpan.FromSeconds(5), 
                testInactivityTimeout: TimeSpan.FromSeconds(1)
            );

            conf.SetDefaultRequestTimeout(
                timeout: RequestTimeout.After(ms:250)
            );

            OnHarnessCreating(conf);
        });

        RootProvider = Services.BuildServiceProvider(true);
    }

    [SetUp]
    public async Task Setup()
    {
        await OnPreSetup();

        Harness = RootProvider!.GetRequiredService<ITestHarness>();
        ScopedProvider = RootProvider!.CreateScope();

        await Harness.Start();
        await OnPostSetup();
        
        Arrange();

        LastException = null;
        try
        {
            await Act();
        }
        catch (Exception e)
        {
            LastException = e;
        }
    }

    [TearDown]
    public async Task Teardown()
    {
        await OnPreTeardown();
        
        await Harness.Stop();

        await OnPostTeardown();

        ScopedProvider.Dispose();
    }

    [OneTimeTearDown]
    public async Task FinalizeOnce()
    {
        await DisposeAsync(Harness);
        await DisposeAsync(RootProvider);

        return;

        static ValueTask DisposeAsync(object? service) 
            => service is IAsyncDisposable dispose 
                ? dispose.DisposeAsync() 
                : ValueTask.CompletedTask;
    }

    protected virtual void OnServicesCreating(IServiceCollection services) { }
    protected virtual void OnHarnessCreating(IBusRegistrationConfigurator conf) { }

    protected virtual Task OnPreSetup() => Task.CompletedTask;
    protected virtual Task OnPostSetup() => Task.CompletedTask;
    protected virtual Task OnPreTeardown() => Task.CompletedTask;
    protected virtual Task OnPostTeardown() => Task.CompletedTask;

    protected abstract void Arrange();
    protected abstract Task Act();
}
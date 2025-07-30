
// ReSharper disable InconsistentNaming
// ReSharper disable MemberCanBePrivate.Global

namespace KbStore.ApiService.Tests.Api;

using System.Reflection;
using MassTransit;
using MassTransit.Testing;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NUnit.Framework;

#pragma warning disable CS8618
public abstract class TestBase
{
    protected IServiceCollection Services;
    protected IServiceProvider? RootProvider;
    protected IServiceScope ScopedProvider;
    
    protected ITestHarness? Harness;

    protected Exception? LastException;

    protected TestBase()
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
            conf.SetDefaultRequestTimeout(
                RequestTimeout.After(ms:250)
            );

            OnHarnessCreating(conf);
        });

        RootProvider = Services.BuildServiceProvider(true);
    }

    [SetUp]
    public async Task Setup()
    {
        await OnPreSetup();

        Mocks.Clear();

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

        if (Harness is not null)
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

    protected List<object> Mocks = new();

    protected TService MockOf<TService>()
        where TService : class
    {
        var instance = Substitute.For<TService>();

        Mocks.Add(instance);

        return instance;
    }

    protected TService? Resolve<TService>()
        where TService : class
    {
        return (TService?) Resolve(typeof(TService));
    }

    protected object? Resolve(Type service)
    {
        var mock = Mocks.FirstOrDefault(m => m.GetType() == service);

        return mock ?? ScopedProvider.ServiceProvider.GetService(service);
    }

    protected Task<IResult?> Execute(Delegate handler, params object?[] inputs)
    {
        var methodInfo = handler.Method;
        var parameters = methodInfo.GetParameters();
        var args = new object?[parameters.Length];

        var provided = new List<object?>(inputs);

        for (var i = 0; i < parameters.Length; i++)
        {
            var paramType = parameters[i].ParameterType;

            args[i] = ResolveParameter(paramType);
        }

        try
        {
            var result = handler.DynamicInvoke(args);

            return result as Task<IResult?> 
                ?? Task.FromResult(result as IResult);
        }
        catch (TargetParameterCountException)
        {
            throw new InvalidOperationException($"Could not resolve all parameters for method {methodInfo.Name}");
        }

        object? ResolveParameter(Type paramType)
        {
            var providedIndex = provided.FindIndex(o => o?.GetType() == paramType);
            if (providedIndex != -1)
            {
                var item = provided[providedIndex];
                provided.RemoveAt(providedIndex);
                return item;
            }

            if (paramType == typeof(CancellationToken))
                return CancellationToken.None;

            return Resolve(paramType);
        }
    }
}
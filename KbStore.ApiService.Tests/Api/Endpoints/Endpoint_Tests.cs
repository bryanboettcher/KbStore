using System.Linq.Expressions;
using System.Reflection;
using MassTransit;
using MassTransit.Testing;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NUnit.Framework;
// ReSharper disable InconsistentNaming
// ReSharper disable MemberCanBePrivate.Global

namespace KbStore.ApiService.Tests.Api.Endpoints;

#pragma warning disable CS8618
public abstract class Endpoint_Tests
{
    protected IServiceCollection Services;
    protected IServiceProvider? RootProvider;
    protected IServiceScope ScopedProvider;
    
    protected ITestHarness? Harness;

    protected Endpoint_Tests()
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
        if (Harness is IAsyncDisposable dispose)
            await dispose.DisposeAsync();

        (RootProvider as IDisposable)?.Dispose();
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

    protected IResult? Execute(Delegate handler, params object?[] inputs)
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

            return result is Task<IResult> taskResult
                ? taskResult.GetAwaiter().GetResult()
                : result as IResult;
        }
        catch (TargetParameterCountException)
        {
            throw new InvalidOperationException($"Could not resolve all parameters for method {methodInfo.Name}");
        }

        object? ResolveParameter(Type paramType)
        {
            var providedIndex = provided.FindIndex(o => o.GetType() == paramType);
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
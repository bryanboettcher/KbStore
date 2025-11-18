
// ReSharper disable InconsistentNaming
// ReSharper disable MemberCanBePrivate.Global

namespace KbStore.Tests;

using System.Diagnostics;
using MassTransit;
using MassTransit.Configuration;
using MassTransit.Saga;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

#pragma warning disable CS8618
public abstract class EventingTestBase
{
    /// <summary>
    /// Default request timeout for all test request clients.
    /// MassTransit's default is 30 seconds, but we want fast feedback in tests.
    /// </summary>
    protected static readonly RequestTimeout DefaultRequestTimeout = RequestTimeout.After(ms: 250);

    protected IServiceCollection Services;
    protected IServiceProvider? RootProvider;
    protected IServiceScope ScopedProvider;

    protected ITestHarness Harness = null!;
    protected Exception? LastException;

    private Stopwatch _fixtureStopwatch = null!;
    private Stopwatch _testStopwatch = null!;

    protected EventingTestBase()
    {
        Services = new ServiceCollection();
    }

    [OneTimeSetUp]
    public async Task InitializeOnce()
    {
        _fixtureStopwatch = Stopwatch.StartNew();
        var fixtureName = GetType().Name;
        Console.WriteLine($"[FIXTURE START] {fixtureName} - OneTimeSetUp beginning");

        await Task.CompletedTask;

        var sw = Stopwatch.StartNew();
        OnServicesCreating(Services);
        Console.WriteLine($"[FIXTURE] {fixtureName} - OnServicesCreating: {sw.ElapsedMilliseconds}ms");

        sw.Restart();
        Services.AddMassTransitTestHarness(conf =>
        {
            conf.SetTestTimeouts(
                testTimeout: TimeSpan.FromSeconds(5),
                testInactivityTimeout: TimeSpan.FromMilliseconds(100)  // Reduced from 1000ms - in-memory transport is fast
            );

            conf.SetDefaultRequestTimeout(
                timeout: RequestTimeout.After(ms:250)
            );

            OnHarnessCreating(conf);
        });
        Console.WriteLine($"[FIXTURE] {fixtureName} - AddMassTransitTestHarness: {sw.ElapsedMilliseconds}ms");

        sw.Restart();
        RootProvider = Services.BuildServiceProvider(true);
        Console.WriteLine($"[FIXTURE] {fixtureName} - BuildServiceProvider: {sw.ElapsedMilliseconds}ms");

        sw.Restart();
        Harness = RootProvider.GetRequiredService<ITestHarness>();
        await Harness.Start();
        Console.WriteLine($"[FIXTURE] {fixtureName} - Harness.Start: {sw.ElapsedMilliseconds}ms");

        Console.WriteLine($"[FIXTURE READY] {fixtureName} - OneTimeSetUp completed in {_fixtureStopwatch.ElapsedMilliseconds}ms");
    }

    [SetUp]
    public async Task Setup()
    {
        _testStopwatch = Stopwatch.StartNew();
        var testName = TestContext.CurrentContext.Test.Name;
        var fixtureName = GetType().Name;
        Console.WriteLine($"  [TEST START] {fixtureName}.{testName}");

        var sw = Stopwatch.StartNew();
        await OnPreSetup();
        Console.WriteLine($"    [TEST] OnPreSetup: {sw.ElapsedMilliseconds}ms");

        sw.Restart();
        ScopedProvider = RootProvider!.CreateScope();
        Console.WriteLine($"    [TEST] CreateScope: {sw.ElapsedMilliseconds}ms");

        sw.Restart();
        await OnPostSetup();
        Console.WriteLine($"    [TEST] OnPostSetup: {sw.ElapsedMilliseconds}ms");

        sw.Restart();
        Arrange();
        Console.WriteLine($"    [TEST] Arrange: {sw.ElapsedMilliseconds}ms");

        sw.Restart();
        LastException = null;
        try
        {
            await Act();
            Console.WriteLine($"    [TEST] Act: {sw.ElapsedMilliseconds}ms");
        }
        catch (Exception e)
        {
            LastException = e;
            Console.WriteLine($"    [TEST] Act (EXCEPTION): {sw.ElapsedMilliseconds}ms - {e.GetType().Name}");
        }
    }

    [TearDown]
    public async Task Teardown()
    {
        var sw = Stopwatch.StartNew();
        await OnPreTeardown();
        Console.WriteLine($"    [TEST] OnPreTeardown: {sw.ElapsedMilliseconds}ms");

        sw.Restart();
        await OnPostTeardown();
        Console.WriteLine($"    [TEST] OnPostTeardown: {sw.ElapsedMilliseconds}ms");

        sw.Restart();
        ScopedProvider.Dispose();
        Console.WriteLine($"    [TEST] Dispose: {sw.ElapsedMilliseconds}ms");

        var testName = TestContext.CurrentContext.Test.Name;
        var fixtureName = GetType().Name;
        Console.WriteLine($"  [TEST END] {fixtureName}.{testName} - Total: {_testStopwatch.ElapsedMilliseconds}ms");
    }

    [OneTimeTearDown]
    public async Task FinalizeOnce()
    {
        var fixtureName = GetType().Name;
        var sw = Stopwatch.StartNew();

        await Harness.Stop();
        Console.WriteLine($"[FIXTURE] {fixtureName} - Harness.Stop: {sw.ElapsedMilliseconds}ms");

        sw.Restart();
        await DisposeAsync(Harness);
        await DisposeAsync(RootProvider);
        Console.WriteLine($"[FIXTURE] {fixtureName} - Dispose: {sw.ElapsedMilliseconds}ms");

        Console.WriteLine($"[FIXTURE END] {fixtureName} - Total fixture time: {_fixtureStopwatch.ElapsedMilliseconds}ms");

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

    /// <summary>
    /// Creates a request client with the default test timeout applied.
    /// Use this instead of Harness.Bus.CreateRequestClient() to ensure fast test feedback.
    /// </summary>
    protected IRequestClient<TRequest> CreateRequestClient<TRequest>()
        where TRequest : class
    {
        return new TimeoutRequestClient<TRequest>(
            Harness.Bus.CreateRequestClient<TRequest>(),
            DefaultRequestTimeout
        );
    }

    /// <summary>
    /// Wrapper that applies default timeout to all GetResponse calls.
    /// This ensures tests fail quickly instead of waiting 30 seconds for MassTransit's default timeout.
    /// </summary>
    private class TimeoutRequestClient<TRequest> : IRequestClient<TRequest>
        where TRequest : class
    {
        private readonly IRequestClient<TRequest> _inner;
        private readonly RequestTimeout _timeout;

        public TimeoutRequestClient(IRequestClient<TRequest> inner, RequestTimeout timeout)
        {
            _inner = inner;
            _timeout = timeout;
        }

        public RequestHandle<TRequest> Create(TRequest message, CancellationToken cancellationToken = default, RequestTimeout timeout = default)
            => _inner.Create(message, cancellationToken, timeout.HasValue ? timeout : _timeout);

        public RequestHandle<TRequest> Create(object values, CancellationToken cancellationToken = default, RequestTimeout timeout = default)
            => _inner.Create(values, cancellationToken, timeout.HasValue ? timeout : _timeout);

        public async Task<Response<T>> GetResponse<T>(TRequest message, CancellationToken cancellationToken = default, RequestTimeout timeout = default)
            where T : class
            => await _inner.GetResponse<T>(message, cancellationToken, timeout.HasValue ? timeout : _timeout);

        public async Task<Response<T>> GetResponse<T>(object values, CancellationToken cancellationToken = default, RequestTimeout timeout = default)
            where T : class
            => await _inner.GetResponse<T>(values, cancellationToken, timeout.HasValue ? timeout : _timeout);

        public async Task<Response<T1, T2>> GetResponse<T1, T2>(TRequest message, CancellationToken cancellationToken = default, RequestTimeout timeout = default)
            where T1 : class
            where T2 : class
            => await _inner.GetResponse<T1, T2>(message, cancellationToken, timeout.HasValue ? timeout : _timeout);

        public async Task<Response<T1, T2>> GetResponse<T1, T2>(object values, CancellationToken cancellationToken = default, RequestTimeout timeout = default)
            where T1 : class
            where T2 : class
            => await _inner.GetResponse<T1, T2>(values, cancellationToken, timeout.HasValue ? timeout : _timeout);

        public async Task<Response<T1, T2, T3>> GetResponse<T1, T2, T3>(TRequest message, CancellationToken cancellationToken = default, RequestTimeout timeout = default)
            where T1 : class
            where T2 : class
            where T3 : class
            => await _inner.GetResponse<T1, T2, T3>(message, cancellationToken, timeout.HasValue ? timeout : _timeout);

        public async Task<Response<T1, T2, T3>> GetResponse<T1, T2, T3>(object values, CancellationToken cancellationToken = default, RequestTimeout timeout = default)
            where T1 : class
            where T2 : class
            where T3 : class
            => await _inner.GetResponse<T1, T2, T3>(values, cancellationToken, timeout.HasValue ? timeout : _timeout);

        // Overloads with RequestPipeConfiguratorCallback
        public async Task<Response<T>> GetResponse<T>(TRequest message, RequestPipeConfiguratorCallback<TRequest> callback, CancellationToken cancellationToken = default, RequestTimeout timeout = default)
            where T : class
            => await _inner.GetResponse<T>(message, callback, cancellationToken, timeout.HasValue ? timeout : _timeout);

        public async Task<Response<T>> GetResponse<T>(object values, RequestPipeConfiguratorCallback<TRequest> callback, CancellationToken cancellationToken = default, RequestTimeout timeout = default)
            where T : class
            => await _inner.GetResponse<T>(values, callback, cancellationToken, timeout.HasValue ? timeout : _timeout);

        public async Task<Response<T1, T2>> GetResponse<T1, T2>(TRequest message, RequestPipeConfiguratorCallback<TRequest> callback, CancellationToken cancellationToken = default, RequestTimeout timeout = default)
            where T1 : class
            where T2 : class
            => await _inner.GetResponse<T1, T2>(message, callback, cancellationToken, timeout.HasValue ? timeout : _timeout);

        public async Task<Response<T1, T2>> GetResponse<T1, T2>(object values, RequestPipeConfiguratorCallback<TRequest> callback, CancellationToken cancellationToken = default, RequestTimeout timeout = default)
            where T1 : class
            where T2 : class
            => await _inner.GetResponse<T1, T2>(values, callback, cancellationToken, timeout.HasValue ? timeout : _timeout);

        public async Task<Response<T1, T2, T3>> GetResponse<T1, T2, T3>(TRequest message, RequestPipeConfiguratorCallback<TRequest> callback, CancellationToken cancellationToken = default, RequestTimeout timeout = default)
            where T1 : class
            where T2 : class
            where T3 : class
            => await _inner.GetResponse<T1, T2, T3>(message, callback, cancellationToken, timeout.HasValue ? timeout : _timeout);

        public async Task<Response<T1, T2, T3>> GetResponse<T1, T2, T3>(object values, RequestPipeConfiguratorCallback<TRequest> callback, CancellationToken cancellationToken = default, RequestTimeout timeout = default)
            where T1 : class
            where T2 : class
            where T3 : class
            => await _inner.GetResponse<T1, T2, T3>(values, callback, cancellationToken, timeout.HasValue ? timeout : _timeout);
    }
}
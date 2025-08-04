namespace KbStore.ApiService.Tests;

using System.Reflection;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using NUnit.Framework;

[TestFixture]
public abstract class Endpoints_Tests
{
    protected Exception? LastException;
    
    [SetUp]
    public async Task Because()
    {
        _mocks.Clear();
        Arrange();

        try
        {
            await Act();
        }
        catch (InvalidOperationException e) when (e.InnerException is TargetParameterCountException)
        {
            Assert.Fail(e.Message);
        }
        catch (Exception e)
        {
            LastException = e;
        }
    }

    protected abstract void Arrange();
    protected abstract Task Act();

    private readonly List<object> _mocks = new();

    protected TService MockOf<TService>()
        where TService : class
    {
        var instance = Substitute.For<TService>();

        _mocks.Add(instance);

        return instance;
    }

    protected TService? Resolve<TService>()
        where TService : class
    {
        return (TService?)Resolve(typeof(TService));
    }

    protected object? Resolve(Type service)
    {
        return _mocks.FirstOrDefault(service.IsInstanceOfType);
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
        catch (TargetParameterCountException e)
        {
            throw new InvalidOperationException($"Could not resolve all parameters for method {methodInfo.Name}", e);
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
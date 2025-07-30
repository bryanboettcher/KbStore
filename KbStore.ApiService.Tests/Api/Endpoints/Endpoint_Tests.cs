

// ReSharper disable StaticMemberInGenericType

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

namespace KbStore.ApiService.Tests.Api.Endpoints;

using MassTransit;
using Microsoft.AspNetCore.Http;
using NUnit.Framework;


public abstract class Endpoint_Tests<TRequest> : TestBase
    where TRequest : class
{
    protected IResult? Output;
    protected Func<ConsumeContext<TRequest>, Task> ConsumeHandler;

    protected Endpoint_Tests()
        => ConsumeHandler = HandlerUnassigned;

    protected override void OnHarnessCreating(IBusRegistrationConfigurator conf)
    {
        conf.AddHandler<TRequest>(async ctx => await ConsumeHandler(ctx));
    }

    protected Task HandlerUnassigned(ConsumeContext<TRequest> context)
    {
        Assert.Fail($"Consumer must be defined for {typeof(TRequest)}.  Use RespondWith<T>(T message) or ResponseUnexpected()");
        return Task.CompletedTask;
    }

    protected Task CallInvalidHandler(ConsumeContext<TRequest> context)
    {
        Assert.Fail("Message should not have been published");
        return Task.CompletedTask;
    }

    protected void RespondWith<TMessage>(object message)
        where TMessage : class
    {
        ConsumeHandler = async ctx => await ctx.RespondAsync<TMessage>(message);
    }

    protected void ResponseUnexpected()
    {
        ConsumeHandler = CallInvalidHandler;
    }
}
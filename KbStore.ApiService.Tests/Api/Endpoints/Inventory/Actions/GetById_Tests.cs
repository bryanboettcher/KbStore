namespace KbStore.ApiService.Tests.Api.Endpoints.Inventory.Actions;

using ApiService.Endpoints.Inventory;
using Contracts.Domains;
using MassTransit;
using Microsoft.AspNetCore.Http.HttpResults;
using NUnit.Framework;
using Shouldly;

#pragma warning disable CS8618
[Category("Unit")]
[Category("StockItem")]
[Category("GetById")]
public abstract class GetById_Tests : Endpoint_Tests<StockItemStatusRequest>
{
    protected static readonly Guid TestId = Guid.NewGuid();

    public class When_getting_successfully : GetById_Tests
    {
        protected override void Arrange()
            => RespondWith<StockItemStatusResponse>(new { });

        protected override async Task Act()
            => Output = await Execute(StockItemEndpoints.GetById, TestId);

        [Test]
        public void It_should_return_something() => Output.ShouldNotBeNull();

        [Test]
        public void It_should_not_throw() => LastException.ShouldBeNull();

        [Test]
        public void It_should_be_successful() => Output.ShouldBeOfType<Ok<StockItemStatusResponse>>();
    }

    public class When_StockItem_is_missing : GetById_Tests
    {
        protected override void Arrange() =>
            RespondWith<StockItemFailure>(new
            {
                Message = "StockItem not found",
                FailureType = FailureType.Missing
            });

        protected override async Task Act()
            => Output = await Execute(StockItemEndpoints.GetById, TestId);

        [Test]
        public void It_should_return_not_found() => Output.ShouldBeOfType<NotFound<StockItemFailure>>();

        [Test]
        public void It_should_not_throw() => LastException.ShouldBeNull();
    }

    public class When_backend_returns_unexpected_response : GetById_Tests
    {
        public class UnexpectedResponse { }

        protected override void Arrange()
            => RespondWith<UnexpectedResponse>(new { });

        protected override async Task Act()
            => Output = await Execute(StockItemEndpoints.GetById, TestId);

        [Test]
        public void It_should_throw_request_timeout_exception() => LastException.ShouldBeOfType<RequestTimeoutException>();

        [Test]
        public void Output_should_be_null() => Output.ShouldBeNull();
    }
}
namespace KbStore.ApiService.Tests.Api.Endpoints.Inventory.Actions;

using ApiService.Endpoints.Inventory;
using KbStore.Inventory.Abstractions.Contracts;
using MassTransit;
using Microsoft.AspNetCore.Http.HttpResults;
using NUnit.Framework;
using Shouldly;

#pragma warning disable CS8618
[Category("Unit")]
[Category("StockItem")]
[Category("Delete")]
public abstract class DeleteStockItem_Tests : Endpoint_Tests<DeleteStockItemRequest>
{
    protected static readonly Guid TestId = Guid.NewGuid();

    public class When_deleting_successfully : DeleteStockItem_Tests
    {
        protected override void Arrange()
            => RespondWith<DeleteStockItemResponse>(new { });

        protected override async Task Act()
            => Output = await Execute(StockItemEndpoints.Delete, TestId);

        [Test]
        public void It_should_return_something() => Output.ShouldNotBeNull();

        [Test]
        public void It_should_not_throw() => LastException.ShouldBeNull();

        [Test]
        public void It_should_be_successful() => Output.ShouldBeOfType<Ok<DeleteStockItemResponse>>();
    }

    public class When_StockItem_is_missing : DeleteStockItem_Tests
    {
        protected override void Arrange()
            => RespondWith<DeleteStockItemResponse>(new { });

        protected override async Task Act()
            => Output = await Execute(StockItemEndpoints.Delete, TestId);

        [Test]
        public void It_should_be_successful() => Output.ShouldBeOfType<Ok<DeleteStockItemResponse>>();

        [Test]
        public void It_should_not_throw() => LastException.ShouldBeNull();
    }

    public class When_backend_returns_unexpected_response : DeleteStockItem_Tests
    {
        public class UnexpectedResponse { }

        protected override void Arrange()
            => RespondWith<UnexpectedResponse>(new { });

        protected override async Task Act()
            => Output = await Execute(StockItemEndpoints.Delete, TestId);

        [Test]
        public void It_should_throw_request_timeout_exception() => LastException.ShouldBeOfType<RequestTimeoutException>();

        [Test]
        public void Output_should_be_null() => Output.ShouldBeNull();
    }
}
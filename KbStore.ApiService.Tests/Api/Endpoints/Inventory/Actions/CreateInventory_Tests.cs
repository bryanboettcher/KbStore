namespace KbStore.ApiService.Tests.Api.Endpoints.Inventory.Actions;

using Abstractions;
using ApiService.Endpoints.Inventory;
using KbStore.Inventory.Abstractions.Contracts;
using MassTransit;
using Microsoft.AspNetCore.Http.HttpResults;
using NUnit.Framework;
using Shouldly;

#pragma warning disable CS8618
[Category("Unit")]
[Category("StockItem")]
[Category("Create")]
public abstract class CreateStockItem_Tests : Endpoint_Tests<CreateStockItemRequest>
{
    protected CreateStockItemPayload CreatePayload(
        string? partNumber = "FAST_M3X20",
        string? description = "test description",
        int stockQuantity = 1000) => new()
        {
            PartNumber = partNumber,
            Description = description,
            StockQuantity = stockQuantity
        };

    public class When_creating_successfully : CreateStockItem_Tests
    {
        protected override void Arrange()
            => RespondWith<CreateStockItemResponse>(new { });

        protected override async Task Act()
            => Output = await Execute(StockItemEndpoints.Create, CreatePayload());

        [Test]
        public void It_should_return_something() => Output.ShouldNotBeNull();

        [Test]
        public void It_should_not_throw() => LastException.ShouldBeNull();

        [Test]
        public void It_should_be_successful() => Output.ShouldBeOfType<Ok<CreateStockItemResponse>>();
    }

    public class When_payload_is_invalid : CreateStockItem_Tests
    {
        protected override void Arrange()
            => ResponseUnexpected();

        protected override async Task Act()
            => Output = await Execute(StockItemEndpoints.Create, CreatePayload(partNumber: null));

        [Test]
        public void It_should_return_bad_request() => Output.ShouldBeOfType<BadRequest>();

        [Test]
        public void It_should_not_throw() => LastException.ShouldBeNull();
    }

    public class When_backend_returns_conflict : CreateStockItem_Tests
    {
        protected override void Arrange() =>
            RespondWith<StockItemFailure>(new
            {
                Message = "Part already exists",
                FailureType = FailureType.Conflict
            });

        protected override async Task Act()
            => Output = await Execute(StockItemEndpoints.Create, CreatePayload());

        [Test]
        public void It_should_return_conflict() 
            => Output.ShouldBeOfType<Conflict<StockItemFailure>>();

        [Test]
        public void It_should_not_throw() 
            => LastException.ShouldBeNull();
    }

    public class When_backend_returns_unexpected_response : CreateStockItem_Tests
    {
        public class UnexpectedResponse { }

        protected override void Arrange()
            => RespondWith<UnexpectedResponse>(new { });

        protected override async Task Act()
            => Output = await Execute(StockItemEndpoints.Create, CreatePayload());

        [Test]
        public void It_should_throw_request_timeout_exception() => LastException.ShouldBeOfType<RequestTimeoutException>();

        [Test]
        public void Output_should_be_null() => Output.ShouldBeNull();
    }
}
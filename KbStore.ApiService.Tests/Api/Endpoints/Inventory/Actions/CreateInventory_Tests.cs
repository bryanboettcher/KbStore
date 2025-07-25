namespace KbStore.ApiService.Tests.Api.Endpoints.Inventory.Actions;

using KbStore.ApiService.Endpoints.Inventory;
using KbStore.ApiService.Tests.Api.Endpoints;
using KbStore.Contracts.Domains;
using MassTransit;
using Microsoft.AspNetCore.Http.HttpResults;
using NUnit.Framework;
using Shouldly;

#pragma warning disable CS8618
[Category("Unit")]
[Category("Inventory")]
[Category("Create")]
public abstract class CreateInventory_Tests : Endpoint_Tests<CreateInventoryRequest>
{
    protected CreateInventoryPayload CreatePayload(
        string? partNumber = "FAST_M3X20",
        string? description = "test description",
        int stockQuantity = 1000,
        InventoryStatus inventoryStatus = InventoryStatus.InStock) => new()
        {
            PartNumber = partNumber,
            Description = description,
            StockQuantity = stockQuantity,
            InventoryStatus = inventoryStatus
        };

    public class When_creating_successfully : CreateInventory_Tests
    {
        protected override void Arrange()
            => RespondWith<CreateInventoryResponse>(new { });

        protected override async Task Act()
            => Output = await Execute(InventoryEndpoints.Create, CreatePayload());

        [Test]
        public void It_should_return_something() => Output.ShouldNotBeNull();

        [Test]
        public void It_should_not_throw() => LastException.ShouldBeNull();

        [Test]
        public void It_should_be_successful() => Output.ShouldBeOfType<Ok<CreateInventoryResponse>>();
    }

    public class When_payload_is_invalid : CreateInventory_Tests
    {
        protected override void Arrange()
            => ResponseUnexpected();

        protected override async Task Act()
            => Output = await Execute(InventoryEndpoints.Create, CreatePayload(partNumber: null));

        [Test]
        public void It_should_return_bad_request() => Output.ShouldBeOfType<BadRequest>();

        [Test]
        public void It_should_not_throw() => LastException.ShouldBeNull();
    }

    public class When_backend_returns_conflict : CreateInventory_Tests
    {
        protected override void Arrange() =>
            RespondWith<InventoryFailure>(new
            {
                Message = "Part already exists",
                FailureType = FailureType.Conflict
            });

        protected override async Task Act()
            => Output = await Execute(InventoryEndpoints.Create, CreatePayload());

        [Test]
        public void It_should_return_conflict() 
            => Output.ShouldBeOfType<Conflict<InventoryFailure>>();

        [Test]
        public void It_should_not_throw() 
            => LastException.ShouldBeNull();
    }

    public class When_backend_returns_unexpected_response : CreateInventory_Tests
    {
        public class UnexpectedResponse { }

        protected override void Arrange()
            => RespondWith<UnexpectedResponse>(new { });

        protected override async Task Act()
            => Output = await Execute(InventoryEndpoints.Create, CreatePayload());

        [Test]
        public void It_should_throw_request_timeout_exception() => LastException.ShouldBeOfType<RequestTimeoutException>();

        [Test]
        public void Output_should_be_null() => Output.ShouldBeNull();
    }
}
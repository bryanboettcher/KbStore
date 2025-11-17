# Implementation Patterns

This document provides reference implementations and templates for common patterns in the KbStore codebase.

## Table of Contents

1. [State Machine Pattern](#state-machine-pattern)
2. [Service Layer Pattern](#service-layer-pattern)
3. [API Endpoint Pattern](#api-endpoint-pattern)
4. [Testing Pattern](#testing-pattern)
5. [Event Choreography Pattern](#event-choreography-pattern)
6. [Exception Handling Pattern](#exception-handling-pattern)

---

## State Machine Pattern

State machines are the core domain pattern. They manage entity lifecycle and enforce business rules through state transitions.

### When to Use
- Managing entities with distinct lifecycle states
- Enforcing business rules that vary by state
- Publishing domain events for inter-domain communication
- Implementing saga patterns for distributed transactions

### Entity Structure

```csharp
// File: KbStore.Catalog/Domains/{Entity}/{Entity}Entity.cs
namespace KbStore.Catalog.Domains.Products;

using MassTransit;

public class ProductEntity : SagaStateMachineInstance
{
    // Primary key - required by MassTransit
    public Guid CorrelationId { get; set; }

    // Optimistic concurrency - required for saga repositories
    public uint RowVersion { get; set; }

    // Current state - mapped to integer in database
    public int CurrentState { get; set; }

    // Domain properties
    public string Sku { get; set; } = default!;
    public string? Name { get; set; }
    public int Quantity { get; set; }
    public int? StockQuantity { get; set; }
    public int? StockThreshold { get; set; }
    public TimeSpan? LeadTime { get; set; }

    // Timestamps
    public DateTimeOffset CreatedOn { get; set; }
    public DateTimeOffset? UpdatedOn { get; set; }

    // Computed properties
    public bool IsEnabled => CurrentState == ProductStates.Enabled;
    public bool IsAvailable => IsEnabled && IsStocked;
    public bool IsStocked { get; set; } = true;

    // Foreign keys to other entities
    public Guid? InventoryId { get; set; }

    // Request state tracking (for request/response patterns)
    public Guid? InventoryStatusId { get; set; }
}
```

### State Machine Implementation

```csharp
// File: KbStore.Catalog/Domains/{Entity}/{Entity}StateMachine.cs
namespace KbStore.Catalog.Domains.Products;

using KbStore.Catalog.Abstractions.Contracts;
using KbStore.Catalog.Abstractions.Exceptions;
using MassTransit;

public sealed class ProductStateMachine : MassTransitStateMachine<ProductEntity>
{
    public ProductStateMachine(ILogger<ProductStateMachine> logger)
    {
        // 1. Define state property mapping
        InstanceState(m => m.CurrentState,
            Enabled,      // Maps to ProductStates.Enabled constant
            Disabled,     // Maps to ProductStates.Disabled constant
            Discontinued  // Maps to ProductStates.Discontinued constant
        );

        // 2. Configure request/response pattern (if needed)
        Request(() => InventoryStatus, s => s.InventoryStatusId, c =>
        {
            c.Timeout = TimeSpan.FromSeconds(5);
            c.ClearRequestIdOnFaulted = true;
        });

        // 3. Configure events
        // Event with custom correlation and ID generation
        Event(() => Created, e => e
            .CorrelateBy((saga, context) => saga.Sku == context.Message.Sku)
            .SelectId(_ => NewId.NextSequentialGuid())
        );

        // Event with missing instance handling
        Event(() => StatusRequested, e => e
            .CorrelateById(c => c.Message.ProductId)
            .OnMissingInstance(b => b.Execute(c =>
                throw new ProductNotFoundException(c.Message.ProductId)))
        );

        // Events using helper method for consistent configuration
        Event(() => NameUpdated, ConfigureEvent);
        Event(() => Deleted, ConfigureEvent);

        // Events from other domains (correlation by foreign key)
        Event(() => InventoryQuantityChanged, e => e
            .CorrelateBy((saga, context) => saga.InventoryId == context.Message.InventoryId)
        );

        // 4. Define initial state behavior
        Initially(
            When(Created)
                .Then(SetProperties)                    // Set entity properties
                .IfElse(                                // Conditional branching
                    ctx => ctx.Saga.InventoryId != null,
                    thenBranch => thenBranch
                        .TransitionTo(InventoryStatus.Pending)
                        .Request(InventoryStatus, c => c.Init<InventoryStatusRequest>(new {
                            c.Saga.InventoryId
                        })),
                    elseBranch => elseBranch
                        .TransitionTo(Enabled)
                        .PublishAsync(Message<ProductCreated>)  // Publish domain event
                )
                .RespondAsync(Message<CreateProductResponse>)   // Respond to request
        );

        // 5. Handle request/response states
        During(InventoryStatus.Pending,
            When(InventoryStatus.Completed)
                .Then(ctx => context.Saga.StockQuantity = ctx.Message.StockQuantity)
                .TransitionTo(Enabled)
                .PublishAsync(Message<ProductCreated>),

            When(InventoryStatus.Faulted)
                .Then(ctx => logger.LogWarning("Inventory check failed"))
                .TransitionTo(Disabled)
                .PublishAsync(Message<ProductCreated>),

            When(InventoryStatus.TimeoutExpired)
                .Then(ctx => logger.LogWarning("Inventory check timed out"))
                .TransitionTo(Disabled)
                .PublishAsync(Message<ProductCreated>)
        );

        // 6. Define state-specific behavior
        During(Enabled,
            // Handle duplicate creation attempts
            When(Created)
                .Then(context => throw ProductConflictException.DuplicateSku(context.Message.Sku)),

            // Handle update commands
            When(NameUpdated)
                .Then(context => context.Saga.Name = context.Message.Name)
                .Then(UpdateTimestamp)
                .RespondAsync(Message<UpdateProductResponse>)
                .PublishAsync(Message<ProductNameUpdated>),

            // Handle state transitions
            When(DisableRequested)
                .TransitionTo(Disabled)
                .Then(UpdateTimestamp)
                .RespondAsync(Message<DisableProductResponse>)
                .PublishAsync(Message<ProductDisabled>),

            // React to events from other domains
            When(InventoryQuantityChanged)
                .Then(UpdateStockQuantity)
                .PublishAsync(Message<ProductAvailabilityChanged>),

            // Finalize (remove from saga repository)
            When(InventoryDeleted)
                .Finalize()
                .PublishAsync(Message<ProductAvailabilityChanged>)
        );

        During(Discontinued,
            // Allow final deletion
            When(Deleted)
                .RespondAsync(Message<DeleteProductResponse>)
                .PublishAsync(Message<ProductDeleted>)
                .Finalize(),

            // Ignore certain events
            Ignore(InventoryDiscontinued),

            // Reject operations on discontinued entities
            When(NameUpdated)
                .Then(context => throw ProductStateException.CannotModifyDiscontinuedProduct(
                    context.Saga.CorrelationId, "UpdateName"))
        );

        // 7. Handle events in any state
        DuringAny(
            When(StatusRequested)
                .RespondAsync(Message<ProductStatusResponse>)
        );

        // 8. Auto-cleanup finalized instances
        SetCompletedWhenFinalized();
    }

    // Event properties (auto-implemented by MassTransit)
    public Event<CreateProductRequest> Created { get; }
    public Event<UpdateProductNameRequest> NameUpdated { get; }
    public Event<DisableProductRequest> DisableRequested { get; }
    public Event<DeleteProductRequest> Deleted { get; }
    public Event<ProductStatusRequest> StatusRequested { get; }

    // Events from other domains
    public Event<InventoryQuantityChanged> InventoryQuantityChanged { get; }
    public Event<InventoryDeleted> InventoryDeleted { get; }
    public Event<InventoryDiscontinued> InventoryDiscontinued { get; }

    // Request/response tracking
    public Request<ProductEntity, InventoryStatusRequest, InventoryStatusResponse> InventoryStatus { get; }

    // State properties
    public State Enabled { get; }
    public State Disabled { get; }
    public State Discontinued { get; }

    // Helper methods
    private static void SetProperties(BehaviorContext<ProductEntity, CreateProductRequest> context)
    {
        context.Saga.Sku = context.Message.Sku;
        context.Saga.Name = context.Message.Name;
        context.Saga.Quantity = context.Message.Quantity;
        context.Saga.CreatedOn = context.Message.Timestamp;
        context.Saga.UpdatedOn = context.Message.Timestamp;
    }

    private static void UpdateTimestamp(BehaviorContext<ProductEntity, ProductCommand> context)
    {
        context.Saga.UpdatedOn = context.Message.Timestamp;
    }

    private static void UpdateStockQuantity(BehaviorContext<ProductEntity, InventoryModel> context)
    {
        context.Saga.StockQuantity = context.Message.StockQuantity;
        context.Saga.IsStocked = context.Saga.StockQuantity >= context.Saga.StockThreshold;
    }

    // Message factory helper
    private static Task<SendTuple<TMessage>> Message<TMessage>(BehaviorContext<ProductEntity> context)
        where TMessage : class, ProductModel
        => context.Init<TMessage>(new
        {
            ProductId = context.Saga.CorrelationId,
            context.Saga.Sku,
            context.Saga.Name,
            context.Saga.Quantity,
            context.Saga.IsEnabled,
            context.Saga.IsAvailable,
            context.Saga.CreatedOn,
            context.Saga.UpdatedOn
        });

    // Event configuration helper
    private static void ConfigureEvent<TMessage>(IEventCorrelationConfigurator<ProductEntity, TMessage> conf)
        where TMessage : class, ProductCommand
    {
        conf.CorrelateById(s => s.Message.ProductId);
        conf.OnMissingInstance(b => b.ExecuteAsync(c =>
            throw new ProductNotFoundException(c.Message.ProductId)));
    }
}
```

### EF Core Mapping

```csharp
// File: KbStore.Catalog/Domains/{Entity}/{Entity}SagaMap.cs
namespace KbStore.Catalog.Domains.Products;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MassTransit;

public class ProductSagaMap : SagaClassMap<ProductEntity>
{
    protected override void Configure(EntityTypeBuilder<ProductEntity> entity, ModelBuilder model)
    {
        // Primary key
        entity.HasKey(e => e.CorrelationId);

        // Indexes for queries
        entity.HasIndex(e => e.Sku).IsUnique();
        entity.HasIndex(e => e.CurrentState);
        entity.HasIndex(e => e.InventoryId);

        // Row version for optimistic concurrency (PostgreSQL xid type)
        entity.Property(e => e.RowVersion)
            .IsRowVersion()
            .HasColumnType("xid");

        // Required fields
        entity.Property(e => e.Sku).IsRequired().HasMaxLength(100);
        entity.Property(e => e.Name).HasMaxLength(200);

        // Computed/ignored properties
        entity.Ignore(e => e.IsEnabled);
        entity.Ignore(e => e.IsAvailable);
    }
}
```

### Common Pitfalls

1. **Forgetting to configure event correlation** - Every event must have correlation configured or it won't route to the correct saga instance
2. **Not handling missing instances** - Use `OnMissingInstance()` to provide clear errors when saga doesn't exist
3. **Ignoring optimistic concurrency** - Always use `RowVersion` to prevent lost updates
4. **Publishing events without TransitionTo** - Events should typically be published AFTER state transitions
5. **Not using Finalize()** - Sagas accumulate in database unless finalized

---

## Service Layer Pattern

The service layer wraps state machines with simple awaitable methods, hiding MassTransit complexity.

### When to Use
- Exposing domain operations to API endpoints
- Providing clean interface for testing
- Converting MassTransit faults to domain exceptions
- Performing validation before sending commands

### Command Service Interface

```csharp
// File: KbStore.Catalog.Abstractions/Services/I{Entity}CommandService.cs
namespace KbStore.Catalog.Abstractions.Services;

using Contracts;

public interface IProductCommandService
{
    Task<ProductModel> CreateAsync(
        string sku,
        string? name,
        ProductDimensions? dimensions,
        int quantity,
        Guid? inventoryId,
        int? stockThreshold,
        TimeSpan? leadTime,
        CancellationToken cancellationToken = default);

    Task<ProductModel> UpdateNameAsync(
        Guid productId,
        string? name,
        CancellationToken cancellationToken = default);

    Task<ProductModel> EnableAsync(
        Guid productId,
        CancellationToken cancellationToken = default);

    Task<ProductModel> DisableAsync(
        Guid productId,
        CancellationToken cancellationToken = default);

    Task<ProductModel> DeleteAsync(
        Guid productId,
        CancellationToken cancellationToken = default);

    Task<ProductModel> GetAsync(
        Guid productId,
        CancellationToken cancellationToken = default);
}
```

### Command Service Implementation

```csharp
// File: KbStore.Catalog.Services/MassTransit{Entity}CommandService.cs
namespace KbStore.Catalog.Services;

using Abstractions.Contracts;
using Abstractions.Exceptions;
using Abstractions.Services;
using Extensions;
using MassTransit;

public class MassTransitProductCommandService : IProductCommandService
{
    private readonly IClientFactory _clientFactory;
    private readonly Func<DateTimeOffset> _now;

    public MassTransitProductCommandService(IClientFactory clientFactory, Func<DateTimeOffset> now)
    {
        _clientFactory = clientFactory ?? throw new ArgumentNullException(nameof(clientFactory));
        _now = now ?? throw new ArgumentNullException(nameof(now));
    }

    public async Task<ProductModel> CreateAsync(
        string sku,
        string? name,
        ProductDimensions? dimensions,
        int quantity,
        Guid? inventoryId,
        int? stockThreshold,
        TimeSpan? leadTime,
        CancellationToken cancellationToken = default)
    {
        // Validation before sending command
        if (string.IsNullOrWhiteSpace(sku))
            throw new ProductValidationException("SKU must have a value");

        if (string.IsNullOrWhiteSpace(name))
            throw new ProductValidationException("Name must have a value");

        if (quantity <= 0)
            throw new ProductValidationException("Quantity must be a positive value");

        if (stockThreshold < 0)
            throw ProductValidationException.InvalidStockThreshold(stockThreshold);

        // Create request client
        var client = _clientFactory.CreateRequestClient<CreateProductRequest>();

        try
        {
            // Send command and await response
            var response = await client.GetResponse<CreateProductResponse>(new
            {
                Sku = sku,
                Name = name,
                Dimensions = dimensions,
                Quantity = quantity,
                InventoryId = inventoryId,
                StockThreshold = stockThreshold,
                LeadTime = leadTime,
                Timestamp = _now()
            }, cancellationToken).ConfigureAwait(false);

            return response.Message;
        }
        catch (RequestFaultException e)
        {
            // Convert MassTransit fault to domain exception
            throw e.ToProductException();
        }
    }

    public async Task<ProductModel> UpdateNameAsync(
        Guid productId,
        string? name,
        CancellationToken cancellationToken = default)
    {
        if (productId == Guid.Empty)
            throw new ArgumentException("ProductId must be set", nameof(productId));

        if (string.IsNullOrWhiteSpace(name))
            throw new ProductValidationException("Name must have a value");

        var client = _clientFactory.CreateRequestClient<UpdateProductNameRequest>();

        try
        {
            var response = await client.GetResponse<UpdateProductResponse>(new
            {
                ProductId = productId,
                Name = name,
                Timestamp = _now()
            }, cancellationToken).ConfigureAwait(false);

            return response.Message;
        }
        catch (RequestFaultException e)
        {
            throw e.ToProductException();
        }
    }

    // ... other methods follow same pattern
}
```

### Query Service Interface

```csharp
// File: KbStore.Catalog.Abstractions/Services/I{Entity}QueryService.cs
namespace KbStore.Catalog.Abstractions.Services;

using Contracts;

public interface IProductQueryService
{
    Task<PaginatedResponse<ProductModel>> SearchAsync(
        ProductPaginatedQuery query,
        CancellationToken cancellationToken = default);
}
```

### Query Service Implementation

```csharp
// File: KbStore.Catalog.Services/DbContext{Entity}QueryService.cs
namespace KbStore.Catalog.Services;

using Abstractions.Contracts;
using Abstractions.Services;
using Infrastructure;
using Microsoft.EntityFrameworkCore;

public class DbContextProductQueryService : IProductQueryService
{
    private readonly ApplicationDbContext _context;

    public DbContextProductQueryService(ApplicationDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<PaginatedResponse<ProductModel>> SearchAsync(
        ProductPaginatedQuery query,
        CancellationToken cancellationToken = default)
    {
        var queryable = _context.Products.AsQueryable();

        // Apply filters
        if (!string.IsNullOrWhiteSpace(query.Sku))
            queryable = queryable.Where(p => p.Sku.Contains(query.Sku));

        if (query.IsEnabled.HasValue)
            queryable = queryable.Where(p => p.IsEnabled == query.IsEnabled.Value);

        // Get total count
        var totalCount = await queryable.CountAsync(cancellationToken);

        // Apply pagination
        var items = await queryable
            .OrderBy(p => p.Sku)
            .Skip(query.Skip)
            .Take(query.Take)
            .Select(p => new ProductModel
            {
                ProductId = p.CorrelationId,
                Sku = p.Sku,
                Name = p.Name,
                Quantity = p.Quantity,
                IsEnabled = p.IsEnabled,
                IsAvailable = p.IsAvailable,
                CreatedOn = p.CreatedOn,
                UpdatedOn = p.UpdatedOn
            })
            .ToListAsync(cancellationToken);

        return new PaginatedResponse<ProductModel>
        {
            Items = items,
            TotalCount = totalCount,
            Page = query.Page,
            PageSize = query.PageSize
        };
    }
}
```

### Service Registration

```csharp
// File: KbStore.Catalog.Services/Extensions/ServiceCollectionExtensions.cs
namespace KbStore.Catalog.Services.Extensions;

using Abstractions.Services;
using Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCatalogServices(this IServiceCollection services)
    {
        // Register time provider
        services.AddSingleton<Func<DateTimeOffset>>(() => DateTimeOffset.UtcNow);

        // Register command services
        services.AddScoped<IProductCommandService, MassTransitProductCommandService>();
        services.AddScoped<IInventoryCommandService, MassTransitInventoryCommandService>();

        // Register query services
        services.AddScoped<IProductQueryService, DbContextProductQueryService>();
        services.AddScoped<IInventoryQueryService, DbContextInventoryQueryService>();

        return services;
    }
}
```

### Common Pitfalls

1. **Not validating before sending commands** - Validate in service layer to provide clear errors
2. **Forgetting to catch RequestFaultException** - Always convert to domain exceptions
3. **Using async void** - All service methods should return Task
4. **Not injecting time provider** - Use `Func<DateTimeOffset>` for testable timestamps
5. **Querying via state machine** - Use DbContext directly for queries, not request/response

---

## API Endpoint Pattern

API endpoints are thin delegates to service layer, using ASP.NET Core minimal APIs.

### When to Use
- Exposing domain operations over HTTP
- Providing RESTful interface for clients
- Handling HTTP-specific concerns (validation, status codes)

### Endpoint Implementation

```csharp
// File: KbStore.ApiService/Endpoints/Catalog/{Entity}Endpoints.cs
namespace KbStore.ApiService.Endpoints.Catalog;

using Abstractions;
using KbStore.Catalog.Abstractions.Contracts;
using KbStore.Catalog.Abstractions.Services;
using Microsoft.AspNetCore.Mvc;

public static class ProductEndpoints
{
    [ProducesResponseType<ProductModel>(200)]
    [ProducesResponseType<ProblemDetails>(400)]
    [ProducesResponseType<ProblemDetails>(409)]
    [ProducesResponseType<ProblemDetails>(500)]
    public static async Task<IResult> Create(
        [FromBody] CreateProductPayload payload,
        [FromServices] IProductCommandService commandService,
        CancellationToken cancellationToken)
    {
        // Validate payload
        if (!payload.IsValid())
            return Results.BadRequest("Invalid payload");

        // Delegate to service layer
        var result = await commandService.CreateAsync(
            payload.Sku!,
            payload.Name,
            payload.Dimensions,
            payload.Quantity,
            payload.InventoryId,
            payload.StockThreshold,
            payload.LeadTime,
            cancellationToken
        ).ConfigureAwait(false);

        return Results.Ok(result);
    }

    [ProducesResponseType<ProductModel>(200)]
    [ProducesResponseType<ProblemDetails>(404)]
    [ProducesResponseType<ProblemDetails>(500)]
    public static async Task<IResult> GetById(
        [FromRoute] Guid id,
        [FromServices] IProductCommandService commandService,
        CancellationToken cancellationToken = default)
    {
        var result = await commandService.GetAsync(id, cancellationToken)
            .ConfigureAwait(false);

        return Results.Ok(result);
    }

    [ProducesResponseType<PaginatedResponse<ProductModel>>(200)]
    [ProducesResponseType<ProblemDetails>(400)]
    [ProducesResponseType<ProblemDetails>(500)]
    public static async Task<IResult> GetAll(
        [AsParameters] ProductPaginatedQuery pagination,
        [FromServices] IProductQueryService queryService,
        CancellationToken cancellationToken = default)
    {
        var result = await queryService.SearchAsync(pagination, cancellationToken)
            .ConfigureAwait(false);

        return Results.Ok(result);
    }

    [ProducesResponseType<ProductModel>(200)]
    [ProducesResponseType<ProblemDetails>(400)]
    [ProducesResponseType<ProblemDetails>(404)]
    [ProducesResponseType<ProblemDetails>(500)]
    public static async Task<IResult> UpdateName(
        [FromRoute] Guid id,
        [FromBody] string? name,
        [FromServices] IProductCommandService commandService,
        CancellationToken cancellationToken = default)
    {
        var result = await commandService.UpdateNameAsync(id, name, cancellationToken)
            .ConfigureAwait(false);

        return Results.Ok(result);
    }

    [ProducesResponseType<ProductModel>(200)]
    [ProducesResponseType<ProblemDetails>(404)]
    [ProducesResponseType<ProblemDetails>(500)]
    public static async Task<IResult> Delete(
        [FromRoute] Guid id,
        [FromServices] IProductCommandService commandService,
        CancellationToken cancellationToken = default)
    {
        var result = await commandService.DeleteAsync(id, cancellationToken)
            .ConfigureAwait(false);

        return Results.Ok(result);
    }

    // Route registration
    public static void MapTo(WebApplication app)
    {
        var group = app.MapGroup("/products")
            .WithTags("Products")
            .WithOpenApi();

        // Create
        group.MapPost("", Create)
            .WithName("CreateProduct")
            .WithSummary("Create a new product");

        // Read
        group.MapGet("{id:guid}", GetById)
            .WithName("GetProductById")
            .WithSummary("Get product by ID");

        group.MapGet("", GetAll)
            .WithName("GetAllProducts")
            .WithSummary("Get all products with pagination");

        // Update operations
        group.MapPatch("{id:guid}/name", UpdateName)
            .WithName("UpdateProductName")
            .WithSummary("Update product name");

        // Delete
        group.MapDelete("{id:guid}", Delete)
            .WithName("DeleteProduct")
            .WithSummary("Delete a product");
    }
}

// Payload classes
public class CreateProductPayload
{
    public string? Sku { get; set; }
    public string? Name { get; set; }
    public ProductDimensions? Dimensions { get; set; }
    public int Quantity { get; set; }
    public Guid? InventoryId { get; set; }
    public int? StockThreshold { get; set; }
    public TimeSpan? LeadTime { get; set; }

    public bool IsValid()
        => !string.IsNullOrWhiteSpace(Sku)
        && (StockThreshold == null || StockThreshold >= 0)
        && (LeadTime == null || LeadTime.Value.TotalSeconds >= 0);
}
```

### Endpoint Registration

```csharp
// File: KbStore.ApiService/Program.cs
var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddNpgsqlDbContext<ApplicationDbContext>("catalog");
builder.Services.AddCatalogServices();

// Add OpenAPI/Scalar
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

var app = builder.Build();

app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

// Register endpoint groups
app.MapEndpoints();

await app.RunAsync();
```

```csharp
// File: KbStore.ApiService/Endpoints/WebApplicationExtensions.cs
namespace KbStore.ApiService.Endpoints;

using Catalog;

public static class WebApplicationExtensions
{
    public static void MapEndpoints(this WebApplication app)
    {
        ProductEndpoints.MapTo(app);
        InventoryEndpoints.MapTo(app);
        // Future: StorefrontEndpoints.MapTo(app);
    }
}
```

### Common Pitfalls

1. **Putting business logic in endpoints** - Endpoints should only handle HTTP concerns
2. **Not using CancellationToken** - Always pass cancellation token to service calls
3. **Returning exceptions as 200 OK** - Let exception middleware handle error responses
4. **Not validating payloads** - Validate before calling service layer
5. **Using route attributes instead of MapGroup** - Use minimal API patterns consistently

---

## Testing Pattern

Tests use the EventingTestBase infrastructure with MassTransit Test Harness for state machine testing.

### When to Use
- Testing state machine behavior
- Testing service layer logic
- Verifying event publishing
- Ensuring business rules are enforced

### State Machine Test Structure

```csharp
// File: KbStore.Catalog.Tests/Domains/{Entity}/{Entity}_{Operation}.cs
namespace KbStore.Catalog.Tests.Domains.Products;

using KbStore.Catalog.Abstractions.Contracts;
using KbStore.Catalog.Domains.Products;
using MassTransit;
using MassTransit.Testing;
using NUnit.Framework;
using Shouldly;

[TestFixture]
public class Product_Create : StateMachine_Tests<ProductStateMachine, ProductEntity>
{
    protected IRequestClient<CreateProductRequest> Client = null!;
    protected Response<CreateProductResponse> Response = null!;

    // Arrange: Setup test dependencies and state
    protected override void Arrange()
    {
        Client = Harness.Bus.CreateRequestClient<CreateProductRequest>();
    }

    // Act: Execute the operation under test
    protected override async Task Act()
    {
        Response = await Client.GetResponse<CreateProductResponse>(new
        {
            Sku = "TEST_SKU",
            Name = "Test Product",
            Quantity = 10,
            InventoryId = (Guid?)null,
            StockThreshold = (int?)null,
            LeadTime = (TimeSpan?)null,
            Timestamp = Now
        });
    }

    // Test: Assertions
    public class When_creating_new_product : Product_Create
    {
        [Test]
        public async Task It_should_create_successfully() => await Assert.MultipleAsync(async () =>
        {
            // No exception thrown
            LastException.ShouldBeNull();

            // Response received
            Response.ShouldNotBeNull();
            Response.Message.Sku.ShouldBe("TEST_SKU");
            Response.Message.Name.ShouldBe("Test Product");

            // Saga instance created
            var sagaId = Response.Message.ProductId;
            SagaHarness.Sagas.Contains(sagaId).ShouldSatisfyAllConditions(saga =>
            {
                saga.ShouldNotBeNull();
                saga.CorrelationId.ShouldBe(sagaId);
                saga.CurrentState.ShouldBe(ProductStates.Enabled);
                saga.Sku.ShouldBe("TEST_SKU");
                saga.Name.ShouldBe("Test Product");
                saga.Quantity.ShouldBe(10);
            });

            // Event published
            (await Harness.Published.Any<ProductCreated>()).ShouldBeTrue();
        });
    }

    public class When_creating_duplicate_sku : Product_Create
    {
        protected override void Arrange()
        {
            base.Arrange();

            // Setup existing saga with same SKU
            Harness.AddOrUpdateSagaInstance<ProductEntity>(ExistingId, entity =>
            {
                entity.CurrentState = ProductStates.Enabled;
                entity.Sku = "TEST_SKU";
                entity.Name = "Existing Product";
                entity.Quantity = 5;
            });
        }

        [Test]
        public void It_should_throw_conflict_exception()
        {
            LastException.ShouldNotBeNull();
            LastException.ShouldBeOfType<ProductConflictException>();
            LastException.Message.ShouldContain("TEST_SKU");
        }
    }

    public class When_creating_with_inventory_link : Product_Create
    {
        private readonly Guid _inventoryId = Guid.NewGuid();

        protected override void Arrange()
        {
            base.Arrange();

            // Setup inventory saga to respond to status request
            Harness.AddOrUpdateSagaInstance<InventoryEntity>(_inventoryId, entity =>
            {
                entity.CurrentState = InventoryStates.Available;
                entity.PartNumber = "PART_123";
                entity.StockQuantity = 100;
            });
        }

        protected override async Task Act()
        {
            Response = await Client.GetResponse<CreateProductResponse>(new
            {
                Sku = "TEST_SKU",
                Name = "Test Product",
                Quantity = 10,
                InventoryId = _inventoryId,
                StockThreshold = 5,
                LeadTime = (TimeSpan?)null,
                Timestamp = Now
            });
        }

        [Test]
        public async Task It_should_link_to_inventory() => await Assert.MultipleAsync(async () =>
        {
            LastException.ShouldBeNull();

            var sagaId = Response.Message.ProductId;
            SagaHarness.Sagas.Contains(sagaId).ShouldSatisfyAllConditions(saga =>
            {
                saga.InventoryId.ShouldBe(_inventoryId);
                saga.StockQuantity.ShouldBe(100);
                saga.IsStocked.ShouldBeTrue();
            });

            // Verify inventory status was requested
            (await Harness.Sent.Any<InventoryStatusRequest>()).ShouldBeTrue();
        });
    }
}
```

### Service Layer Test Structure

```csharp
// File: KbStore.Catalog.Tests/Services/{Entity}CommandService_*.cs
namespace KbStore.Catalog.Tests.Services;

using KbStore.Catalog.Abstractions.Exceptions;
using KbStore.Catalog.Abstractions.Services;
using NUnit.Framework;
using Shouldly;

[TestFixture]
public class ProductCommandService_Create : CommandService_Tests<IProductCommandService>
{
    protected override async Task Act()
    {
        await Subject.CreateAsync(
            sku: "TEST_SKU",
            name: "Test Product",
            dimensions: null,
            quantity: 10,
            inventoryId: null,
            stockThreshold: null,
            leadTime: null
        );
    }

    public class When_creating_with_valid_data : ProductCommandService_Create
    {
        [Test]
        public void It_should_succeed()
        {
            LastException.ShouldBeNull();
        }
    }

    public class When_creating_with_empty_sku : ProductCommandService_Create
    {
        protected override async Task Act()
        {
            await Subject.CreateAsync(
                sku: "",
                name: "Test Product",
                dimensions: null,
                quantity: 10,
                inventoryId: null,
                stockThreshold: null,
                leadTime: null
            );
        }

        [Test]
        public void It_should_throw_validation_exception()
        {
            LastException.ShouldNotBeNull();
            LastException.ShouldBeOfType<ProductValidationException>();
            LastException.Message.ShouldContain("SKU");
        }
    }
}
```

### Test Base Classes

```csharp
// File: KbStore.Tests/EventingTestBase.cs
namespace KbStore.Tests;

using MassTransit.Testing;
using NUnit.Framework;

public abstract class EventingTestBase
{
    protected ITestHarness Harness { get; private set; } = default!;
    protected DateTimeOffset Now { get; private set; }
    protected Guid ExistingId { get; private set; }
    protected Exception? LastException { get; private set; }

    [SetUp]
    public async Task SetUp()
    {
        Now = DateTimeOffset.UtcNow;
        ExistingId = Guid.NewGuid();

        // Create test harness
        Harness = new InMemoryTestHarness();

        // Configure test harness with state machines
        ConfigureHarness(Harness);

        await Harness.Start();

        try
        {
            Arrange();
            await Act();
        }
        catch (Exception e)
        {
            LastException = e;
        }
    }

    [TearDown]
    public async Task TearDown()
    {
        await Harness.Stop();
    }

    protected abstract void ConfigureHarness(ITestHarness harness);
    protected virtual void Arrange() { }
    protected abstract Task Act();
}
```

### Common Pitfalls

1. **Not using async/await in tests** - Test methods should use async/await properly
2. **Asserting before Act completes** - Always await Act() before assertions
3. **Not checking LastException** - Check for exceptions in success scenarios
4. **Not cleaning up test harness** - Always call TearDown
5. **Testing implementation details** - Test behavior, not internal state

---

## Event Choreography Pattern

**Status**: PLANNED BUT NOT IMPLEMENTED - This pattern is documented for future implementation.

### When to Use (Future)
- Coordinating workflows across bounded contexts
- Reacting to domain events from other contexts
- Maintaining denormalized read models
- Implementing eventual consistency

### Event Publishing (Implemented)

Events are published from state machines:

```csharp
// In state machine
When(InventoryQuantityChanged)
    .Then(UpdateStockQuantity)
    .PublishAsync(Message<ProductAvailabilityChanged>);
```

### Event Consumer (Planned)

```csharp
// File: KbStore.ApiService/Consumers/Catalog/ProductAvailabilityConsumer.cs
namespace KbStore.ApiService.Consumers.Catalog;

using KbStore.Catalog.Abstractions.Contracts;
using KbStore.Storefront.Abstractions.Services;  // Future
using MassTransit;

public class ProductAvailabilityConsumer : IConsumer<ProductAvailabilityChanged>
{
    private readonly IStorefrontService _storefrontService;
    private readonly ILogger<ProductAvailabilityConsumer> _logger;

    public ProductAvailabilityConsumer(
        IStorefrontService storefrontService,
        ILogger<ProductAvailabilityConsumer> logger)
    {
        _storefrontService = storefrontService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ProductAvailabilityChanged> context)
    {
        _logger.LogInformation(
            "Product availability changed: {ProductId}, Available: {IsAvailable}",
            context.Message.ProductId,
            context.Message.IsAvailable);

        try
        {
            // Orchestration logic - coordinate across domains
            await _storefrontService.UpdateAvailabilityAsync(
                productId: context.Message.ProductId,
                isAvailable: context.Message.IsAvailable,
                stockQuantity: context.Message.StockQuantity,
                cancellationToken: context.CancellationToken
            );

            _logger.LogInformation(
                "Storefront updated for product {ProductId}",
                context.Message.ProductId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to update storefront for product {ProductId}",
                context.Message.ProductId);
            throw;
        }
    }
}
```

### Consumer Registration (Planned)

```csharp
// File: KbStore.ApiService/Extensions/HostBuilderExtensions.cs
public static class HostBuilderExtensions
{
    public static IHostApplicationBuilder AddMassTransit(this IHostApplicationBuilder builder)
    {
        builder.Services.AddMassTransit(x =>
        {
            // Register consumers
            x.AddConsumer<ProductAvailabilityConsumer>();
            x.AddConsumer<InventoryQuantityChangedConsumer>();

            x.UsingRabbitMq((context, cfg) =>
            {
                var configuration = context.GetRequiredService<IConfiguration>();
                var connectionString = configuration.GetConnectionString("queue");

                cfg.Host(new Uri(connectionString!));
                cfg.ConfigureEndpoints(context);
            });
        });

        return builder;
    }
}
```

### Common Pitfalls (Future)

1. **Not handling consumer failures** - Implement retry policies and error handling
2. **Creating circular dependencies** - Keep event flow unidirectional
3. **Not correlating entities** - Use same Guid across domains
4. **Putting domain logic in consumers** - Consumers orchestrate, domains implement
5. **Not considering eventual consistency** - Design for async processing

---

## Exception Handling Pattern

Domain exceptions provide clear errors with context-specific information.

### When to Use
- Representing domain rule violations
- Providing actionable error messages
- Converting framework exceptions to domain exceptions

### Exception Definitions

```csharp
// File: KbStore.Catalog.Abstractions/Exceptions/ProductValidationException.cs
namespace KbStore.Catalog.Abstractions.Exceptions;

public class ProductValidationException : Exception
{
    public ProductValidationException(string message) : base(message)
    {
    }

    public static ProductValidationException InvalidStockThreshold(int? value)
        => new($"Stock threshold must be greater than or equal to zero, but was {value}");

    public static ProductValidationException InvalidLeadTime(TimeSpan? value)
        => new($"Lead time must be a positive duration, but was {value}");
}

public class ProductNotFoundException : Exception
{
    public Guid ProductId { get; }

    public ProductNotFoundException(Guid productId)
        : base($"Product with ID {productId} was not found")
    {
        ProductId = productId;
    }
}

public class ProductStateException : Exception
{
    public Guid ProductId { get; }

    private ProductStateException(Guid productId, string message) : base(message)
    {
        ProductId = productId;
    }

    public static ProductStateException AlreadyEnabled(Guid productId)
        => new(productId, $"Product {productId} is already enabled");

    public static ProductStateException AlreadyDisabled(Guid productId)
        => new(productId, $"Product {productId} is already disabled");

    public static ProductStateException CannotModifyDiscontinuedProduct(Guid productId, string operation)
        => new(productId, $"Cannot perform {operation} on discontinued product {productId}");
}

public class ProductConflictException : Exception
{
    public string Sku { get; }

    private ProductConflictException(string sku, string message) : base(message)
    {
        Sku = sku;
    }

    public static ProductConflictException DuplicateSku(string sku)
        => new(sku, $"Product with SKU '{sku}' already exists");
}
```

### Exception Conversion

```csharp
// File: KbStore.Catalog.Services/Extensions/ProductRequestFaultExceptionExtensions.cs
namespace KbStore.Catalog.Services.Extensions;

using Abstractions.Exceptions;
using MassTransit;

public static class ProductRequestFaultExceptionExtensions
{
    public static Exception ToProductException(this RequestFaultException exception)
    {
        var fault = exception.Fault;
        var faultType = fault.Exceptions.FirstOrDefault()?.ExceptionType;
        var message = fault.Exceptions.FirstOrDefault()?.Message ?? "Unknown error";

        return faultType switch
        {
            nameof(ProductNotFoundException) => ParseNotFoundException(message),
            nameof(ProductValidationException) => new ProductValidationException(message),
            nameof(ProductStateException) => ParseStateException(message),
            nameof(ProductConflictException) => ParseConflictException(message),
            _ => new Exception($"Unknown product error: {message}")
        };
    }

    private static ProductNotFoundException ParseNotFoundException(string message)
    {
        // Extract Guid from message: "Product with ID {guid} was not found"
        var guidString = message.Split(' ').FirstOrDefault(s => Guid.TryParse(s, out _));
        var productId = Guid.TryParse(guidString, out var id) ? id : Guid.Empty;
        return new ProductNotFoundException(productId);
    }

    private static ProductStateException ParseStateException(string message)
    {
        var parts = message.Split(' ');
        var productId = Guid.TryParse(parts.FirstOrDefault(s => Guid.TryParse(s, out _)), out var id)
            ? id
            : Guid.Empty;

        if (message.Contains("already enabled"))
            return ProductStateException.AlreadyEnabled(productId);

        if (message.Contains("already disabled"))
            return ProductStateException.AlreadyDisabled(productId);

        if (message.Contains("discontinued"))
        {
            var operation = parts.LastOrDefault() ?? "operation";
            return ProductStateException.CannotModifyDiscontinuedProduct(productId, operation);
        }

        return ProductStateException.CannotModifyDiscontinuedProduct(productId, "unknown");
    }

    private static ProductConflictException ParseConflictException(string message)
    {
        // Extract SKU from message: "Product with SKU '{sku}' already exists"
        var sku = message.Split('\'').Skip(1).FirstOrDefault() ?? "unknown";
        return ProductConflictException.DuplicateSku(sku);
    }
}
```

### Exception Middleware (Future)

```csharp
// File: KbStore.ApiService/Middleware/ExceptionHandlingMiddleware.cs
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ProductNotFoundException ex)
        {
            _logger.LogWarning(ex, "Product not found: {ProductId}", ex.ProductId);
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            await context.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Title = "Product Not Found",
                Status = 404,
                Detail = ex.Message
            });
        }
        catch (ProductValidationException ex)
        {
            _logger.LogWarning(ex, "Product validation failed");
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Title = "Validation Error",
                Status = 400,
                Detail = ex.Message
            });
        }
        catch (ProductConflictException ex)
        {
            _logger.LogWarning(ex, "Product conflict: {Sku}", ex.Sku);
            context.Response.StatusCode = StatusCodes.Status409Conflict;
            await context.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Title = "Conflict",
                Status = 409,
                Detail = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Title = "Internal Server Error",
                Status = 500,
                Detail = "An unexpected error occurred"
            });
        }
    }
}
```

### Common Pitfalls

1. **Using generic exceptions** - Create specific exception types for each error scenario
2. **Losing exception context** - Include relevant IDs and values in exception messages
3. **Not logging exceptions** - Always log at appropriate level before re-throwing
4. **Exposing internal details** - Convert to user-friendly messages at API boundary
5. **Not testing exception paths** - Write tests for error scenarios

---

## Summary

These patterns form the foundation of the KbStore architecture:

1. **State Machines** - Core domain logic with state-based behavior
2. **Service Layer** - Clean interface hiding messaging complexity
3. **API Endpoints** - Thin HTTP layer delegating to services
4. **Testing** - Comprehensive coverage using test harness
5. **Event Choreography** - Cross-domain coordination (planned)
6. **Exception Handling** - Clear, actionable error messages

For navigation and file locations, see `docs/NAVIGATION.md`.
For data flows and lifecycles, see `docs/DATAFLOWS.md`.

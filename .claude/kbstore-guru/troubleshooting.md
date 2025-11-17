# KbStore Troubleshooting Guide

## Quick Diagnostic Commands

```bash
# Check if services are running
dotnet run --project /mnt/c/users/bryan/source/bryanboettcher/KbStore/KbStore.AppHost

# Build and check for compilation errors
dotnet build /mnt/c/users/bryan/source/bryanboettcher/KbStore/KbStore.sln

# Run tests to verify functionality
dotnet test /mnt/c/users/bryan/source/bryanboettcher/KbStore/KbStore.sln

# Check specific domain tests
dotnet test /mnt/c/users/bryan/source/bryanboettcher/KbStore/KbStore.Catalog.Tests
dotnet test /mnt/c/users/bryan/source/bryanboettcher/KbStore/KbStore.Storefront.Tests
```

## Common Issues and Solutions

### 1. State Machine Not Receiving Events

**Symptoms**:
- Commands sent but no response
- Saga instances not created
- Events not triggering state transitions

**Root Causes**:

#### Missing Event Correlation
```csharp
// WRONG: No correlation configured
Event(() => NameUpdated);

// RIGHT: Correlation specified
Event(() => NameUpdated, e => e.CorrelateById(c => c.Message.ProductId));
```

**Fix**: Always configure correlation for every event in state machine constructor.

#### Missing OnMissingInstance Handler
```csharp
// WRONG: Silent failure when saga not found
Event(() => NameUpdated, e => e.CorrelateById(c => c.Message.ProductId));

// RIGHT: Explicit error when saga missing
Event(() => NameUpdated, e => e
    .CorrelateById(c => c.Message.ProductId)
    .OnMissingInstance(b => b.Execute(c =>
        throw new ProductNotFoundException(c.Message.ProductId)))
);
```

**Fix**: Add `.OnMissingInstance()` handler to provide clear errors.

#### State Machine Not Registered
```csharp
// In HostBuilderExtensions.cs
services.AddMassTransit(x =>
{
    // Must register state machine
    x.AddSagaStateMachine<ProductStateMachine, ProductEntity>()
        .EntityFrameworkRepository(r =>
        {
            r.ExistingDbContext<ApplicationDbContext>();
            r.UsePostgres();
        });
});
```

**Fix**: Verify state machine is registered in `HostBuilderExtensions.AddMassTransit()`.

**Diagnostic Steps**:
1. Check MassTransit logs for correlation errors
2. Verify saga repository configuration
3. Check database for existing saga instances
4. Use RabbitMQ management UI to see message flow

### 2. OptimisticConcurrencyException

**Symptoms**:
- `DbUpdateConcurrencyException` thrown
- Saga updates fail intermittently
- RowVersion conflicts in logs

**Root Cause**: Multiple consumers or state transitions updating same saga instance simultaneously.

**Solutions**:

#### Ensure RowVersion Configuration
```csharp
// In SagaMap
entity.Property(x => x.RowVersion)
    .HasColumnType("xid")  // PostgreSQL
    .IsRowVersion();
```

#### MassTransit Retries (Built-in)
MassTransit automatically retries on optimistic concurrency exceptions. Usually resolves itself.

#### Check for Duplicate Event Processing
```csharp
// Ensure idempotency in event handlers
When(InventoryQuantityChanged)
    .Then(ctx =>
    {
        // Only update if actually changed
        if (ctx.Saga.StockQuantity != ctx.Message.StockQuantity)
        {
            ctx.Saga.StockQuantity = ctx.Message.StockQuantity;
        }
    });
```

**Diagnostic Steps**:
1. Check logs for retry patterns
2. Verify RowVersion is configured
3. Look for duplicate event publishing
4. Check consumer concurrency settings

### 3. Database Migration Failures

**Symptoms**:
- Migration command fails
- "Column already exists" errors
- "Table not found" errors on startup

**Solutions**:

#### Check Migration History
```bash
# Connect to PostgreSQL via PgWeb (port 5050)
# Run query:
SELECT * FROM "__EFMigrationsHistory" ORDER BY "MigrationId";
```

#### Reset Database (DEV ONLY)
```bash
# WARNING: Deletes all data
dotnet ef database drop --project /mnt/c/users/bryan/source/bryanboettcher/KbStore/KbStore.Catalog --context ApplicationDbContext --force
dotnet ef database update --project /mnt/c/users/bryan/source/bryanboettcher/KbStore/KbStore.Catalog --context ApplicationDbContext
```

#### Fix Migration Conflicts
```bash
# Remove bad migration
dotnet ef migrations remove --project /mnt/c/users/bryan/source/bryanboettcher/KbStore/KbStore.Catalog --context ApplicationDbContext

# Recreate migration
dotnet ef migrations add FixedMigration --project /mnt/c/users/bryan/source/bryanboettcher/KbStore/KbStore.Catalog --context ApplicationDbContext
```

#### Auto-Migration Not Running
```csharp
// Verify in Program.cs
await app.RunMigrationsAsync().ConfigureAwait(false);
```

**Fix**: Ensure `RunMigrationsAsync()` is called before `RunAsync()`.

### 4. Events Not Being Consumed

**Symptoms**:
- Events published but consumers don't execute
- Cross-domain orchestration not working
- SellableItem not created when Product is created

**Root Causes**:

#### Consumer Not Registered
```csharp
// In ApiService HostBuilderExtensions
services.AddMassTransit(x =>
{
    // Must register consumer
    x.AddConsumer<ProductCreatedConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.ConfigureEndpoints(context);  // Required!
    });
});
```

**Fix**: Verify consumer is registered AND `ConfigureEndpoints()` is called.

#### Service Not Injected
```csharp
// Consumer requires service
public class ProductCreatedConsumer : IConsumer<ProductCreated>
{
    private readonly ISellableItemCommandService _service;

    public ProductCreatedConsumer(ISellableItemCommandService service)
    {
        _service = service;  // Must be registered in DI
    }
}

// In ApiService Program.cs or Extensions
builder.Services.AddStorefrontServices();  // Registers ISellableItemCommandService
```

**Fix**: Ensure all dependencies are registered in DI container.

#### Wrong Event Type
```csharp
// WRONG: Consumer expects different event
public class ProductCreatedConsumer : IConsumer<ProductUpdated>  // Wrong!

// RIGHT: Matches published event
public class ProductCreatedConsumer : IConsumer<ProductCreated>
```

**Fix**: Match consumer event type to published event.

**Diagnostic Steps**:
1. Check RabbitMQ management UI for queues and messages
2. Verify consumer registration in logs
3. Check ApiService startup logs for "Consumer configured" messages
4. Use `Harness.Consumed.Any<TEvent>()` in tests

### 5. Test Failures

#### Saga Not Found in Test Harness
```csharp
// WRONG: Using wrong ID
var saga = SagaHarness.Sagas.Contains(wrongId);

// RIGHT: Use actual saga ID from response
var sagaId = Response.Message.ProductId;
var saga = SagaHarness.Sagas.Contains(sagaId);
saga.ShouldNotBeNull();
```

**Fix**: Ensure you're using correct correlation ID.

#### Event Not Published in Test
```csharp
// Check if event was actually published
(await Harness.Published.Any<ProductCreated>()).ShouldBeTrue();

// If false, check state machine
When(Created)
    .PublishAsync(Message<ProductCreated>);  // Must be here
```

**Fix**: Verify state machine publishes event during transition.

#### Test Timeout
```csharp
// In EventingTestBase or test configuration
services.AddMassTransitTestHarness(conf =>
{
    conf.SetTestTimeouts(
        testTimeout: TimeSpan.FromSeconds(5),  // Increase if needed
        testInactivityTimeout: TimeSpan.FromSeconds(1)
    );
});
```

**Fix**: Increase timeout for slow operations (e.g., inventory requests).

#### Mock Response Not Working
```csharp
// Setup handler in test
protected override void OnHarnessCreating(IBusRegistrationConfigurator configurator)
{
    base.OnHarnessCreating(configurator);

    configurator.AddHandler<InventoryStatusRequest>(async context =>
    {
        await context.RespondAsync<InventoryStatusResponse>(new
        {
            InventoryId = context.Message.InventoryId,
            StockQuantity = 100,
            Status = InventoryStatus.Available,
            // ... other fields
        });
    });
}
```

**Fix**: Ensure handler is registered BEFORE test harness starts.

### 6. Service Layer Exceptions

#### RequestFaultException Not Converted
```csharp
// WRONG: MassTransit exception leaks to caller
try
{
    var response = await client.GetResponse<CreateProductResponse>(...);
    return response.Message;
}
catch (Exception e)
{
    throw;  // Leaks RequestFaultException
}

// RIGHT: Convert to domain exception
try
{
    var response = await client.GetResponse<CreateProductResponse>(...);
    return response.Message;
}
catch (RequestFaultException e)
{
    throw e.ToProductException();  // Extension method
}
```

**Fix**: Always catch `RequestFaultException` and convert to domain exception.

#### Missing Validation
```csharp
// WRONG: No validation before sending command
public async Task<ProductModel> CreateAsync(string sku, ...)
{
    var client = _clientFactory.CreateRequestClient<CreateProductRequest>();
    var response = await client.GetResponse<CreateProductResponse>(new { Sku = sku, ... });
    return response.Message;
}

// RIGHT: Validate before sending
public async Task<ProductModel> CreateAsync(string sku, ...)
{
    if (string.IsNullOrWhiteSpace(sku))
        throw new ProductValidationException("SKU must have a value");

    var client = _clientFactory.CreateRequestClient<CreateProductRequest>();
    // ...
}
```

**Fix**: Add validation in service layer to fail fast with clear errors.

### 7. MongoDB Issues

#### Connection String Problems
```csharp
// Verify in Storefront Program.cs
builder.AddMongoDBClient("storefront");  // Must match Aspire resource name

// Aspire orchestration
var storefront_db = mongo.AddDatabase("storefront");  // Must match
```

**Fix**: Ensure connection string names match between Aspire and domain projects.

#### Index Not Created
```csharp
// In Storefront Program.cs
await app.EnsureMongoDbIndexesAsync();  // Must be called before RunAsync

// Extension method implementation
public static async Task EnsureMongoDbIndexesAsync(this WebApplication app)
{
    var client = app.Services.GetRequiredService<IMongoClient>();
    var database = client.GetDatabase("storefront");
    var collection = database.GetCollection<SellableItemEntity>("sellableItemSagas");

    // Create indexes
    await collection.Indexes.CreateOneAsync(
        new CreateIndexModel<SellableItemEntity>(
            Builders<SellableItemEntity>.IndexKeys.Ascending(x => x.Sku),
            new CreateIndexOptions { Unique = true }
        )
    );
}
```

**Fix**: Ensure index creation runs on startup.

#### Saga Persistence Fails
```csharp
// Verify saga repository configuration
services.AddSagaStateMachineRepository<SellableItemEntity>()
    .MongoDbRepository(r =>
    {
        r.Connection = connectionString;
        r.DatabaseName = "storefront";
        r.CollectionName = "sellableItemSagas";  // Must be consistent
    });
```

**Fix**: Check connection string, database name, and collection name.

### 8. API Endpoint Issues

#### Endpoint Not Registered
```csharp
// In ProductEndpoints.cs
public static void MapTo(WebApplication app)
{
    var group = app.MapGroup("/products");
    group.MapPost("", Create);
    group.MapGet("{id:guid}", GetById);
    // ... other endpoints
}

// In Program.cs
app.MapApplicationEndpoints();  // Must call this

// In WebApplicationExtensions.cs
public static void MapApplicationEndpoints(this WebApplication app)
{
    ProductEndpoints.MapTo(app);
    InventoryEndpoints.MapTo(app);
    // SellableItemEndpoints.MapTo(app);  // Add when created
}
```

**Fix**: Ensure endpoint mapping is called in startup AND endpoint is added to extensions.

#### Service Not Injected
```csharp
// Endpoint requires service
public static async Task<IResult> Create(
    [FromServices] IProductCommandService commandService,  // Must be registered
    ...)
{
    // ...
}

// In ApiService Program.cs or Extensions
builder.Services.AddCatalogServices();  // Registers IProductCommandService
```

**Fix**: Ensure service is registered in ApiService DI container.

#### Wrong HTTP Verb
```csharp
// WRONG: Using POST for query
group.MapPost("", GetAll);  // Should be GET

// RIGHT: Use appropriate verb
group.MapGet("", GetAll);
group.MapPost("", Create);
group.MapPatch("{id:guid}/name", UpdateName);
group.MapDelete("{id:guid}", Delete);
```

**Fix**: Use correct HTTP verb for operation semantics.

### 9. Aspire Orchestration Issues

#### Container Won't Start
```bash
# Check Docker is running
docker ps

# Check Aspire logs
# Logs are in Aspire dashboard (auto-opens with dotnet run)
```

**Solutions**:
- Ensure Docker Desktop is running
- Check port conflicts (5050, 15672, etc.)
- Delete volumes and restart: `docker volume prune`
- Check Aspire version matches (9.4.1)

#### Database Not Accessible
```bash
# Check connection string in logs
# Verify in Aspire dashboard under "Resources"
```

**Fix**: Connection strings are auto-generated by Aspire. Check dashboard for actual values.

### 10. Cross-Domain Correlation Issues

#### Wrong Guid Used
```csharp
// WRONG: Different Guid for each domain
var catalogProductId = Guid.NewGuid();
var storefrontItemId = Guid.NewGuid();  // NO!

// RIGHT: Same Guid across domains
var correlationId = Guid.NewGuid();
// Catalog uses: Product.CorrelationId = correlationId
// Storefront uses: SellableItem.CorrelationId = correlationId
```

**Fix**: Use same `Guid` as `CorrelationId` in both domains for the same logical entity.

#### Event Missing Correlation ID
```csharp
// WRONG: Event without ProductId
public record ProductCreated
{
    public string Sku { get; init; }
    // Missing ProductId!
}

// RIGHT: Include correlation ID
public record ProductCreated : ProductModel
{
    public Guid ProductId { get; init; }  // Cross-domain correlation
    public string Sku { get; init; }
    // ...
}
```

**Fix**: Always include correlation ID in events for cross-domain tracking.

## Debugging Strategies

### 1. Enable Verbose Logging
```json
// In appsettings.Development.json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "MassTransit": "Debug",
      "Microsoft.EntityFrameworkCore": "Information"
    }
  }
}
```

### 2. Use RabbitMQ Management UI
- URL: http://localhost:15672
- Login: guest/guest
- Check:
  - Queues (should see one per consumer)
  - Messages (should show throughput)
  - Exchanges (should see events being published)

### 3. Use Database Management UIs
- **PgWeb** (PostgreSQL): http://localhost:5050
  - Check saga tables
  - Query migration history
  - Verify data integrity

- **MongoExpress** (MongoDB): Auto-assigned port
  - Check collections
  - Verify indexes
  - View documents

### 4. Use Aspire Dashboard
- Auto-opens with orchestration
- Shows:
  - Service health
  - Logs for all services
  - Resource status
  - Metrics and traces

### 5. Unit Test Isolation
```csharp
// Test specific scenario in isolation
[Test]
public async Task Should_handle_inventory_timeout()
{
    // Setup slow inventory response
    configurator.AddHandler<InventoryStatusRequest>(async context =>
    {
        await Task.Delay(10000);  // Trigger timeout
    });

    // Act
    var response = await Client.GetResponse<CreateProductResponse>(...);

    // Assert timeout behavior
    response.Message.IsEnabled.ShouldBeFalse();
}
```

### 6. Step-Through Debugging
```csharp
// Add breakpoints in:
// 1. State machine When() handlers
// 2. Service layer methods
// 3. API endpoint handlers
// 4. Event consumers

// Run with debugger:
// dotnet run --project KbStore.AppHost
// Attach debugger to specific service process
```

## Performance Issues

### Slow State Machine Transitions
- Check database query performance
- Verify indexes on `CorrelationId`, `CurrentState`
- Look for N+1 query problems

### High Memory Usage
- Check saga cleanup (are finalized sagas being removed?)
- Verify RowVersion/optimistic concurrency
- Look for saga leaks (instances never finalized)

### Message Backlog
- Check consumer performance (slow processing?)
- Verify consumer concurrency settings
- Look for unhandled exceptions causing retries

## Getting Help

### Check Documentation
1. `/mnt/c/users/bryan/source/bryanboettcher/KbStore/CLAUDE.md` - Primary guide
2. `/mnt/c/users/bryan/source/bryanboettcher/KbStore/docs/PATTERNS.md` - Implementation patterns
3. This troubleshooting guide

### Check External Docs
- MassTransit: https://masstransit.io/documentation/concepts
- Aspire: https://learn.microsoft.com/en-us/dotnet/aspire/
- EF Core: https://learn.microsoft.com/en-us/ef/core/

### Common Search Patterns
- "MassTransit saga correlation"
- "EF Core optimistic concurrency PostgreSQL"
- "MassTransit consumer not executing"
- "Aspire connection string configuration"

## Prevention Best Practices

1. **Always Configure Event Correlation** - No event without correlation
2. **Always Add OnMissingInstance Handlers** - Provide clear errors
3. **Always Validate in Service Layer** - Fail fast with meaningful errors
4. **Always Convert MassTransit Exceptions** - Hide framework details
5. **Always Check for Idempotency** - Events can be replayed
6. **Always Use Same Guid Across Domains** - Cross-domain correlation
7. **Always Test Error Scenarios** - Not just happy paths
8. **Always Log Extensively** - Distributed systems are hard to debug

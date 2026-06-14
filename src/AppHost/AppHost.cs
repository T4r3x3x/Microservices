var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder
    .AddPostgres("postgres")
    .WithDataVolume();

var catalogDatabase = postgres.AddDatabase("catalog-db", "catalog");
var ordersDatabase = postgres.AddDatabase("orders-db", "orders");

var cartCache = builder
    .AddRedis("cart-cache")
    .WithDataVolume();

var messaging = builder
    .AddRabbitMQ("messaging")
    .WithDataVolume()
    .WithManagementPlugin();

var catalog = builder
    .AddProject<Projects.Catalog_Api>("catalog-api")
    .WithReference(catalogDatabase)
    .WaitFor(catalogDatabase);

var cart = builder
    .AddProject<Projects.Cart_Api>("cart-api")
    .WithReference(cartCache)
    .WithReference(catalog)
    .WaitFor(cartCache)
    .WaitFor(catalog);

var orders = builder
    .AddProject<Projects.Orders_Api>("orders-api")
    .WithReference(ordersDatabase)
    .WithReference(messaging)
    .WithReference(cart)
    .WaitFor(ordersDatabase)
    .WaitFor(messaging)
    .WaitFor(cart);

builder
    .AddProject<Projects.Notifications_Worker>("notifications-worker")
    .WithReference(messaging)
    .WaitFor(messaging);

builder
    .AddProject<Projects.Gateway>("gateway")
    .WithReference(catalog)
    .WithReference(cart)
    .WithReference(orders)
    .WithExternalHttpEndpoints();

builder.Build().Run();

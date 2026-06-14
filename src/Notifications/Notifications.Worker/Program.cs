using Notifications.Worker;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

if (builder.Configuration.GetConnectionString("messaging") is not null)
{
    builder.AddRabbitMQClient("messaging");
}

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();

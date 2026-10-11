using WalletSystem.Worker.Messaging;
using WalletSystem.Worker.Outbox;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection("RabbitMq"));
builder.Services.Configure<OutboxOptions>(builder.Configuration.GetSection("Outbox"));

builder.Services.AddSingleton<RabbitMqPublisher>();
builder.Services.AddHostedService<OutboxPublisherService>();

await builder.Build().RunAsync();
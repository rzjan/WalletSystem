using WalletSystem.Worker.Consumer;
using WalletSystem.Worker.Messaging;
using WalletSystem.Worker.Outbox;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection("RabbitMq"));
builder.Services.Configure<OutboxOptions>(builder.Configuration.GetSection("Outbox"));
builder.Services.Configure<ConsumerOptions>(builder.Configuration.GetSection("Consumer"));

// Publisher del Outbox
builder.Services.AddSingleton<RabbitMqPublisher>();
builder.Services.AddHostedService<OutboxPublisherService>();

// Consumer con Inbox, reintentos y DLQ
builder.Services.AddSingleton<FaultInjector>();
builder.Services.AddSingleton<IEventHandler, LoggingEventHandler>();
builder.Services.AddSingleton<InboxProcessor>();
builder.Services.AddHostedService<EventConsumerService>();

await builder.Build().RunAsync();
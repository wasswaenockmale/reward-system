using MassTransit;
using NotificationService.Consumers;

var builder = WebApplication.CreateBuilder(args);

// ── MassTransit Consumers ─────────────────────────────────────────────────────
// CONCEPT: We register all consumers here. MassTransit creates the RabbitMQ
// queues automatically and routes messages to the right consumer.
builder.Services.AddMassTransit(x =>
{
    // Register all consumers
    x.AddConsumer<PointsAssignedConsumer>();
    x.AddConsumer<PointsRedeemedConsumer>();
    x.AddConsumer<WalletCreditedConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        var host = builder.Configuration["RabbitMq:Host"] ?? "rabbitmq";
        cfg.Host(host, "/", h =>
        {
            h.Username("guest");
            h.Password("guest");
        });

        // ConfigureEndpoints auto-creates queues for each consumer
        cfg.ConfigureEndpoints(context);
    });
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "notification-service" }));

app.MapControllers();
app.Run();

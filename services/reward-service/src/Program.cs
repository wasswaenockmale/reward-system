using System.Text;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RewardService.Data;
using RewardService.HttpClients;
using RewardService.Services;

var builder = WebApplication.CreateBuilder(args);

// ── Database ──────────────────────────────────────────────────────────────────
builder.Services.AddDbContext<RewardDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// ── Typed HTTP Clients ────────────────────────────────────────────────────────
// CONCEPT: IHttpClientFactory manages HttpClient instances (connection pooling).
// AddHttpClient<T> creates a typed client with a base URL from config.
builder.Services.AddHttpClient<UserServiceClient>(client =>
    client.BaseAddress = new Uri(builder.Configuration["Services:UserService"]!));

builder.Services.AddHttpClient<WalletServiceClient>(client =>
    client.BaseAddress = new Uri(builder.Configuration["Services:WalletService"]!));

// ── Business Logic ────────────────────────────────────────────────────────────
builder.Services.AddScoped<IRewardService, RewardServiceImpl>();

// ── MassTransit + RabbitMQ ────────────────────────────────────────────────────
// CONCEPT: MassTransit is the .NET messaging library. It sits on top of RabbitMQ.
// AddMassTransit registers IPublishEndpoint in DI — we inject it in RewardService.
builder.Services.AddMassTransit(x =>
{
    x.UsingRabbitMq((context, cfg) =>
    {
        var host = builder.Configuration["RabbitMq:Host"] ?? "rabbitmq";
        cfg.Host(host, "/", h =>
        {
            h.Username("guest");
            h.Password("guest");
        });
        cfg.ConfigureEndpoints(context);
    });
});

// ── JWT Auth ──────────────────────────────────────────────────────────────────
var jwtSecret = builder.Configuration["Jwt:Secret"]!;
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<RewardDbContext>();
    db.Database.Migrate();
}

app.UseSwagger();
app.UseSwaggerUI();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();

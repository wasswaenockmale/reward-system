using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// YARP Reverse Proxy
// CONCEPT: YARP reads routes from appsettings.json (ReverseProxy section).
// It forwards requests to downstream services — no custom controller code needed.
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// JWT Auth (gateway-level)
// CONCEPT: The gateway validates JWT tokens BEFORE forwarding to services.
// Individual services can also validate (defence in depth), but the gateway
// is the main guard — it blocks unauthenticated requests early.
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

// CORS (allow frontend / mobile apps to call the gateway)
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

// Map the YARP proxy — handles all routing defined in appsettings.json
app.MapReverseProxy();

// Gateway health check
app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy",
    service = "api-gateway",
    timestamp = DateTime.UtcNow
}));

app.Run();

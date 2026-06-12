using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using Real_Estate_WebAPI.Interfaces;
using Real_Estate_WebAPI.Repositories;
using Real_Estate_WebAPI.Services;
using Real_Estate_WebAPI.Services.Auth;
using Real_Estate_WebAPI.Services.Email;
using Real_Estate_WebAPI.Settings;
using Resend;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ---------------- BASIC SERVICES ----------------
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ---------------- CONFIGURATION ----------------
builder.Services.Configure<MongoDbSettings>(
    builder.Configuration.GetSection("MongoDbSettings"));

builder.Services.Configure<ResendSettings>(
    builder.Configuration.GetSection("ResendSettings"));

builder.Services.Configure<JwtSettings>(
    builder.Configuration.GetSection("JwtSettings"));

// ---------------- DATABASE ----------------
builder.Services.AddSingleton<IMongoClient>(sp =>
{
    var settings = sp.GetRequiredService<IOptions<MongoDbSettings>>().Value;

    var mongoSettings = MongoClientSettings.FromConnectionString(settings.ConnectionString);

    mongoSettings.ServerSelectionTimeout = TimeSpan.FromSeconds(5);
    mongoSettings.ConnectTimeout = TimeSpan.FromSeconds(5);
    mongoSettings.SocketTimeout = TimeSpan.FromSeconds(5);

    // 🔥 THIS IS THE MISSING PIECE
    mongoSettings.MaxConnectionPoolSize = 5;   // default is ~100
    mongoSettings.MinConnectionPoolSize = 0;

    mongoSettings.SslSettings = new SslSettings
    {
        EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls12
    };

    return new MongoClient(mongoSettings);
});

builder.Services.AddSingleton<DatabaseInitializer>();

// ---------------- DEPENDENCY INJECTION ----------------
builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IPropertyRepository, PropertyRepository>();
builder.Services.AddScoped<IPropertyImageRepository, PropertyImageRepository>();

builder.Services.Configure<ResendSettings>(
    builder.Configuration.GetSection("Resend"));

builder.Services.AddSingleton<IResend>(_ =>
{
    var apiKey = builder.Configuration["Resend:ApiKey"];
    return ResendClient.Create(apiKey);
});

// ---------------- JWT AUTH ----------------
var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>()
    ?? throw new Exception("JwtSettings not configured properly");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,

            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSettings.Key))
        };
    });

// ---------------- CORS ----------------
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy
            .SetIsOriginAllowed(_ => true) // ⚠️ allow all (dev only)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});


builder.Services.AddOptions();
builder.Services.AddHttpClient<ResendClient>();


builder.Services.AddTransient<IResend, ResendClient>();

builder.Services.AddSingleton<IResend>(_ =>
{
    return ResendClient.Create("re_L92MdRY2_7g1MmS5oULRDEpB2c98pD6Pg");

});
var apiKey = builder.Configuration["Resend:ApiKey"];

Console.WriteLine("RESEND API KEY => " + apiKey);
var app = builder.Build();

// ---------------- MIDDLEWARE ----------------

// Swagger (only in dev recommended)
app.UseSwagger();
app.UseSwaggerUI();

// Initialize DB
app.Lifetime.ApplicationStarted.Register(() =>
{
    _ = Task.Run(async () =>
    {
        using var scope = app.Services.CreateScope();

        try
        {
            var initializer = scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();
            await initializer.InitializeAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine("DB init failed: " + ex.Message);
        }
    });
});
// app.UseHttpsRedirection(); // enable in production

// CORS MUST come before auth
app.UseCors("AllowFrontend");

// Handle preflight (optional but safe)
app.Use(async (context, next) =>
{
    if (context.Request.Method == HttpMethods.Options)
    {
        context.Response.StatusCode = StatusCodes.Status200OK;
        return;
    }

    await next();
});

// Auth
app.UseAuthentication();
app.UseAuthorization();

// Routes
app.MapControllers();

app.Run();
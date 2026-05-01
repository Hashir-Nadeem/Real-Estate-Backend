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
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ✅ PORT CONFIG HERE
var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(int.Parse(port));
});

// services...
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// MongoDB
builder.Services.Configure<MongoDbSettings>(
    builder.Configuration.GetSection("MongoDbSettings"));

builder.Services.AddSingleton<DatabaseInitializer>();

builder.Services.AddSingleton<IMongoClient>(sp =>
{
    var settings = sp.GetRequiredService<IOptions<MongoDbSettings>>().Value;
    return new MongoClient(settings.ConnectionString);
});

// Services
builder.Services.AddHttpContextAccessor();
builder.Services.Configure<SmtpSettings>(
    builder.Configuration.GetSection("SmtpSettings"));

builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IPropertyRepository, PropertyRepository>();

// JWT
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
.AddJwtBearer(options =>
{
    var jwt = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>();

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwt.Issuer,
        ValidAudience = jwt.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwt.Key))
    };
});

builder.Services.Configure<JwtSettings>(
    builder.Configuration.GetSection("JwtSettings"));

// ---------------- CORS (FIXED) ----------------

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy
      .SetIsOriginAllowed(origin => true) // ?? works with ngrok.dev
      .AllowAnyHeader()
      .AllowAnyMethod()
      .AllowCredentials();
    });
});

var app = builder.Build();


app.MapGet("/", () => "API is running");
// Swagger
app.UseSwagger();
app.UseSwaggerUI();

// DB Init
using (var scope = app.Services.CreateScope())
{
    var initializer = scope.ServiceProvider
        .GetRequiredService<DatabaseInitializer>();

    await initializer.InitializeAsync();
}

// HTTPS
//app.UseHttpsRedirection();

// ?? IMPORTANT: CORS FIRST
app.UseCors("AllowAll");

// ?? Handle OPTIONS (preflight)
app.Use(async (context, next) =>
{
    if (context.Request.Method == "OPTIONS")
    {
        context.Response.StatusCode = 200;
        await context.Response.CompleteAsync();
        return;
    }

    await next();
});

// Auth AFTER CORS
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
using JedoxTranslator.API.Endpoints;
using JedoxTranslator.Core.Data;
using JedoxTranslator.Core.Repositories;
using JedoxTranslator.Core.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddSingleton(Log.Logger);

builder.Services.AddDbContext<TranslationDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        b => b.MigrationsAssembly(typeof(TranslationDbContext).Assembly.GetName().Name)));

builder.Services.AddScoped<ITranslationRepository, TranslationRepository>();
builder.Services.AddScoped<ITranslationService, TranslationService>();

// Configure Azure AD authentication
var tenantId = builder.Configuration["AzureAd:TenantId"];
var instance = builder.Configuration["AzureAd:Instance"];
var audience = builder.Configuration["AzureAd:Audience"];

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = $"{instance}{tenantId}/v2.0";
        options.Audience = audience;
        options.RequireHttpsMetadata = true; // Set to true for production
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidAudiences = new[] { audience, builder.Configuration["AzureAd:ClientId"] }
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddOpenApi();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(
                "http://localhost:8080",
                "http://localhost:5000",
                "https://localhost:5001",
                "http://localhost")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var app = builder.Build();

//TODO: Migrating here is only for the Demo purposes. In prod, we'll need to move migrations to be part of the pipeline. 
using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<TranslationDbContext>();
        Log.Information("Applying database migrations...");
        db.Database.Migrate();
        Log.Information("Database migrations applied successfully");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "An error occurred while migrating the database");
        throw;
    }
}

app.UseCors();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options
            .WithTitle("Jedox Translator API")
            .WithTheme(ScalarTheme.Purple)
            .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
    });
}

app.UseAuthentication();
app.UseAuthorization();

app.RegisterEndpoints();

app.Run();

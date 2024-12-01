using API.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Google;
using Persistence;
using Serilog;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration) // Reads settings from appsettings.json
    .Enrich.FromLogContext()
    .WriteTo.Console() // Logs to the console
    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day) // Logs to files daily
    .CreateLogger();

// Use Serilog as the logging provider
builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddApplicationServices(builder.Configuration);

// Add Authentication and configure Google OAuth
//builder.Services.AddAuthentication(options =>
//{
//    options.DefaultScheme = "Cookies"; // Use cookies for session management
//    options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme; // Use Google OAuth for login
//})
//.AddCookie("Cookies") // Add cookie-based authentication
//.AddGoogle(options =>
//{
//    options.ClientId = builder.Configuration["GoogleOAuth:ClientId"]; // Google Client ID from appsettings
//    options.ClientSecret = builder.Configuration["GoogleOAuth:ClientSecret"]; // Google Client Secret from appsettings
//});

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
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
        };
    });

builder.Services.AddControllers();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Enable authentication and authorization middleware
app.UseAuthentication(); // Add authentication middleware
app.UseAuthorization();  // Add authorization middleware

app.UseCors("CorsPolicy");

app.MapControllers();

using var scope = app.Services.CreateScope();
var services = scope.ServiceProvider;

try
{
    var context = services.GetRequiredService<DataContext>();
    context.Database.Migrate();
    Log.Information("Database migration completed successfully");
}
catch (Exception ex)
{
    Log.Fatal(ex, "An error occurred during migration");
}
finally
{
    Log.CloseAndFlush(); // Ensure all logs are flushed on application shutdown
}

app.Run();

using Encrypz.Infrastructure.Data;
using Encrypz.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Encrypz.API.Services;

namespace Encrypz.API;

public class Program
{
    public static void Main(string[] args)
    {
        DotNetEnv.Env.Load();
        var builder = WebApplication.CreateBuilder(args);
        var desktopSettings = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Encrypz", "settings.json");
        if (!string.IsNullOrEmpty(builder.Configuration["Desktop:InstanceId"]))
            builder.Configuration.AddJsonFile(desktopSettings, optional: true, reloadOnChange: false).AddEnvironmentVariables();

        // Add services to the container.
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("Configure ConnectionStrings:DefaultConnection in %LOCALAPPDATA%\\Encrypz\\settings.json or the ConnectionStrings__DefaultConnection environment variable.");

        builder.Services.AddDbContext<ApplicationDbContext>(options =>
            options.UseMySql(connectionString, ServerVersion.Parse("8.0.32-mysql")));

        builder.Services.AddControllers();
        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        
        builder.Services.AddScoped<IGoogleDriveService, GoogleDriveService>();
        builder.Services.AddHostedService<RecycleBinCleanupService>();

        var allowedOrigins = builder.Configuration["Frontend:AllowedOrigins"]
            ?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            ?? new[] { "http://localhost:5173", "http://localhost:5174" };

        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowFrontend", policy =>
            {
                policy.WithOrigins(allowedOrigins)
                      .AllowAnyHeader()
                      .AllowAnyMethod();
            });
        });

        var app = builder.Build();

        // Initialize the schema before reporting readiness.
        try
        {
            using var scope = app.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Database.EnsureCreated();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(DatabaseConnectionFailure.Prefix + DatabaseConnectionFailure.Classify(ex));
            app.Logger.LogError(ex, "Database initialization failed.");
            Environment.ExitCode = 1;
            return;
        }

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        if (string.IsNullOrEmpty(builder.Configuration["Desktop:InstanceId"]))
            app.UseHttpsRedirection();

        app.UseCors("AllowFrontend");

        app.UseAuthorization();


        app.MapControllers();
        app.MapGet("/health", () => Results.Text(builder.Configuration["Desktop:InstanceId"] ?? "ready"));

        app.Run();
    }
}

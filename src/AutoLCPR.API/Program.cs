using System.Text.Json.Serialization;
using AutoLCPR.API.Services;
using AutoLCPR.Application;
using AutoLCPR.Infrastructure.Data;
using QuestPDF.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddProblemDetails();

var connectionString = ResolveConnectionString(builder.Configuration);
builder.Services.AddInfrastructureServices(connectionString);
builder.Services.AddApplicationServices();
builder.Services.AddScoped<NfeImportOrchestrator>();

QuestPDF.Settings.License = LicenseType.Community;

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await DbInitializer.InitializeAsync(db);
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();

static string ResolveConnectionString(IConfiguration configuration)
{
    var connString = configuration.GetConnectionString("DefaultConnection") ?? "Data Source=autolcpr.db";

    var databasePath = configuration["DatabasePath"] ?? "%AppData%/AutoLCPR/data";
    databasePath = Environment.ExpandEnvironmentVariables(databasePath);
    Directory.CreateDirectory(databasePath);

    return connString.Replace("{DatabasePath}", databasePath);
}

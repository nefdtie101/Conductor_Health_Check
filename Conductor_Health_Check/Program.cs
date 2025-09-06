using Bash;
using Conductor_Health_Check;
using Conductor_Health_Check.Services;
using PowerShell;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
// Add health checks
builder.Services.AddHealthChecks();

builder.Services.AddSingleton<PowershellRunner>();
builder.Services.AddSingleton<BashRunner>();

// Add LogService
builder.Services.AddSingleton<LogService>();

// Register configuration for dependency injection
builder.Services.AddSingleton<IConfiguration>(builder.Configuration);

// Register your custom services
builder.Services.AddHostedService<TaskService>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapHealthChecks("/health");

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
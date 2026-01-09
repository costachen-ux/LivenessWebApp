using LivenessWebApp;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

bool running = true;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// 1. 註冊為 Singleton 讓全域共用
builder.Services.AddSingleton<GlobalErrorMonitor>();

// 2. 註冊 HealthCheck 邏輯
builder.Services.AddHealthChecks()
    .AddCheck("liveness_logic", () => 
    {
        var monitor = builder.Services.BuildServiceProvider().GetRequiredService<GlobalErrorMonitor>();
        return monitor.IsUnstable() 
            ? HealthCheckResult.Unhealthy("偵測到異常高失敗率，請求重啟") 
            : HealthCheckResult.Healthy();
    });

// builder.Services.AddHealthChecks()
//     .AddCheck("self", () => running ? HealthCheckResult.Healthy() : HealthCheckResult.Unhealthy())
//     .AddRedis("redis", tags: ["services"]);;

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// 3. 使用 Middleware (放在最前面)
app.UseMiddleware<ErrorTrackingMiddleware>();

// 4. 開放 API 給 K8s 檢查
app.MapHealthChecks("/healthz/liveness");

// app.UseHealthChecks("/self", new HealthCheckOptions
// {
//     Predicate = r => r.Name.Contains("self")
// });
// app.UseHealthChecks("/ready", new HealthCheckOptions
// {
//     Predicate = r => r.Tags.Contains("services")
// });

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.Map("/switch", appBuilder =>
{
    appBuilder.Run(async context =>
    {
        running = !running;
        await context.Response.WriteAsync($"{Environment.MachineName} running {running}");
    });
});

app.MapGet("/weatherforecast", () =>
    {
        var forecast = Enumerable.Range(1, 5).Select(index =>
                new WeatherForecast
                (
                    DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                    Random.Shared.Next(-20, 55),
                    summaries[Random.Shared.Next(summaries.Length)]
                ))
            .ToArray();
        return forecast;
    })
    .WithName("GetWeatherForecast");

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
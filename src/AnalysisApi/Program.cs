using System.Text.Json;
using AnalysisApi;
using Dapper;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(o =>
    
        o.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
        o.JsonSerializerOptions.WriteIndented = true;
    });

builder.Services.Configure<ApiBehaviorOptions>(o =>
    o.InvalidModelStateResponseFactory = ctx =>
    {
        var message = ctx.ModelState.Values
            .SelectMany(v => v.Errors)
            .Select(e => e.ErrorMessage)
            .FirstOrDefault(x => !string.IsNullOrEmpty(x)) ?? "Некорректное тело запроса";

        return new OkObjectResult(new AnalysisResponse
        {
            IsError = 1,
            ErrorCode = ErrorCodes.InvalidJson,
            ErrorMessage = message
        });
    });

builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<IValidator<AnalysisRequest>, AnalysisRequestValidator>();
builder.Services.AddSingleton<IAnalysisService, AnalysisService>();
builder.Services.AddSingleton(NpgsqlDataSource.Create(
    builder.Configuration.GetConnectionString("Default")
    ?? "Host=localhost;Port=5432;Username=postgres;Password=postgres;Database=test_task"));

var app = builder.Build();

app.MapSwagger("api/swagger/{documentName}/swagger.json");
app.UseSwaggerUI(o =>
{
    o.RoutePrefix = "api/swagger";
    o.SwaggerEndpoint("/api/swagger/v1/swagger.json", "Analysis API");
});

app.MapControllers();

await DbInit.EnsureElementsTableAsync(app.Services.GetRequiredService<NpgsqlDataSource>());

app.Run();

static class DbInit
{
    public static async Task EnsureElementsTableAsync(NpgsqlDataSource dataSource)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await using var connection = await dataSource.OpenConnectionAsync();
                await connection.ExecuteAsync("""
                    CREATE TABLE IF NOT EXISTS elements (
                        id           BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
                        attr_value   TEXT NOT NULL,
                        element_html TEXT NOT NULL
                    );
                    """);
                return;
            }
            catch (Exception) when (attempt < 15)
            {
                await Task.Delay(TimeSpan.FromSeconds(2));
            }
        }
    }
}

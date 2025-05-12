using System.Net.Http.Json;
using CalculatorApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

const string MeasurementsApiBaseUrl = "http://measurements-api";
const string EmissionsApiBaseUrl = "http://emissions-api";
const int RetryCount = 3;
const int EmissionsTimeoutSeconds = 20;
const int IntervalSeconds = 900;

app.MapGet("/api/calculator", async (
    string userId,
    long from,
    long to,
    IHttpClientFactory httpClientFactory) =>
{
    var httpClient = httpClientFactory.CreateClient();

    var measurements = await GetMeasurementsAsync(httpClient, userId, from, to);
    if (measurements is null || measurements.Count == 0)
        return Results.StatusCode(503);

    var emissions = await GetEmissionsAsync(httpClient, from, to);
    if (emissions is null || emissions.Count == 0)
        return Results.StatusCode(503);

    var grouped = measurements
        .GroupBy(m => m.Timestamp - (m.Timestamp % IntervalSeconds))
        .ToDictionary(
            g => g.Key,
            g => g.Average(m => m.Watts)
        );

    double totalKg = 0;

    foreach (var (timestamp, avgWatts) in grouped)
    {
        var factor = emissions.FirstOrDefault(e => e.Timestamp == timestamp)?.Factor ?? 0;
        var kWh = avgWatts / 4.0 / 1000.0;
        totalKg += kWh * factor;
    }

    return Results.Ok(new { totalEmissionKg = Math.Round(totalKg, 4) });
});

app.Run();

static async Task<List<MeasurementDto>?> GetMeasurementsAsync(HttpClient client, string userId, long from, long to)
{
    for (int i = 0; i < RetryCount; i++)
    {
        try
        {
            var result = await client.GetFromJsonAsync<List<MeasurementDto>>(
                $"{MeasurementsApiBaseUrl}/api/measurements/{userId}?from={from}&to={to}");

            if (result is not null && result.Count > 0)
                return result;
        }
        catch
        {
            await Task.Delay(500);
        }
    }

    return null;
}

static async Task<List<EmissionDto>?> GetEmissionsAsync(HttpClient client, long from, long to)
{
    try
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(EmissionsTimeoutSeconds));
        return await client.GetFromJsonAsync<List<EmissionDto>>(
            $"{EmissionsApiBaseUrl}/api/emissions?from={from}&to={to}", cts.Token);
    }
    catch
    {
        return null;
    }
}

app.Run("http://0.0.0.0:8080");
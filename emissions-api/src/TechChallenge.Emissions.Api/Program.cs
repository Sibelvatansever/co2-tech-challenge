using Microsoft.AspNetCore.Mvc;
using SecurityDriven.Core;
using TechChallenge.ChaosMonkey;
using TechChallenge.Common.Exceptions;
using TechChallenge.DataSimulator;
using TechChallenge.Emissions.Api;
using TechChallenge.Emissions.Api.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<IEmissionsRepository, CalculationBasedEmissionsRepository>();
var interval = (int)TimeSpan.FromMinutes(15).TotalSeconds;
builder.Services
    .AddSingleton<IValueCalculator<SeededContext, double>, RandomBasedDeterministicValueCalculator>()
    .AddSingleton<IPointsProvider, PointsProvider>(
        sp => ActivatorUtilities.CreateInstance<PointsProvider>(sp, interval, interval));

var chaosChance = ChausChance.FromPercentage(builder.Configuration.GetValue<int>("ChaosMonkey:Percentage"));
var delay = TimeSpan.FromSeconds(builder.Configuration.GetValue<int>("ChaosMonkey:DelayInSeconds"));
builder.Services.AddScoped<IChaosMonkey>(sp => new DelayChaosMonkey(delay, chaosChance, CryptoRandom.Shared));

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapGet(
        "/emissions",
        async (
            [FromQuery] long from,
            [FromQuery] long to,
            IEmissionsRepository repository,
            IChaosMonkey chaosMonkey,
            ILogger<Program> logger,
            CancellationToken cancellationToken) =>
        {
            using (var _ = logger.BeginScope(
                       new Dictionary<string, object>
                       {
                           ["from"] = from,
                           ["to"] = to,
                       })) ;
            try
            {
                await chaosMonkey.UnleashChaos();

                if (from >= to)
                    return Results.BadRequest("Invalid request time frame");

                var measurements =
                    repository
                        .GetAsync(
                            from,
                            to,
                            cancellationToken)
                        .SelectAwait(m => ValueTask.FromResult(new EmissionResponse(m.Timestamp, m.KgPerWattHr)));

                return Results.Ok(measurements);
            }
            catch (NotFoundException e)
            {
                return Results.NotFound(e.Message);
            }
        })
    .WithName("GetUserMeasurements")
    .Produces<IReadOnlyCollection<EmissionResponse>>()
    .Produces(StatusCodes.Status400BadRequest)
    .Produces(StatusCodes.Status404NotFound)
    .Produces(StatusCodes.Status500InternalServerError)
    .WithOpenApi();

app.Run("http://0.0.0.0:80");

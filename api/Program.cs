using System.Text.Json.Serialization;
using seedapi.Endpoints;
using seedapi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<SeedFileService>();
builder.Services.AddHostedService<SeedReloadHostedService>();

builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull);

var app = builder.Build();

app.MapSeedEndpoints();

app.Run();

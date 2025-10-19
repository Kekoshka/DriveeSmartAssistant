using DriveeSmartAssistant.Classes;
using DriveeSmartAssistant.Interfaces;
using DriveeSmartAssistant.Models.Pipelines.Options;
using DriveeSmartAssistant.Services;
using Microsoft.Extensions.Options;

string dir = AppDomain.CurrentDomain.BaseDirectory;
Console.WriteLine(dir);

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<MyAppSettings>(builder.Configuration.GetSection("MyAppSettings"));
builder.Services.Configure<RidePriceOptions>(builder.Configuration.GetSection("MyAppSettings:Ride"));
builder.Services.Configure<UserAcceptanceOptions>(builder.Configuration.GetSection("MyAppSettings:User"));

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register services
builder.Services.AddSingleton<IRidePriceService, RidePriceService>();
builder.Services.AddSingleton<IUserAcceptanceService, UserAcceptanceService>();
builder.Services.AddTransient<IMainHandleService, MainHandleService>();

// Configure logging
builder.Services.AddLogging();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Initialize ML model on startup
try
{
    var ridePriceService = app.Services.GetRequiredService<IRidePriceService>();
    var userAcceptanceService = app.Services.GetRequiredService<IUserAcceptanceService>();
    var settings = app.Services.GetRequiredService<IOptions<MyAppSettings>>().Value;
    var zip = settings.zip;
    var train = settings.train;
    // Try to load pre-trained models
    if (!ridePriceService.IsModelLoaded)
    {
        // Train new models if pre-trained not available
        ridePriceService.TrainModels(dir + train);
        ridePriceService.SaveModels(dir + zip + "price_model.zip");
    }
    if (!userAcceptanceService.IsModelLoaded)
    {
        // Train new models if pre-trained not available
        userAcceptanceService.TrainModel(dir + train);
        userAcceptanceService.SaveModel(dir + zip + "userAcceptance_model.zip");
    }
    userAcceptanceService.LoadModel(dir + zip + "userAcceptance_model.zip");
    ridePriceService.LoadModels(dir + zip + "price_model.zip");
    Console.WriteLine("ML models initialized successfully");
}
catch (Exception ex)
{
    Console.WriteLine($"Error initializing ML models: {ex.Message}");
}


app.Run();

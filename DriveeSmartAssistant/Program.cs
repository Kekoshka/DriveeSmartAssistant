using DriveeSmartAssistant.Interfaces;
using DriveeSmartAssistant.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register services
builder.Services.AddSingleton<IRidePriceService, RidePriceService>();

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

    // Try to load pre-trained models
    

    if (!ridePriceService.IsModelLoaded)
    {
        // Train new models if pre-trained not available
        ridePriceService.TrainModels("C:/Users/Kekoshka/Desktop/train.csv");
        ridePriceService.SaveModels("Models/price_model.zip", "Models/userAcceptance_model.zip", "Models/driverAcceptance_model.zip");
    }
    ridePriceService.LoadModels("Models/price_model.zip", "Models/userAcceptance_model.zip", "Models/driverAcceptance_model.zip");
    Console.WriteLine("ML models initialized successfully");
}
catch (Exception ex)
{
    Console.WriteLine($"Error initializing ML models: {ex.Message}");
}


app.Run();

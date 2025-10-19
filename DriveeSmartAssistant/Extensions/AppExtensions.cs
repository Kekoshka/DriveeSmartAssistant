using DriveeSmartAssistant.Common.Options;
using DriveeSmartAssistant.Interfaces;
using DriveeSmartAssistant.Services;
using Microsoft.Extensions.Options;

namespace DriveeSmartAssistant.Extensions
{
    public static class AppExtensions
    {
        public static void InitializeMLModels(this WebApplication app)
        {
            try
            {
                string dir = AppDomain.CurrentDomain.BaseDirectory;
                var ridePriceService = app.Services.GetRequiredService<IRidePriceService>();
                var userAcceptanceService = app.Services.GetRequiredService<IUserAcceptanceService>();
                var settings = app.Services.GetRequiredService<IOptions<MyAppSettings>>().Value;
                var zip = settings.zip;
                var train = settings.train;
                // Try to load pre-trained models
                if (!ridePriceService.IsModelLoaded)
                {
                    // Train new models if pre-trained not available
                    ridePriceService.TrainModels(train);
                    ridePriceService.SaveModels(dir + zip + "price_model.zip");
                }
                if (!userAcceptanceService.IsModelLoaded)
                {
                    // Train new models if pre-trained not available
                    userAcceptanceService.TrainModel(train);
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
        }

        public static void RegisterServices(this WebApplicationBuilder builder)
        {
            builder.Services.AddSingleton<IRidePriceService, RidePriceService>();
            builder.Services.AddSingleton<IUserAcceptanceService, UserAcceptanceService>();
            builder.Services.AddTransient<IMainHandleService, MainHandleService>();
            builder.Services.AddSingleton<IOrderCompletionService, OrderCompletionService>();

        }
        public static void ConfigureOptions(this WebApplicationBuilder builder)
        {
            builder.Services.Configure<MyAppSettings>(builder.Configuration.GetSection("MyAppSettings"));
            builder.Services.Configure<RidePriceOptions>(builder.Configuration.GetSection("MyAppSettings:Ride"));
            builder.Services.Configure<UserAcceptanceOptions>(builder.Configuration.GetSection("MyAppSettings:User"));
        }
    }
}

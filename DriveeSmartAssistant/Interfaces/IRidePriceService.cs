using DriveeSmartAssistant.Models;
using DriveeSmartAssistant.Models.Mapping;
using Microsoft.ML;

namespace DriveeSmartAssistant.Interfaces
{
    public interface IRidePriceService
    {
        void TrainModels(string csvFilePath);
        float GetRecommendedPrice(PricePredictionInput input);
        float GetUserAcceptanceProbability(UserAcceptanceInput input);
        float GetDriverAcceptanceProbability(DriverAcceptanceInput input);
        void SaveModels(string priceModelPath, string userAcceptanceModelPath, string driverAcceptanceModelPath);
        void LoadModels(string priceModelPath, string userAcceptanceModelPath, string driverAcceptanceModelPath);
        bool IsModelLoaded { get; }
    }
}

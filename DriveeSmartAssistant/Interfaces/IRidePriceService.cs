using DriveeSmartAssistant.Classes.Inputs;

namespace DriveeSmartAssistant.Interfaces
{
    public interface IRidePriceService
    {
        void TrainModels(string csvFilePath);
        float GetRecommendedPrice(PricePredictionInput input);
        void SaveModels(string priceModelPath);
        void LoadModels(string priceModelPath);
        bool IsModelLoaded { get; }
    }
}

using DriveeSmartAssistant.Models.Inputs;
using DriveeSmartAssistant.Models.Predictions;

namespace DriveeSmartAssistant.Interfaces
{
    public interface IOrderCompletionService
    {
        void TrainModel(string trainingDataPath);
        OrderCompletionPrediction PredictCompletion(string csvLine);
        OrderCompletionPrediction Predict(ModelInputForPrediction input);
        List<OrderCompletionPrediction> PredictBatch(List<string> csvLines);
        void SaveModel(string modelPath);
        void LoadModel(string modelPath);
        bool IsModelLoaded { get; }
    }
}

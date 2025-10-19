using DriveeSmartAssistant.Models.Inputs;
using Microsoft.ML;
using Microsoft.ML.Trainers.LightGbm;
using System.Globalization;

namespace DriveeSmartAssistant.Interfaces
{
    public interface IUserAcceptanceService
    {
        void TrainModel(string csvFilePath);
        float PredictAcceptanceProbability(UserAcceptanceInput input);
        void SaveModel(string modelPath);
        void LoadModel(string modelPath);
        bool IsModelLoaded { get; }
    }
}

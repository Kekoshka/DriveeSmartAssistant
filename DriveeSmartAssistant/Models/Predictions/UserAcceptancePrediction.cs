using Microsoft.ML.Data;

namespace DriveeSmartAssistant.Models.Predictions
{
    public class UserAcceptancePrediction
    {
        [ColumnName("PredictedLabel")]
        public bool PredictedAcceptance { get; set; }

        [ColumnName("Probability")]
        public float AcceptanceProbability { get; set; }
    }
}

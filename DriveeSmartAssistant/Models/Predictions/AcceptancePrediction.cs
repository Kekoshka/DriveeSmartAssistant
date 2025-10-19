using Microsoft.ML.Data;

namespace DriveeSmartAssistant.Models.Predictions
{
    public class AcceptancePrediction
    {
        [ColumnName("PredictedLabel")]
        public bool WillAccept { get; set; }

        [ColumnName("Probability")]
        public float Probability { get; set; }
    }
}

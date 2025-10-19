using Microsoft.ML.Data;

namespace DriveeSmartAssistant.Models.Predictions
{
    public class OrderCompletionPrediction
    {
        [ColumnName("PredictedLabel")]
        public bool WillBeCompleted { get; set; }

        [ColumnName("Probability")]
        public float Probability { get; set; }

        [ColumnName("Score")]
        public float Score { get; set; }
    }
}

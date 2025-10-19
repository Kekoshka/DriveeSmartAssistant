using Microsoft.ML.Data;

namespace DriveeSmartAssistant.Classes.Predictions
{
    public class RecommendedPricePrediction
    {
        [ColumnName("Score")]
        public float RecommendedPrice { get; set; }
    }
}

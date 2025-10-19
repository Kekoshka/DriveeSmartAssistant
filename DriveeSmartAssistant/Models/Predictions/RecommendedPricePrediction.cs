using Microsoft.ML.Data;

namespace DriveeSmartAssistant.Models.Predictions
{
    public class RecommendedPricePrediction
    {
        [ColumnName("Score")]
        public float RecommendedPrice { get; set; }
    }
}

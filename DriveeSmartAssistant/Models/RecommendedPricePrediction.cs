using Microsoft.ML.Data;

namespace DriveeSmartAssistant.Models
{
    public class RecommendedPricePrediction
    {
        [ColumnName("Score")]
        public float RecommendedPrice { get; set; }
    }
}

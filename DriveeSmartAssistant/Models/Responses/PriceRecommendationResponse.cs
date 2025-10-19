namespace DriveeSmartAssistant.Models.Responses
{
    public class PriceRecommendationResponse
    {
        public float RecommendedPrice { get; set; }
        public string Currency { get; set; } = "local";
        public DateTime Timestamp { get; set; }
    }
}

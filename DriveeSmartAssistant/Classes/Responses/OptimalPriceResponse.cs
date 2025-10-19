namespace DriveeSmartAssistant.Classes.Responses
{
    public class OptimalPriceResponse
    {
        public float OptimalPrice { get; set; }
        public float DriverAcceptanceProbability { get; set; }
        public float UserAcceptanceProbability { get; set; }
        public float CombinedProbability { get; set; }
        public DateTime Timestamp { get; set; }
    }
}

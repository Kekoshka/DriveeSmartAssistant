namespace DriveeSmartAssistant.Classes.Responses
{
    public class AcceptanceProbabilityResponse
    {
        public float UserPrice { get; set; }
        public float DriverPrice { get; set; }
        public float DriverAcceptanceProbability { get; set; }
        public float UserAcceptanceProbability { get; set; }
        public DateTime Timestamp { get; set; }
    }
}

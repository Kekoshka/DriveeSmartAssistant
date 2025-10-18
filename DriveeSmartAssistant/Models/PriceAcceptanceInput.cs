namespace DriveeSmartAssistant.Models
{
    public class PriceAcceptanceInput
    {
        public float ProposedPrice { get; set; }
        public float DistanceInMeters { get; set; }
        public float DurationInSeconds { get; set; }
        public float DriverRating { get; set; }
        public float PickupInMeters { get; set; }
        public float DriverExperienceDays { get; set; }
        public string TimeOfDay { get; set; }
    }
}

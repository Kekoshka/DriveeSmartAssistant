namespace DriveeSmartAssistant.Models.Mapping
{
    public class ProcessedRideData
    {
        public float DistanceInMeters { get; set; }
        public float DurationInSeconds { get; set; }
        public float DriverRating { get; set; }
        public float PickupInMeters { get; set; }
        public float DriverExperienceDays { get; set; }
        public string TimeOfDay { get; set; }
        public string Platform { get; set; }
        public float PriceBidLocal { get; set; }
        public float PriceStartLocal { get; set; }
        public bool IsDone { get; set; }
    }
}

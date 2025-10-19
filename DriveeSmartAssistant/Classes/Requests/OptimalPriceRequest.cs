namespace DriveeSmartAssistant.Classes.Requests
{
    public class OptimalPriceRequest
    {
        public float DistanceInMeters { get; set; }
        public float DurationInSeconds { get; set; }
        public float DriverRating { get; set; }
        public float PickupInMeters { get; set; }
        public float DriverExperienceDays { get; set; }
        public string TimeOfDay { get; set; }
        public float MinPrice { get; set; } = 50;
        public float MaxPrice { get; set; } = 1000;
        public float Step { get; set; } = 10;
    }
}

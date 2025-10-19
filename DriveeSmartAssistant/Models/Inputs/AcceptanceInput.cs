namespace DriveeSmartAssistant.Models.Inputs
{
    public class AcceptanceInput
    {
        public float DistanceInMeters { get; set; }
        public float DurationInSeconds { get; set; }
        public float DriverRating { get; set; }
        public float PickupInMeters { get; set; }
        public float DriverExperienceDays { get; set; }
        public string CarName { get; set; } = "Unknown";

        // Временные признаки
        public float HourOfDay { get; set; }
        public float DayOfWeek { get; set; }
        public float Month { get; set; }
        public float IsWeekend { get; set; }
        public float IsPeakHour { get; set; }

        // Ценовые признаки
        public float ProposedPrice { get; set; }
        public float PriceStartLocal { get; set; }

        // Категориальные признаки
        public string Platform { get; set; } = "android";
        public string TimeOfDay { get; set; } 
    }
}

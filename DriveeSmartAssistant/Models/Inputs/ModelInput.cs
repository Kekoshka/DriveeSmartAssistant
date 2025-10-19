namespace DriveeSmartAssistant.Models.Inputs
{
    public class ModelInput
    {
        public float DistanceInMeters { get; set; }
        public float DurationInSeconds { get; set; }
        public float DriverRating { get; set; }
        public float PickupInMeters { get; set; }
        public float DriverExperienceMonth { get; set; }
        public string CarName { get; set; }

        public string TimeOfDay { get; set; }
        public float HourOfDay { get; set; } 
        public float DayOfWeek { get; set; } 
        public float Month { get; set; } 
        public float IsWeekend { get; set; } 
        public float IsPeakHour { get; set; } 

        public string Platform { get; set; }
        public float PriceBidLocal { get; set; }
        public float PriceStartLocal { get; set; }

        public float PriceRatio => PriceStartLocal > 0 ? PriceBidLocal / PriceStartLocal : 1.0f;
        public float PriceDifference => PriceBidLocal - PriceStartLocal;

        public bool IsDone { get; set; } 
        public bool UserAccepted { get; set; } 
        public bool DriverAccepted { get; set; } 
    }
}

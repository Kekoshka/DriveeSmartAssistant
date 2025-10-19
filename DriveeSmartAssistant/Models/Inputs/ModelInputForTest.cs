using Microsoft.ML.Data;

namespace DriveeSmartAssistant.Models.Inputs
{
    public class ModelInputForTest
    {
        public float DistanceInMeters { get; set; }
        public float DurationInSeconds { get; set; }
        public float PickupInMeters { get; set; }
        public float PickupInSeconds { get; set; }
        public float PriceBidLocal { get; set; }
        public float PriceStartLocal { get; set; }
        public float DriverRating { get; set; }
        public float DriverExperienceDays { get; set; }
        public string Platform { get; set; }
        public string CarName { get; set; }
        public float HourOfDay { get; set; }
        public float DayOfWeek { get; set; }
        public float Month { get; set; }
        public bool Label { get; set; } // IsDone - целевая переменная
    }

    // Отдельный класс для предсказания (без Label)
    public class ModelInputForPrediction
    {
        public float DistanceInMeters { get; set; }
        public float DurationInSeconds { get; set; }
        public float PickupInMeters { get; set; }
        public float PickupInSeconds { get; set; }
        public float PriceBidLocal { get; set; }
        public float PriceStartLocal { get; set; }
        public float DriverRating { get; set; }
        public float DriverExperienceDays { get; set; }
        public string Platform { get; set; }
        public string CarName { get; set; }
        public float HourOfDay { get; set; }
        public float DayOfWeek { get; set; }
        public float Month { get; set; }
    }
}

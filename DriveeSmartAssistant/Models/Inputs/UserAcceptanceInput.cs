using Microsoft.ML.Data;
using System.ComponentModel.DataAnnotations;

namespace DriveeSmartAssistant.Models.Inputs
{
    public class UserAcceptanceInput
    {
        // Параметры поездки
        public float DistanceInMeters { get; set; }
        public float DurationInSeconds { get; set; }
        public float PickupInMeters { get; set; }
        public float PickupInSeconds { get; set; }

        // Данные водителя
        public float DriverRating { get; set; }
        public float DriverExperienceDays { get; set; }
        public string CarName { get; set; } = "Unknown";
        public string Platform { get; set; } = "android";

        // Ценовые параметры
        public float UserMaxPrice { get; set; }   
        public float DriverPrice { get; set; }     

        // Временные параметры
        public DateTime OrderTime { get; set; } = DateTime.Now;

        // Вычисляемые свойства для удобства (не используются в обучении)
        public float PriceRatio => UserMaxPrice > 0 ? DriverPrice / UserMaxPrice : 1.0f;

        public float PriceDifference => DriverPrice - UserMaxPrice;

        public float HourOfDay => OrderTime.Hour;

        public float DayOfWeek => (int)OrderTime.DayOfWeek;

        public float IsWeekend => OrderTime.DayOfWeek == System.DayOfWeek.Saturday || OrderTime.DayOfWeek == System.DayOfWeek.Sunday ? 1.0f : 0.0f;
        public float IsPeakHour => OrderTime.Hour >= 7 && OrderTime.Hour < 10 || OrderTime.Hour >= 17 && OrderTime.Hour < 20 ? 1.0f : 0.0f;
        // Это свойство нужно только для обучения, но не для предсказания
        public bool UserAccepted { get; set; }
    }
}

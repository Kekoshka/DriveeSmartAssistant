using Microsoft.AspNetCore.Mvc;
using Microsoft.ML.Data;
using System.Globalization;

namespace DriveeSmartAssistant.Models
{
    public class UserAcceptanceData
    {
        [LoadColumn(0)] public string OrderId { get; set; }
        [LoadColumn(1)] public DateTime OrderTimestamp { get; set; }
        [LoadColumn(2)] public float DistanceInMeters { get; set; }
        [LoadColumn(3)] public float DurationInSeconds { get; set; }
        [LoadColumn(4)] public string TenderId { get; set; }
        [LoadColumn(5)] public DateTime TenderTimestamp { get; set; }
        [LoadColumn(6)] public string DriverId { get; set; }
        [LoadColumn(7)] public DateTime DriverRegDate { get; set; }
        [LoadColumn(8)] public string DriverRatingString { get; set; }
        [LoadColumn(9)] public string CarName { get; set; }
        [LoadColumn(10)] public string CarModel { get; set; }
        [LoadColumn(11)] public string Platform { get; set; }
        [LoadColumn(12)] public float PickupInMeters { get; set; }
        [LoadColumn(13)] public float PickupInSeconds { get; set; }
        [LoadColumn(14)] public string UserId { get; set; }
        [LoadColumn(15)] public float PriceStartLocal { get; set; }
        [LoadColumn(16)] public float PriceBidLocal { get; set; }
        [LoadColumn(17)] public string Status { get; set; }

        // Вычисляемые свойства для обучения
        [NoColumn]
        public bool UserAccepted => Status?.ToLower() == "done";

        [NoColumn]
        public float DriverRating => ConvertDriverRating(DriverRatingString);

        [NoColumn]
        public float DriverExperienceDays => (float)(OrderTimestamp - DriverRegDate).TotalDays;

        [NoColumn]
        public float PriceRatio => PriceStartLocal > 0 ? PriceBidLocal / PriceStartLocal : 1.0f;

        [NoColumn]
        public float PriceDifference => PriceBidLocal - PriceStartLocal;

        [NoColumn]
        public float HourOfDay => (float)OrderTimestamp.Hour;

        [NoColumn]
        public float DayOfWeek => (float)(int)OrderTimestamp.DayOfWeek;

        [NoColumn]
        public bool IsWeekend => OrderTimestamp.DayOfWeek == System.DayOfWeek.Saturday || OrderTimestamp.DayOfWeek == System.DayOfWeek.Sunday;

        [NoColumn]
        public bool IsPeakHour => (OrderTimestamp.Hour >= 7 && OrderTimestamp.Hour < 10) || (OrderTimestamp.Hour >= 17 && OrderTimestamp.Hour < 20);

        private float ConvertDriverRating(string rating)
        {
            if (string.IsNullOrEmpty(rating)) return 5.0f;
            return float.Parse(rating.Replace(",", "."), CultureInfo.InvariantCulture);
        }
    }

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
        public float UserMaxPrice { get; set; }    // Максимальная цена пользователя
        public float DriverPrice { get; set; }     // Цена, предложенная водителем

        // Временные параметры
        public DateTime OrderTime { get; set; } = DateTime.Now;

        // Вычисляемые свойства для удобства (не используются в обучении)
        public float PriceRatio => UserMaxPrice > 0 ? DriverPrice / UserMaxPrice : 1.0f;

        public float PriceDifference => DriverPrice - UserMaxPrice;

        public float HourOfDay => (float)OrderTime.Hour;

        public float DayOfWeek => (float)(int)OrderTime.DayOfWeek;

        public float IsWeekend => (OrderTime.DayOfWeek == System.DayOfWeek.Saturday || OrderTime.DayOfWeek == System.DayOfWeek.Sunday) ? 1.0f : 0.0f;
        public float IsPeakHour => ((OrderTime.Hour >= 7 && OrderTime.Hour < 10) || (OrderTime.Hour >= 17 && OrderTime.Hour < 20)) ? 1.0f : 0.0f;
        // Это свойство нужно только для обучения, но не для предсказания
        public bool UserAccepted { get; set; }
    }
    public class UserAcceptancePrediction
    {
        [ColumnName("PredictedLabel")]
        public bool PredictedAcceptance { get; set; }

        [ColumnName("Probability")]
        public float AcceptanceProbability { get; set; }
    }
}

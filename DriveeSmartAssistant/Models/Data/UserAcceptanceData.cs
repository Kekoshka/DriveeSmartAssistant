using Microsoft.AspNetCore.Mvc;
using Microsoft.ML.Data;
using System.Globalization;

namespace DriveeSmartAssistant.Models.Data
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
        public float HourOfDay => OrderTimestamp.Hour;

        [NoColumn]
        public float DayOfWeek => (int)OrderTimestamp.DayOfWeek;

        [NoColumn]
        public bool IsWeekend => OrderTimestamp.DayOfWeek == System.DayOfWeek.Saturday || OrderTimestamp.DayOfWeek == System.DayOfWeek.Sunday;

        [NoColumn]
        public bool IsPeakHour => OrderTimestamp.Hour >= 7 && OrderTimestamp.Hour < 10 || OrderTimestamp.Hour >= 17 && OrderTimestamp.Hour < 20;

        private float ConvertDriverRating(string rating)
        {
            if (string.IsNullOrEmpty(rating)) return 5.0f;
            return float.Parse(rating.Replace(",", "."), CultureInfo.InvariantCulture);
        }
    }
}

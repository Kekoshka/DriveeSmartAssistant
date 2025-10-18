using Microsoft.ML.Data;

namespace DriveeSmartAssistant.Models
{
    public class RideData
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

        // Вычисляемые свойства
        public float DriverRating
        {
            get
            {
                if (string.IsNullOrEmpty(DriverRatingString)) return 5.0f;
                return float.Parse(DriverRatingString.Replace(",", "."));
            }
            set
            {
                DriverRating = value;
            }
        }

        public bool IsDone
        {
            get { return Status?.ToLower() == "done"; }
            set
            {
                IsDone = value;
            }
        }

        public float DriverExperienceDays
        {
            get { return (float)(OrderTimestamp - DriverRegDate).TotalDays; }
            set
            {
                DriverExperienceDays = value;
            }
        }

        public string TimeOfDay
        {
            get
            {
                var hour = OrderTimestamp.Hour;
                if (hour >= 6 && hour < 12) return "Morning";
                if (hour >= 12 && hour < 18) return "Afternoon";
                if (hour >= 18 && hour < 24) return "Evening";
                return "Night";
            }
            set { TimeOfDay = value; }
        }
    }
}

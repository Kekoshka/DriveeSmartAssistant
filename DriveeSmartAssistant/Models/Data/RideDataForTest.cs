using Microsoft.ML.Data;

namespace DriveeSmartAssistant.Models.Data
{
    public class RideDataForTest
    {
        [LoadColumn(0)] public string OrderId { get; set; }
        [LoadColumn(1)] public string OrderTimestampString { get; set; }
        [LoadColumn(2)] public float DistanceInMeters { get; set; }
        [LoadColumn(3)] public float DurationInSeconds { get; set; }
        [LoadColumn(4)] public string TenderId { get; set; }
        [LoadColumn(5)] public string TenderTimestampString { get; set; }
        [LoadColumn(6)] public string DriverId { get; set; }
        [LoadColumn(7)] public string DriverRegDateString { get; set; }
        [LoadColumn(8)] public string DriverRatingString { get; set; }
        [LoadColumn(9)] public string CarModel { get; set; }
        [LoadColumn(10)] public string CarName { get; set; }
        [LoadColumn(11)] public string Platform { get; set; }
        [LoadColumn(12)] public float PickupInMeters { get; set; }
        [LoadColumn(13)] public float PickupInSeconds { get; set; }
        [LoadColumn(14)] public string UserId { get; set; }
        [LoadColumn(15)] public float PriceStartLocal { get; set; }
        [LoadColumn(16)] public float PriceBidLocal { get; set; }
        [LoadColumn(17)] public string Status { get; set; }

        // Вычисляемые свойства
        public DateTime OrderTimestamp
        {
            get { return ParseDateTime(OrderTimestampString); }
            set {  }
        }
        public DateTime DriverRegDate
        {
            get { return ParseDateTime(DriverRegDateString); }
            set {  }
        }
        public bool IsDone => Status?.ToLower() == "done";
        public float DriverRating
        {
            get { return ConvertDriverRating(DriverRatingString); }
            set {  }
        }
        public float DriverExperienceDays => (float)(OrderTimestamp - DriverRegDate).TotalDays;
        public int HourOfDay => OrderTimestamp.Hour;
        public int DayOfWeek => (int)OrderTimestamp.DayOfWeek;
        public int Month => OrderTimestamp.Month;

        private DateTime ParseDateTime(string dateTimeString)
        {
            if (DateTime.TryParse(dateTimeString, out DateTime result))
                return result;

            // Попробуем разные форматы дат
            if (DateTime.TryParseExact(dateTimeString, "dd.MM.yyyy H:mm",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out result))
                return result;

            if (DateTime.TryParseExact(dateTimeString, "yyyy-MM-dd HH:mm:ss",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out result))
                return result;

            return DateTime.MinValue;
        }

        private float ConvertDriverRating(string rating)
        {
            if (string.IsNullOrEmpty(rating)) return 5.0f;
            return float.Parse(rating.Replace(",", "."), System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}

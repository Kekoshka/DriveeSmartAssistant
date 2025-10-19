using Microsoft.ML.Data;
using System.ComponentModel.DataAnnotations;

namespace DriveeSmartAssistant.Models.Inputs
{
    public class PricePredictionInput
    {
        public float DistanceInMeters { get; set; }
        public float DurationInSeconds { get; set; }
        public float DriverRating { get; set; }
        public float PickupInMeters { get; set; }
        public float DriverExperienceDays { get; set; }
        public string TimeOfDay { get; set; }
        public string Platform { get; set; }
        public string CarName { get; set; }

        // Новые временные параметры - все float
        public float HourOfDay { get; set; }
        public float DayOfWeek { get; set; }
        public float Month { get; set; }
        public float IsWeekend { get; set; }
        public float IsPeakHour { get; set; }
    }
}

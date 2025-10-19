using Microsoft.ML.Data;
using System.ComponentModel.DataAnnotations;

namespace DriveeSmartAssistant.Classes.Inputs
{
    public class DriverAcceptanceInput
    {
        // Параметры поездки
        public float DistanceInMeters { get; set; }
        public float DurationInSeconds { get; set; }
        public float DriverRating { get; set; }
        public float PickupInMeters { get; set; }
        public float DriverExperienceMonth { get; set; }
        public string CarName { get; set; } = "Unknown";
        public string Platform { get; set; } = "android";

        // Временные параметры
        public float HourOfDay { get; set; }
        public float DayOfWeek { get; set; }
        public float Month { get; set; }

        // Ценовые параметры
        public float UserPrice { get; set; } // Цена пассажира
        public float DriverMinPrice { get; set; } // Минимальная цена водителя
    }
}

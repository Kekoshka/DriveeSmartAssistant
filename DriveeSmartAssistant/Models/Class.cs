using Microsoft.ML.Data;
using System.ComponentModel.DataAnnotations;

namespace DriveeSmartAssistant.Models
{

    public class PriceRecommendationRequest
    {
        [Required]
        public float DistanceInMeters { get; set; }

        [Required]
        public float DurationInSeconds { get; set; }

        [Required]
        public float DriverRating { get; set; }

        [Required]
        public float PickupInMeters { get; set; }

        [Required]
        public float DriverExperienceMonth { get; set; }

        [Required]
        public DateTime TimeOfDay { get; set; }

        [Required]
        public string Platform { get; set; } = "android";
        [Required]
        public string CarName { get; set; }
    }
    public class ModelInput
    {
        public float DistanceInMeters { get; set; }
        public float DurationInSeconds { get; set; }
        public float DriverRating { get; set; }
        public float PickupInMeters { get; set; }
        public float DriverExperienceMonth { get; set; }
        public string CarName { get; set; }

        // Временные признаки - ВСЕ должны быть float
        public string TimeOfDay { get; set; }
        public float HourOfDay { get; set; } // Изменили int на float
        public float DayOfWeek { get; set; } // Изменили int на float
        public float Month { get; set; } // Изменили int на float
        public float IsWeekend { get; set; } // Изменили bool на float (1.0 = true, 0.0 = false)
        public float IsPeakHour { get; set; } // Изменили bool на float

        public string Platform { get; set; }
        public float PriceBidLocal { get; set; }
        public float PriceStartLocal { get; set; }

        public float PriceRatio => PriceStartLocal > 0 ? PriceBidLocal / PriceStartLocal : 1.0f;
        public float PriceDifference => PriceBidLocal - PriceStartLocal;

        public bool IsDone { get; set; } // Также меняем bool на float
        public bool UserAccepted { get; set; } // Пассажир принял
        public bool DriverAccepted { get; set; } // Водитель принял
    }

    public class UserAcceptanceInput
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
        public float DriverPrice { get; set; } // Цена водителя
        public float UserMaxPrice { get; set; } // Максимальная цена пользователя
    }

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

    public class PriceRecommendationResponse
    {
        public float RecommendedPrice { get; set; }
        public string Currency { get; set; } = "local";
        public DateTime Timestamp { get; set; }
    }
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
        public string TimeOfDay { get; set; } // оставляем для обратной совместимости
    }
    public class AcceptanceProbabilityRequest
    {
        [Required]
        public float DistanceInMeters { get; set; }

        [Required]
        public float DurationInSeconds { get; set; }

        [Required]
        public float DriverRating { get; set; }

        [Required]
        public float PickupInMeters { get; set; }

        [Required]
        public float DriverExperienceMonth { get; set; }

        [Required]
        public DateTime TimeOfDay { get; set; }

        [Required]
        public string Platform { get; set; } = "android";

        [Required]
        public float UserPrice { get; set; }

        [Required]
        public float DriverPrice { get; set; }
        [Required]
        public string CarName { get; set; }
    }

    public class AcceptanceProbabilityResponse
    {
        public float UserPrice { get; set; }
        public float DriverPrice { get; set; }
        public float DriverAcceptanceProbability { get; set; }
        public float UserAcceptanceProbability { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class OptimalPriceRequest
    {
        public float DistanceInMeters { get; set; }
        public float DurationInSeconds { get; set; }
        public float DriverRating { get; set; }
        public float PickupInMeters { get; set; }
        public float DriverExperienceDays { get; set; }
        public string TimeOfDay { get; set; }
        public float MinPrice { get; set; } = 50;
        public float MaxPrice { get; set; } = 1000;
        public float Step { get; set; } = 10;
    }

    public class OptimalPriceResponse
    {
        public float OptimalPrice { get; set; }
        public float DriverAcceptanceProbability { get; set; }
        public float UserAcceptanceProbability { get; set; }
        public float CombinedProbability { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class ErrorResponse
    {
        public string Error { get; set; }
        public string Details { get; set; }
        public DateTime Timestamp { get; set; }
    }
}

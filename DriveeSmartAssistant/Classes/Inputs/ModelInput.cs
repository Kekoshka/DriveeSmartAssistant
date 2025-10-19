namespace DriveeSmartAssistant.Classes.Inputs
{
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
}

using System.ComponentModel.DataAnnotations;

namespace DriveeSmartAssistant.Classes.Requests
{
    public class UserAcceptanceRequest
    {
        [Required]
        public float DistanceInMeters { get; set; }

        [Required]
        public float DurationInSeconds { get; set; }

        [Required]
        public float PickupInMeters { get; set; }

        [Required]
        public float PickupInSeconds { get; set; }

        [Required]
        public float DriverRating { get; set; }

        [Required]
        public float DriverExperienceMonth { get; set; }
        [Required]
        public float UserMaxPrice { get; set; }    // Максимальная цена пользователя
        [Required]
        public float DriverPrice { get; set; }     // Цена, предложенная водителем

        [Required]
        public DateTime TimeOfDay { get; set; }

        [Required]
        public string Platform { get; set; } = "android";
        [Required]
        public string CarName { get; set; }
    }
}

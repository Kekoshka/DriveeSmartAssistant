using System.ComponentModel.DataAnnotations;

namespace DriveeSmartAssistant.Models.Requests
{
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
}

using DriveeSmartAssistant.Interfaces;
using DriveeSmartAssistant.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DriveeSmartAssistant.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SAController : ControllerBase
    {
        IRidePriceService _ridePriceService;
        ILogger<SAController> _logger;
        public SAController(IRidePriceService ridePriceService, ILogger<SAController> logger)
        {
            _ridePriceService = ridePriceService;
            _logger = logger;
        }

        [HttpPost("recommend")]
        public async Task<ActionResult<PriceRecommendationResponse>> GetPriceRecommendation([FromBody] PriceRecommendationRequest request)
        {
            try
            {
                if (!_ridePriceService.IsModelLoaded)
                {
                    return StatusCode(503, new ErrorResponse
                    {
                        Error = "Service Unavailable",
                        Details = "ML model is not loaded",
                        Timestamp = DateTime.UtcNow
                    });
                }

                var input = new PricePredictionInput
                {
                    DistanceInMeters = request.DistanceInMeters,
                    DurationInSeconds = request.DurationInSeconds,
                    DriverRating = request.DriverRating,
                    PickupInMeters = request.PickupInMeters,
                    DriverExperienceDays = request.DriverExperienceMonth,
                    TimeOfDay = GetTimeOfDay(request.TimeOfDay),
                    CarName = request.CarName,
                    Platform = request.Platform,
                    DayOfWeek = ((float)request.TimeOfDay.DayOfWeek),
                    HourOfDay = (float)request.TimeOfDay.Hour,
                    Month = (float)request.TimeOfDay.Month

                };

                var recommendedPrice = _ridePriceService.GetRecommendedPrice(input);

                _logger.LogInformation($"Price recommendation generated: {recommendedPrice} for distance {request.DistanceInMeters}m");

                return Ok(new PriceRecommendationResponse
                {
                    RecommendedPrice = recommendedPrice,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating price recommendation");
                return StatusCode(500, new ErrorResponse
                {
                    Error = "Internal Server Error",
                    Details = ex.Message,
                    Timestamp = DateTime.UtcNow
                });
            }
        }

        [HttpPost("acceptance-probability")]
        public ActionResult<AcceptanceProbabilityResponse> GetAcceptanceProbability([FromBody] AcceptanceProbabilityRequest request)
        {
            try
            {
                if (!_ridePriceService.IsModelLoaded)
                {
                    return StatusCode(503, new ErrorResponse
                    {
                        Error = "Service Unavailable",
                        Details = "ML model is not loaded",
                        Timestamp = DateTime.UtcNow
                    });
                }

                UserAcceptanceInput userInput = new()
                {
                    CarName = request.CarName,
                    DurationInSeconds = request.DurationInSeconds,
                    DayOfWeek = (float)request.TimeOfDay.DayOfWeek,
                    DistanceInMeters = request.DistanceInMeters,
                    DriverExperienceMonth = request.DriverExperienceMonth,
                    DriverPrice = request.DriverPrice,
                    DriverRating = request.DriverRating,
                    HourOfDay = request.TimeOfDay.Hour,
                    Month = request.TimeOfDay.Month,
                    PickupInMeters = request.PickupInMeters,
                    Platform = request.Platform,
                    UserMaxPrice = request.UserPrice
                };
                var userAcceptanceProbability = _ridePriceService.GetUserAcceptanceProbability(userInput);

                DriverAcceptanceInput driverInput = new()
                {
                    CarName = request.CarName,
                    DurationInSeconds = request.DurationInSeconds,
                    DayOfWeek = (float)request.TimeOfDay.DayOfWeek,
                    DistanceInMeters = request.DistanceInMeters,
                    DriverExperienceMonth = request.DriverExperienceMonth,
                    DriverMinPrice = request.DriverPrice,
                    DriverRating = request.DriverRating,
                    HourOfDay = request.TimeOfDay.Hour,
                    Month = request.TimeOfDay.Month,
                    PickupInMeters = request.PickupInMeters,
                    Platform = request.Platform,
                    UserPrice = request.UserPrice
                };
                var driverAcceptanceProbability = _ridePriceService.GetDriverAcceptanceProbability(driverInput);


                _logger.LogInformation($"Acceptance probabilities calculated - Driver: {driverAcceptanceProbability:P2}, User: {userAcceptanceProbability:P2}");

                return Ok(new AcceptanceProbabilityResponse
                {
                    UserPrice = request.UserPrice,
                    DriverPrice = request.DriverPrice,
                    UserAcceptanceProbability = userAcceptanceProbability,
                    DriverAcceptanceProbability = driverAcceptanceProbability,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating acceptance probability");
                return StatusCode(500, new ErrorResponse
                {
                    Error = "Internal Server Error",
                    Details = ex.Message,
                    Timestamp = DateTime.UtcNow
                });
            }
        }

        [HttpGet("health")]
        public ActionResult HealthCheck()
        {
            var status = new
            {
                Status = _ridePriceService.IsModelLoaded ? "Healthy" : "Unhealthy",
                ModelLoaded = _ridePriceService.IsModelLoaded,
                Timestamp = DateTime.UtcNow
            };

            return _ridePriceService.IsModelLoaded ? Ok(status) : StatusCode(503, status);
        }
        private string GetTimeOfDay(DateTime timestamp)
        {
            var hour = timestamp.Hour;
            if (hour >= 6 && hour < 12) return "Morning";
            if (hour >= 12 && hour < 18) return "Afternoon";
            if (hour >= 18 && hour < 24) return "Evening";
            return "Night";
        }
    }
}

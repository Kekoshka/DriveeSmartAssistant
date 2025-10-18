using DriveeSmartAssistant.Extensions;
using DriveeSmartAssistant.Interfaces;
using DriveeSmartAssistant.Models;
using Mapster;

namespace DriveeSmartAssistant.Services
{
    public class MainHandleService : IMainHandleService
    {
        IRidePriceService _ridePriceService;
        IUserAcceptanceService _userAcceptanceService;
        ILogger<MainHandleService> _logger;
        public MainHandleService(IRidePriceService ridePriceService, IUserAcceptanceService userAcceptanceService, ILogger<MainHandleService> logger)
        {
            _ridePriceService = ridePriceService;
            _userAcceptanceService = userAcceptanceService;
            _logger = logger;
        }

        public float GetUserAcceptance(UserAcceptanceRequest request)
        {
            var driverPrice = request.DriverPrice;
            var priceRecReqest = request.Adapt<PriceRecommendationRequest>();
            var recommendedPrice = GetRecommendedPrice(priceRecReqest);

            var userAccInput = request.Adapt<UserAcceptanceInput>();
            userAccInput.UserMaxPrice = recommendedPrice;
            var basePercent = _userAcceptanceService.PredictAcceptanceProbability(userAccInput);

            userAccInput.DriverPrice = recommendedPrice;
            var maxPercent = _userAcceptanceService.PredictAcceptanceProbability(userAccInput);

            userAccInput.DriverPrice *= 8;
            var minPercent = _userAcceptanceService.PredictAcceptanceProbability(userAccInput);

            var multiplyCoeff = 1 / (maxPercent - minPercent);
            var normalPercent = basePercent * multiplyCoeff;

            return normalPercent;
        }

        public float GetRecommendedPrice(PriceRecommendationRequest request)
        {
            try
            {
                if (!_ridePriceService.IsModelLoaded)
                {
                    return 0;//Exception
                }

                var input = new PricePredictionInput
                {
                    DistanceInMeters = request.DistanceInMeters,
                    DurationInSeconds = request.DurationInSeconds,
                    DriverRating = request.DriverRating,
                    PickupInMeters = request.PickupInMeters,
                    DriverExperienceDays = request.DriverExperienceMonth,
                    TimeOfDay = request.TimeOfDay.GetTimeOfDay(),
                    CarName = request.CarName,
                    Platform = request.Platform,
                    DayOfWeek = ((float)request.TimeOfDay.DayOfWeek),
                    HourOfDay = (float)request.TimeOfDay.Hour,
                    Month = (float)request.TimeOfDay.Month

                };

                var recommendedPrice = _ridePriceService.GetRecommendedPrice(input);

                _logger.LogInformation($"Price recommendation generated: {recommendedPrice} for distance {request.DistanceInMeters}m");

                return recommendedPrice;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating price recommendation");
                return 0;//Exception
            }


        }
    }
}

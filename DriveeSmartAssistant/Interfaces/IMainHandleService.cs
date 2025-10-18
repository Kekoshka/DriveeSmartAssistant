using DriveeSmartAssistant.Models;

namespace DriveeSmartAssistant.Interfaces
{
    public interface IMainHandleService
    {
        public float GetUserAcceptance(UserAcceptanceRequest request);
        public float GetRecommendedPrice(PriceRecommendationRequest request);
    }
}

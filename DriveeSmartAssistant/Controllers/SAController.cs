using DriveeSmartAssistant.Interfaces;
using DriveeSmartAssistant.Models;
using DriveeSmartAssistant.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DriveeSmartAssistant.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SAController : ControllerBase
    {
        IMainHandleService _mainHandleService;
        ILogger<SAController> _logger;
        public SAController(IMainHandleService mainHandleService, ILogger<SAController> logger)
        {
            _mainHandleService = mainHandleService;
            _logger = logger;
        }

        [HttpPost("recommend")]
        public async Task<ActionResult<PriceRecommendationResponse>> GetPriceRecommendation([FromBody] PriceRecommendationRequest request)
        {
            try
            {

                var recommendedPrice = _mainHandleService.GetRecommendedPrice(request);

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

        [HttpPost("predict")]
        public ActionResult<PredictAcceptanceResponse> PredictAcceptance([FromBody] UserAcceptanceRequest request)
        {
            try
            {

                var userAcceptPercent = _mainHandleService.GetUserAcceptance(request);

                return Ok(new
                {
                    userAcceptPercent
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
        
        public class PredictAcceptanceResponse
        {
            public float AcceptanceProbability { get; set; }
            public float UserMaxPrice { get; set; }
            public float DriverPrice { get; set; }
            public float PriceRatio { get; set; }
            public DateTime Timestamp { get; set; }
        }

    }
}

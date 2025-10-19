using System.IO;
using System.Text;
using DriveeSmartAssistant.Interfaces;
using DriveeSmartAssistant.Models.Inputs;
using DriveeSmartAssistant.Models.Predictions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DriveeSmartAssistant.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SATestController : ControllerBase
    {
        private readonly IOrderCompletionService _completionService;
        private readonly ILogger<SATestController> _logger;

        public SATestController(
            IOrderCompletionService completionService,
            ILogger<SATestController> logger)
        {
            _completionService = completionService;
            _logger = logger;
        }

        [HttpPost("train")]
        public IActionResult TrainModel([FromBody] TrainModelRequest request)
        {
            try
            {
                _completionService.TrainModel(request.DataPath);
                return Ok("Модель успешно обучена");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Ошибка обучения: {ex.Message}");
                return BadRequest($"Ошибка обучения: {ex.Message}");
            }
        }

        [HttpPost("predict")]
        public ActionResult<OrderCompletionPrediction> Predict([FromBody] string csvLine)
        {
            try
            {
                var prediction = _completionService.PredictCompletion(csvLine);
                return Ok(prediction);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Ошибка предсказания: {ex.Message}");
                return BadRequest($"Ошибка предсказания: {ex.Message}");
            }
        }

        [HttpPost("predict-from-object")]
        public ActionResult<OrderCompletionPrediction> PredictFromObject([FromBody] ModelInputForPrediction input)
        {
            try
            {
                var prediction = _completionService.Predict(input);
                return Ok(prediction);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Ошибка предсказания: {ex.Message}");
                return BadRequest($"Ошибка предсказания: {ex.Message}");
            }
        }

        [HttpPost("predict-batch")]
        public ActionResult<List<OrderCompletionPrediction>> PredictBatch([FromBody] List<string> csvLines)
        {
            try
            {
                var predictions = _completionService.PredictBatch(csvLines);
                return Ok(predictions);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Ошибка пакетного предсказания: {ex.Message}");
                return BadRequest($"Ошибка пакетного предсказания: {ex.Message}");
            }
        }

        [HttpPost("upload-csv")]
        public async Task<ActionResult<List<PredictionResult>>> PredictFromCsv(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Файл не предоставлен");

            if (!_completionService.IsModelLoaded)
                return BadRequest("Модель не загружена. Сначала обучите модель.");

            try
            {
                var lines = new List<string>();
                using (var reader = new StreamReader(file.OpenReadStream()))
                {
                    string line;
                    while ((line = await reader.ReadLineAsync()) != null)
                    {
                        if (!string.IsNullOrWhiteSpace(line))
                            lines.Add(line);
                    }
                }

                // Пропускаем заголовок если есть
                if (lines.Count > 0 && lines[0].Contains("order_id"))
                    lines.RemoveAt(0);

                var predictions = _completionService.PredictBatch(lines);

                // Создаем CSV содержимое
                var csvContent = new StringBuilder();

                // Добавляем заголовок CSV
                csvContent.AppendLine("order_id,features,WillBeCompleted,Probability");

                int t = 0;
                int f = 0;

                for (int i = 1; i < predictions.Count; i++)
                {
                    csvContent.AppendLine($"{lines[i]},{predictions[i - 1].WillBeCompleted},{predictions[i - 1].Probability}");
                    if (predictions[i - 1].WillBeCompleted)
                        t++;
                    else f++;
                }

                // Преобразуем в массив байтов и возвращаем как файл
                var bytes = Encoding.UTF8.GetBytes(csvContent.ToString());
                var resultFile = File(bytes, "text/csv", "predictions.csv");

                return resultFile;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Ошибка обработки CSV: {ex.Message}");
                return StatusCode(500, $"Ошибка обработки файла: {ex.Message}");
            }
        }
    }

        public class TrainModelRequest
    {
        public string DataPath { get; set; }
    }

    public class PredictionResult
    {
        public string OrderId { get; set; }
        public bool WillBeCompleted { get; set; }
        public float Probability { get; set; }
    }

}

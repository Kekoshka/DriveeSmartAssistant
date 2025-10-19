using DriveeSmartAssistant.Interfaces;
using Microsoft.ML;
using Microsoft.ML.Trainers.LightGbm;
using Microsoft.Extensions.Options;
using DriveeSmartAssistant.Models.Data;
using DriveeSmartAssistant.Models.Inputs;
using DriveeSmartAssistant.Models.Predictions;
using DriveeSmartAssistant.Common.Configs;
using DriveeSmartAssistant.Common.Options;
using Mapster;
namespace DriveeSmartAssistant.Services
{
    public class UserAcceptanceService : IUserAcceptanceService
    {
        private readonly MLContext _mlContext;
        private ITransformer _model;
        private PredictionEngine<UserAcceptanceInput, UserAcceptancePrediction> _predictionEngine;
        private readonly ILogger<UserAcceptanceService> _logger;
        private readonly UserAcceptanceOptions _options;

        public bool IsModelLoaded => _model != null;

        public UserAcceptanceService(ILogger<UserAcceptanceService> logger, IOptions<UserAcceptanceOptions> options)
        {
            _mlContext = new MLContext(seed: 0);
            _logger = logger;
            _options = options.Value;
        }

        public void TrainModel(string csvFilePath)
        {
            try
            {
                if (!File.Exists(csvFilePath))
                {
                    throw new FileNotFoundException($"CSV файл не найден: {csvFilePath}");
                }
                var rawData = LoadRawData(csvFilePath);
                var trainingData = ConvertToTrainingData(rawData);
                var acceptedRides = trainingData.Where(r => r.UserAccepted).ToList();
                var allRides = trainingData;
                if (acceptedRides.Count == 0)
                {
                    throw new InvalidOperationException("Нет принятых заказов для обучения модели");
                }
                var validtrainingData = trainingData.Where(r =>
                        r.UserMaxPrice >= 50 &&
                        r.UserMaxPrice <= 3000 &&
                        r.DriverPrice <= 3000 &&
                        r.DriverPrice >= 50&&
                        r.DistanceInMeters > 0 &&
                        r.DurationInSeconds > 0
                    ).ToList();

                var dataView = _mlContext.Data.LoadFromEnumerable(trainingData);
                var pipeline = UserAcceptanceConfig.CreateUserAcceptanceTrainingPipeline(_mlContext, _options);
                _model = pipeline.Fit(dataView);

                _predictionEngine = _mlContext.Model.CreatePredictionEngine<UserAcceptanceInput, UserAcceptancePrediction>(_model);
                _logger.LogInformation("Модель принятия пользователем успешно обучена");

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обучении модели принятия пользователем");
                throw;
            }
        }

        private List<UserAcceptanceData> LoadRawData(string csvFilePath)
        {
            var rawData = new List<UserAcceptanceData>();
            var lines = File.ReadAllLines(csvFilePath);

            for (int i = 1; i < lines.Length; i++)
            {
                try
                {
                    var line = lines[i];
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    var parts = line.Split(';');
                    if (parts.Length < 18)
                        continue;

                    var rawItem = new UserAcceptanceData
                    {
                        OrderId = parts[0],
                        OrderTimestamp = DateTime.Parse(parts[1]),
                        DistanceInMeters = float.Parse(parts[2]),
                        DurationInSeconds = float.Parse(parts[3]),
                        TenderId = parts[4],
                        TenderTimestamp = DateTime.Parse(parts[5]),
                        DriverId = parts[6],
                        DriverRegDate = DateTime.Parse(parts[7]),
                        DriverRatingString = parts[8],
                        CarName = parts[9],
                        CarModel = parts[10],
                        Platform = parts[11],
                        PickupInMeters = float.Parse(parts[12]),
                        PickupInSeconds = float.Parse(parts[13]),
                        UserId = parts[14],
                        PriceStartLocal = float.Parse(parts[15]),
                        PriceBidLocal = float.Parse(parts[16]),
                        Status = parts[17].ToLower().Trim()
                    };

                    rawData.Add(rawItem);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"Ошибка обработки строки {i + 1}: {ex.Message}");
                }
            }

            return rawData;
        }

        private List<UserAcceptanceInput> ConvertToTrainingData(List<UserAcceptanceData> rawData)
        {
            var trainingData = new List<UserAcceptanceInput>();

            foreach (var rawItem in rawData)
            {
                var trainingItem = rawItem.Adapt<UserAcceptanceInput>();
                trainingItem.UserMaxPrice = rawItem.PriceStartLocal;
                trainingItem.DriverPrice = rawItem.PriceBidLocal;
                trainingItem.OrderTime = rawItem.OrderTimestamp;
                
                trainingData.Add(trainingItem);
            }

            return trainingData;
        }

        public float PredictAcceptanceProbability(UserAcceptanceInput input)
        {
            if (!IsModelLoaded) throw new InvalidOperationException("Модель не загружена");

            var predictionInput = input.Adapt<UserAcceptanceInput>();

            return _predictionEngine.Predict(predictionInput).AcceptanceProbability;

        }

        public void SaveModel(string modelPath)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(modelPath));
            var tempData = _mlContext.Data.LoadFromEnumerable(new[] { new UserAcceptanceInput() });
            _mlContext.Model.Save(_model, tempData.Schema, modelPath);
            _logger.LogInformation("Модель принятия пользователем сохранена");
        }

        public void LoadModel(string modelPath)
        {
            if (!File.Exists(modelPath))
                throw new FileNotFoundException($"Файл модели не найден: {modelPath}");

            var tempData = _mlContext.Data.LoadFromEnumerable(new[] { new UserAcceptanceInput() });
            _model = _mlContext.Model.Load(modelPath, out var modelSchema);
            _predictionEngine = _mlContext.Model.CreatePredictionEngine<UserAcceptanceInput, UserAcceptancePrediction>(_model);
            _logger.LogInformation("Модель принятия пользователем загружена из файла");
        }
    }
}
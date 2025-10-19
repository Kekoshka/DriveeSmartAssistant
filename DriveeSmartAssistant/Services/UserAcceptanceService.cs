using DriveeSmartAssistant.Interfaces;
using DriveeSmartAssistant.Models;
using Microsoft.ML;
using Microsoft.ML.Trainers.LightGbm;
using System.Globalization;
using DriveeSmartAssistant.Classes.Inputs;
using DriveeSmartAssistant.Classes.Predictions;
using DriveeSmartAssistant.Classes.Data;
using DriveeSmartAssistant.Models.Pipelines.Configs;
using DriveeSmartAssistant.Models.Pipelines.Options;
using Microsoft.Extensions.Options;
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

        public UserAcceptanceService(ILogger<UserAcceptanceService> logger, IOptions<UserAcceptanceOptions> o)
        {
            _mlContext = new MLContext(seed: 0);
            _logger = logger;
            _options = o.Value;
        }

        public void TrainModel(string csvFilePath)
        {
            try
            {
                _logger.LogInformation($"Начинаем загрузку данных для модели принятия пользователем из: {csvFilePath}");

                if (!File.Exists(csvFilePath))
                {
                    throw new FileNotFoundException($"CSV файл не найден: {csvFilePath}");
                }

                // Загружаем сырые данные
                var rawData = LoadRawData(csvFilePath);
                _logger.LogInformation($"Загружено сырых данных: {rawData.Count}");

                // Преобразуем в формат для обучения
                var trainingData = ConvertToTrainingData(rawData);
                _logger.LogInformation($"Преобразовано в тренировочные данные: {trainingData.Count}");

                var acceptedRides = trainingData.Where(r => r.UserAccepted).ToList();
                var allRides = trainingData;

                _logger.LogInformation($"Принятых заказов: {acceptedRides.Count}");
                _logger.LogInformation($"Всего заказов: {allRides.Count}");

                if (acceptedRides.Count == 0)
                {
                    throw new InvalidOperationException("Нет принятых заказов для обучения модели");
                }

                var validtrainingData = trainingData.Where(r =>
                        r.UserMaxPrice >= 50 &&
                        r.UserMaxPrice <= 2000 && // Максимальная реалистичная цена
                        r.DriverPrice <= 2000 &&
                        r.DriverPrice >= 50&&
                        r.DistanceInMeters > 0 &&
                        r.DurationInSeconds > 0
                    ).ToList();


                // Создаем dataset
                var dataView = _mlContext.Data.LoadFromEnumerable(trainingData);

                // Pipeline для модели принятия пользователем
                var pipeline = UserAcceptanceConfig.CreateUserAcceptanceTrainingPipeline(_mlContext, _options);

                _logger.LogInformation("Обучаем модель принятия пользователем...");
                _model = pipeline.Fit(dataView);
                _logger.LogInformation("Модель принятия пользователем обучена");

                // Создаем PredictionEngine
                _predictionEngine = _mlContext.Model.CreatePredictionEngine<UserAcceptanceInput, UserAcceptancePrediction>(_model);

                // Логируем статистику
                LogModelStatistics(trainingData);

                _logger.LogInformation("Модель принятия пользователем успешно обучена");

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обучении модели принятия пользователем");
                throw;
            }
        }

        private void ValidateModel(IDataView data, List<UserAcceptanceInput> originalData)
        {
            try
            {
                // Кросс-валидация
                var crossValidationResults = _mlContext.BinaryClassification.CrossValidate(
                    data,
                    (IEstimator<ITransformer>)_model,
                    numberOfFolds: 5,
                    labelColumnName: nameof(UserAcceptanceInput.UserAccepted));

                var avgAccuracy = crossValidationResults.Average(r => r.Metrics.Accuracy);
                var avgAuc = crossValidationResults.Average(r => r.Metrics.AreaUnderRocCurve);
                var avgF1 = crossValidationResults.Average(r => r.Metrics.F1Score);

                _logger.LogInformation($"Результаты кросс-валидации:");
                _logger.LogInformation($"- Точность (Accuracy): {avgAccuracy:P2}");
                _logger.LogInformation($"- AUC: {avgAuc:P2}");
                _logger.LogInformation($"- F1-score: {avgF1:P2}");

            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Не удалось выполнить валидацию модели: {ex.Message}");
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
                var trainingItem = new UserAcceptanceInput
                {
                    // Параметры поездки
                    DistanceInMeters = rawItem.DistanceInMeters,
                    DurationInSeconds = rawItem.DurationInSeconds,
                    PickupInMeters = rawItem.PickupInMeters,
                    PickupInSeconds = rawItem.PickupInSeconds,

                    // Данные водителя
                    DriverRating = rawItem.DriverRating,
                    DriverExperienceDays = rawItem.DriverExperienceDays,
                    CarName = rawItem.CarName,
                    Platform = rawItem.Platform,

                    // Ценовые параметры
                    UserMaxPrice = rawItem.PriceStartLocal,
                    DriverPrice = rawItem.PriceBidLocal,

                    // Временные параметры
                    OrderTime = rawItem.OrderTimestamp,

                    // Метка для обучения
                    UserAccepted = rawItem.UserAccepted
                };

                trainingData.Add(trainingItem);
            }

            return trainingData;
        }

        private IEstimator<ITransformer> CreateTrainingPipeline()
        {
            return _mlContext.Transforms.Categorical.OneHotEncoding("PlatformEncoded", nameof(UserAcceptanceInput.Platform))
                .Append(_mlContext.Transforms.Categorical.OneHotEncoding("CarNameEncoded", nameof(UserAcceptanceInput.CarName)))
                .Append(_mlContext.Transforms.Concatenate("Features",
                    // 1. Ценовые признаки
                    nameof(UserAcceptanceInput.PriceRatio),
                    nameof(UserAcceptanceInput.PriceDifference),

                    // 2. Параметры поездки
                    nameof(UserAcceptanceInput.DistanceInMeters),
                    nameof(UserAcceptanceInput.DurationInSeconds),
                    nameof(UserAcceptanceInput.PickupInMeters),
                    nameof(UserAcceptanceInput.PickupInSeconds),

                    // 3. Качество водителя
                    nameof(UserAcceptanceInput.DriverRating),
                    nameof(UserAcceptanceInput.DriverExperienceDays),

                    // 4. Временные факторы
                    nameof(UserAcceptanceInput.HourOfDay),
                    nameof(UserAcceptanceInput.DayOfWeek),
                    nameof(UserAcceptanceInput.IsWeekend),
                    nameof(UserAcceptanceInput.IsPeakHour),

                    // 5. Категориальные признаки
                    "PlatformEncoded",
                    "CarNameEncoded"
                ))
                .Append(_mlContext.Transforms.NormalizeMinMax("Features"))
                .Append(_mlContext.Transforms.CopyColumns("Label", nameof(UserAcceptanceInput.UserAccepted)))
                .Append(_mlContext.BinaryClassification.Trainers.LightGbm(new LightGbmBinaryTrainer.Options
                {
                    NumberOfIterations = 500,           // Увеличиваем количество итераций
                    LearningRate = 0.05,                // Уменьшаем learning rate
                    NumberOfLeaves = 127,               // Увеличиваем сложность модели
                    MinimumExampleCountPerLeaf = 10,    // Уменьшаем минимальное количество примеров
                    UseCategoricalSplit = true,
                    HandleMissingValue = false,
                    Sigmoid = 0.5,                      // Настраиваем сигмоиду для калибровки вероятностей
                }));
        }

        // Вспомогательный метод для создания кастомных признаков в пайплайне
        private IEstimator<ITransformer> CreateCustomFeaturesPipeline()
        {
            return _mlContext.Transforms.CustomMapping<UserAcceptanceInput, ProcessedFeatures>(
                (input, output) =>
                {
                    output.PriceRatioFeature = input.UserMaxPrice > 0 ? input.DriverPrice / input.UserMaxPrice : 1.0f;
                    output.PriceDifferenceFeature = input.DriverPrice - input.UserMaxPrice;
                    output.HourOfDayFeature = (float)input.OrderTime.Hour;
                    output.DayOfWeekFeature = (float)(int)input.OrderTime.DayOfWeek;
                    output.IsWeekendFeature = (input.OrderTime.DayOfWeek == DayOfWeek.Saturday || input.OrderTime.DayOfWeek == DayOfWeek.Sunday) ? 1.0f : 0.0f;
                    output.IsPeakHourFeature = ((input.OrderTime.Hour >= 7 && input.OrderTime.Hour < 10) || (input.OrderTime.Hour >= 17 && input.OrderTime.Hour < 20)) ? 1.0f : 0.0f;
                },
                "FeatureMapping"
            );
        }

        public float PredictAcceptanceProbability(UserAcceptanceInput input)
        {
            if (!IsModelLoaded) throw new InvalidOperationException("Модель не загружена");

            try
            {
                // UserAccepted не нужно устанавливать для предсказания
                var predictionInput = new UserAcceptanceInput
                {
                    // Параметры поездки
                    DistanceInMeters = input.DistanceInMeters,
                    DurationInSeconds = input.DurationInSeconds,
                    PickupInMeters = input.PickupInMeters,
                    PickupInSeconds = input.PickupInSeconds,

                    // Данные водителя
                    DriverRating = input.DriverRating,
                    DriverExperienceDays = input.DriverExperienceDays,
                    CarName = input.CarName,
                    Platform = input.Platform,

                    // Ценовые параметры
                    UserMaxPrice = input.UserMaxPrice,
                    DriverPrice = input.DriverPrice,

                    // Временные параметры
                    OrderTime = input.OrderTime

                    // UserAccepted не устанавливаем - оно не используется для предсказания
                };

                var prediction = _predictionEngine.Predict(predictionInput);

                _logger.LogInformation(
                    $"Предсказание принятия пользователем: {prediction.AcceptanceProbability:P2} " +
                    $"(цена водителя: {input.DriverPrice}, ожидание пользователя: {input.UserMaxPrice})"
                );

                return prediction.AcceptanceProbability;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при предсказании вероятности принятия");
                return CalculateFallbackProbability(input);
            }
        }

        // Остальные методы остаются без изменений...
        private float CalculateFallbackProbability(UserAcceptanceInput input)
        {
            if (input.UserMaxPrice <= 0) return 0.5f;

            var priceRatio = input.DriverPrice / input.UserMaxPrice;

            if (priceRatio <= 0.7f) return 0.95f;
            if (priceRatio <= 0.9f) return 0.85f;
            if (priceRatio <= 1.0f) return 0.75f;
            if (priceRatio <= 1.1f) return 0.50f;
            if (priceRatio <= 1.3f) return 0.25f;
            return 0.10f;
        }

        private void LogModelStatistics(List<UserAcceptanceInput> trainingData)
        {
            var acceptedCount = trainingData.Count(r => r.UserAccepted);
            var totalCount = trainingData.Count;

            _logger.LogInformation($"Статистика модели принятия пользователем:");
            _logger.LogInformation($"- Принято заказов: {acceptedCount}/{totalCount} ({acceptedCount * 100.0 / totalCount:F1}%)");
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

    // Вспомогательный класс для кастомных признаков
    public class ProcessedFeatures
    {
        public float PriceRatioFeature { get; set; }
        public float PriceDifferenceFeature { get; set; }
        public float HourOfDayFeature { get; set; }
        public float DayOfWeekFeature { get; set; }
        public float IsWeekendFeature { get; set; }
        public float IsPeakHourFeature { get; set; }
    }
}
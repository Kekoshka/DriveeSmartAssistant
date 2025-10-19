using DriveeSmartAssistant.Interfaces;
using DriveeSmartAssistant.Models.Data;
using DriveeSmartAssistant.Models.Inputs;
using DriveeSmartAssistant.Models.Predictions;
using Microsoft.ML;

namespace DriveeSmartAssistant.Services
{
    public class OrderCompletionService : IOrderCompletionService
    {
        private readonly MLContext _mlContext;
        private ITransformer _model;
        private PredictionEngine<ModelInputForPrediction, OrderCompletionPrediction> _predictionEngine;
        private readonly ILogger<OrderCompletionService> _logger;

        public bool IsModelLoaded => _model != null;

        public OrderCompletionService(ILogger<OrderCompletionService> logger)
        {
            _mlContext = new MLContext(seed: 0);
            _logger = logger;
        }

        public void TrainModel(string trainingDataPath)
        {
            try
            {
                _logger.LogInformation("Начало обучения модели...");

                // 1. Ручная загрузка данных из файла
                var rideData = LoadAndParseTrainingData(trainingDataPath);
                _logger.LogInformation($"Успешно загружено {rideData.Count} записей");

                // 2. Преобразование в ModelInput
                var modelInputs = ConvertToModelInputs(rideData);
                _logger.LogInformation($"Преобразовано {modelInputs.Count} записей для обучения");

                // 3. Проверка баланса классов
                var completedCount = modelInputs.Count(x => x.Label);
                var cancelledCount = modelInputs.Count(x => !x.Label);
                _logger.LogInformation($"Выполнено заказов: {completedCount}, Отменено: {cancelledCount}");

                // 4. Загрузка в ML.NET
                var dataView = _mlContext.Data.LoadFromEnumerable(modelInputs);

                // 5. Разделение на train/test
                var trainTestSplit = _mlContext.Data.TrainTestSplit(dataView, testFraction: 0.2);
                var trainData = trainTestSplit.TrainSet;
                var testData = trainTestSplit.TestSet;

                _logger.LogInformation($"Данные для обучения: {trainData.GetRowCount()}, для тестирования: {testData.GetRowCount()}");

                // 6. Создание и обучение pipeline
                var pipeline = CreatePipeline();
                _model = pipeline.Fit(trainData);

                // 7. Оценка модели
                EvaluateModel(testData);

                // 8. Создание PredictionEngine
                _predictionEngine = _mlContext.Model.CreatePredictionEngine<ModelInputForPrediction, OrderCompletionPrediction>(_model);

                _logger.LogInformation("Модель успешно обучена и готова к использованию");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Ошибка обучения модели: {ex.Message}");
                _logger.LogError($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        private List<RideDataForTest> LoadAndParseTrainingData(string filePath)
        {
            var rideData = new List<RideDataForTest>();

            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Файл не найден: {filePath}");

            var lines = File.ReadAllLines(filePath);
            _logger.LogInformation($"Прочитано {lines.Length} строк из файла");

            // Пропускаем заголовок
            for (int i = 1; i < 50000; i++)
            {
                try
                {
                    var line = lines[i].Trim();
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    // Разделитель - табуляция
                    var parts = line.Split(';');
                    if (parts.Length < 18)
                    {
                        _logger.LogWarning($"Строка {i} содержит недостаточно данных: {parts.Length} частей вместо 18");
                        continue;
                    }

                    var data = new RideDataForTest
                    {
                        OrderId = parts[0],
                        OrderTimestamp = ParseDateTime(parts[1]),
                        DistanceInMeters = ParseFloat(parts[2]),
                        DurationInSeconds = ParseFloat(parts[3]),
                        TenderId = parts[4],
                        TenderTimestampString = parts[5],
                        DriverId = parts[6],
                        DriverRegDate = ParseDateTime(parts[7]),
                        DriverRating = ParseDriverRating(parts[8]),
                        CarModel = parts[9],
                        CarName = parts[10],
                        Platform = parts[11],
                        PickupInMeters = ParseFloat(parts[12]),
                        PickupInSeconds = ParseFloat(parts[13]),
                        UserId = parts[14],
                        PriceStartLocal = ParseFloat(parts[15]),
                        PriceBidLocal = ParseFloat(parts[16]),
                        Status = parts[17]?.Trim().ToLower()
                    };

                    
                        rideData.Add(data);
                    
                
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"Ошибка обработки строки {i}: {ex.Message}");
                }
            }

            return rideData;
        }

        private DateTime ParseDateTime(string dateTimeString)
        {
            if (string.IsNullOrWhiteSpace(dateTimeString))
                return DateTime.MinValue;

            try
            {
                // Пробуем разные форматы дат
                if (DateTime.TryParse(dateTimeString, out DateTime result))
                    return result;

                if (DateTime.TryParseExact(dateTimeString, "dd.MM.yyyy H:mm",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out result))
                    return result;

                if (DateTime.TryParseExact(dateTimeString, "yyyy-MM-dd HH:mm:ss",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out result))
                    return result;

                _logger.LogWarning($"Не удалось распарсить дату: {dateTimeString}");
                return DateTime.MinValue;
            }
            catch
            {
                return DateTime.MinValue;
            }
        }

        private float ParseFloat(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return 0f;

            try
            {
                return float.Parse(value.Replace(",", "."), System.Globalization.CultureInfo.InvariantCulture);
            }
            catch
            {
                return 0f;
            }
        }

        private float ParseDriverRating(string rating)
        {
            if (string.IsNullOrWhiteSpace(rating))
                return 5.0f;

            try
            {
                return float.Parse(rating.Replace(",", "."), System.Globalization.CultureInfo.InvariantCulture);
            }
            catch
            {
                return 5.0f;
            }
        }

        private List<ModelInputForTest> ConvertToModelInputs(List<RideDataForTest> rideData)
        {
            var modelInputs = new List<ModelInputForTest>();

            foreach (var ride in rideData)
            {
                try
                {
                    var input = new ModelInputForTest
                    {
                        DistanceInMeters = ride.DistanceInMeters,
                        DurationInSeconds = ride.DurationInSeconds,
                        PickupInMeters = ride.PickupInMeters,
                        PickupInSeconds = ride.PickupInSeconds,
                        PriceBidLocal = ride.PriceBidLocal,
                        PriceStartLocal = ride.PriceStartLocal,
                        DriverRating = ride.DriverRating,
                        DriverExperienceDays = ride.DriverExperienceDays,
                        Platform = string.IsNullOrEmpty(ride.Platform) ? "unknown" : ride.Platform,
                        CarName = string.IsNullOrEmpty(ride.CarName) ? "unknown" : ride.CarName,
                        HourOfDay = ride.HourOfDay,
                        DayOfWeek = ride.DayOfWeek,
                        Month = ride.Month,
                        Label = ride.IsDone
                    };

                    modelInputs.Add(input);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"Ошибка преобразования данных для заказа {ride.OrderId}: {ex.Message}");
                }
            }

            return modelInputs;
        }

        private IEstimator<ITransformer> CreatePipeline()
        {
            // Упрощенный pipeline без преобразования Label в Key
            return _mlContext.Transforms.Categorical.OneHotEncoding("PlatformEncoded", "Platform")
                .Append(_mlContext.Transforms.Categorical.OneHotEncoding("CarNameEncoded", "CarName"))
                .Append(_mlContext.Transforms.Concatenate("Features",
                    "DistanceInMeters",
                    "DurationInSeconds",
                    "PickupInMeters",
                    "PickupInSeconds",
                    "PriceBidLocal",
                    "PriceStartLocal",
                    "DriverRating",
                    "DriverExperienceDays",
                    "PlatformEncoded",
                    "CarNameEncoded"))
                .Append(_mlContext.BinaryClassification.Trainers.LightGbm(
                    new Microsoft.ML.Trainers.LightGbm.LightGbmBinaryTrainer.Options
                    {
                        NumberOfLeaves = 31,
                        MinimumExampleCountPerLeaf = 10,
                        LearningRate = 0.1,
                        NumberOfIterations = 500,
                        LabelColumnName = "Label"
                    }));
        }

        private void EvaluateModel(IDataView testData)
        {
            try
            {
                var predictions = _model.Transform(testData);
                var metrics = _mlContext.BinaryClassification.Evaluate(predictions, "Label");

                _logger.LogInformation("=== РЕЗУЛЬТАТЫ ОЦЕНКИ МОДЕЛИ ===");
                _logger.LogInformation($"Точность (Accuracy): {metrics.Accuracy:P2}");
                _logger.LogInformation($"AUC: {metrics.AreaUnderRocCurve:P2}");
                _logger.LogInformation($"F1-score: {metrics.F1Score:P2}");
                _logger.LogInformation($"Precision: {metrics.PositivePrecision:P2}");
                _logger.LogInformation($"Recall: {metrics.PositiveRecall:P2}");
                _logger.LogInformation($"LogLoss: {metrics.LogLoss:F4}");
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Не удалось оценить модель: {ex.Message}");
            }
        }

        public OrderCompletionPrediction PredictCompletion(string csvLine)
        {
            if (!IsModelLoaded)
                throw new InvalidOperationException("Модель не загружена. Сначала обучите или загрузите модель.");

            try
            {
                var input = ParseCsvLine(csvLine);
                var prediction = _predictionEngine.Predict(input);


                return prediction;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Ошибка предсказания: {ex.Message}");
                throw;
            }
        }

        public List<OrderCompletionPrediction> PredictBatch(List<string> csvLines)
        {
            if (!IsModelLoaded)
                throw new InvalidOperationException("Модель не загружена.");

            var results = new List<OrderCompletionPrediction>();

            foreach (var line in csvLines)
            {
                try
                {
                    var prediction = PredictCompletion(line);
                    results.Add(prediction);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"Ошибка обработки строки: {ex.Message}");
                    results.Add(new OrderCompletionPrediction { WillBeCompleted = false, Probability = 0 });
                }
            }

            return results;
        }

        private ModelInputForPrediction ParseCsvLine(string csvLine)
        {
            var parts = csvLine.Split(',');

            if (parts.Length < 17)
                throw new ArgumentException($"Неверный формат CSV строки. Ожидается 17 частей, получено {parts.Length}");

            return new ModelInputForPrediction
            {
                DistanceInMeters = ParseFloat(parts[2]),
                DurationInSeconds = ParseFloat(parts[3]),
                PickupInMeters = ParseFloat(parts[12]),
                PickupInSeconds = ParseFloat(parts[13]),
                PriceBidLocal = ParseFloat(parts[16]),
                PriceStartLocal = ParseFloat(parts[15]),
                DriverRating = ParseDriverRating(parts[8]),
                DriverExperienceDays = CalculateDriverExperience(parts[7], parts[1]),
                Platform = parts[11] ?? "android",
                CarName = parts[10] ?? "unknown",
                HourOfDay = ParseDateTime(parts[1]).Hour,
                DayOfWeek = (int)ParseDateTime(parts[1]).DayOfWeek,
                Month = ParseDateTime(parts[1]).Month
            };
        }

        private float CalculateDriverExperience(string regDate, string orderDate)
        {
            try
            {
                var registrationDate = ParseDateTime(regDate);
                var orderDateTime = ParseDateTime(orderDate);

                if (registrationDate == DateTime.MinValue || orderDateTime == DateTime.MinValue)
                    return 0f;

                return (float)(orderDateTime - registrationDate).TotalDays;
            }
            catch
            {
                return 0f;
            }
        }

        public OrderCompletionPrediction Predict(ModelInputForPrediction input)
        {
            if (!IsModelLoaded)
                throw new InvalidOperationException("Модель не загружена.");

            return _predictionEngine.Predict(input);
        }

        public void SaveModel(string modelPath)
        {
            if (!IsModelLoaded)
                throw new InvalidOperationException("Нет обученной модели для сохранения");

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(modelPath));

                // Создаем временный DataView для схемы
                var tempData = new List<ModelInputForTest> { new ModelInputForTest() };
                var dataView = _mlContext.Data.LoadFromEnumerable(tempData);

                _mlContext.Model.Save(_model, dataView.Schema, modelPath);

                _logger.LogInformation($"Модель сохранена: {modelPath}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Ошибка сохранения модели: {ex.Message}");
                throw;
            }
        }

        public void LoadModel(string modelPath)
        {
            if (!File.Exists(modelPath))
                throw new FileNotFoundException($"Файл модели не найден: {modelPath}");

            try
            {
                // Создаем временный DataView для схемы
                var tempData = new List<ModelInputForTest> { new ModelInputForTest() };
                var dataView = _mlContext.Data.LoadFromEnumerable(tempData);

                _model = _mlContext.Model.Load(modelPath, out var modelSchema);
                _predictionEngine = _mlContext.Model.CreatePredictionEngine<ModelInputForPrediction, OrderCompletionPrediction>(_model);

                _logger.LogInformation($"Модель загружена: {modelPath}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Ошибка загрузки модели: {ex.Message}");
                throw;
            }
        }
    }
}

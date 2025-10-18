using DriveeSmartAssistant.Interfaces;
using DriveeSmartAssistant.Models;
using Microsoft.Extensions.Logging;
using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Trainers.LightGbm;
using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

namespace DriveeSmartAssistant.Services;
public class RidePriceService : IRidePriceService
{
    private readonly MLContext _mlContext;
    private ITransformer _priceModel;
    private ITransformer _userAcceptanceModel;
    private ITransformer _driverAcceptanceModel;
    private PredictionEngine<ModelInput, AcceptancePrediction> _acceptanceEngine;
    private PredictionEngine<ModelInput, AcceptancePrediction> _userAcceptanceEngine;
    private PredictionEngine<ModelInput, AcceptancePrediction> _driverAcceptanceEngine;
    private readonly ILogger<RidePriceService> _logger;

    public bool IsModelLoaded => _priceModel != null && _userAcceptanceModel != null && _driverAcceptanceModel != null;

    public RidePriceService(ILogger<RidePriceService> logger)
    {
        _mlContext = new MLContext(seed: 0);
        _logger = logger;
    }

    public void TrainModels(string csvFilePath)
    {
        try
        {
            _logger.LogInformation($"Начинаем загрузку данных из: {csvFilePath}");

            if (!File.Exists(csvFilePath))
            {
                throw new FileNotFoundException($"CSV файл не найден: {csvFilePath}");
            }

            // Загружаем и обрабатываем данные
            var processedData = LoadAndProcessData(csvFilePath);
            _logger.LogInformation($"Всего загружено поездок: {processedData.Count}");

            var successfulRides = processedData.Where(r => r.IsDone).ToList();
            var allRides = processedData;

            _logger.LogInformation($"Успешных заказов: {successfulRides.Count}");
            _logger.LogInformation($"Всего заказов: {allRides.Count}");

            if (successfulRides.Count == 0)
            {
                throw new InvalidOperationException("Нет успешных заказов для обучения модели цены");
            }

            // Создаем отдельные datasets
            var successfulDataView = _mlContext.Data.LoadFromEnumerable(successfulRides);
            var allDataView = _mlContext.Data.LoadFromEnumerable(allRides);

            // Фильтруем аномальные цены
            var validSuccessfulRides = successfulRides.Where(r =>
                r.PriceBidLocal >= 50 &&
                r.PriceBidLocal <= 2000 && // Максимальная реалистичная цена
                r.DistanceInMeters > 0 &&
                r.DurationInSeconds > 0
            ).ToList();

            _logger.LogInformation($"После фильтрации аномалий: {validSuccessfulRides.Count} успешных заказов");

            if (validSuccessfulRides.Count == 0)
            {
                throw new InvalidOperationException("Нет валидных успешных заказов для обучения");
            }

            // Улучшенный pipeline с нормализацией
            var pricePipeline = _mlContext.Transforms.Categorical.OneHotEncoding("PlatformEncoded", nameof(ModelInput.Platform))
                .Append(_mlContext.Transforms.Categorical.OneHotEncoding("CarName", nameof(ModelInput.CarName))
                .Append(_mlContext.Transforms.Concatenate("Features",
                    // Базовые числовые признаки
                    nameof(ModelInput.DistanceInMeters),
                    nameof(ModelInput.DurationInSeconds),
                    nameof(ModelInput.DriverRating),
                    nameof(ModelInput.PickupInMeters),
                    nameof(ModelInput.DriverExperienceMonth),

                    // Временные числовые признаки (все float)
                    nameof(ModelInput.HourOfDay),
                    nameof(ModelInput.DayOfWeek),
                    nameof(ModelInput.Month),
                    nameof(ModelInput.IsWeekend),

                    // Закодированные категориальные признаки
                    "PlatformEncoded",
                    "CarName"
                ))
                .Append(_mlContext.Transforms.NormalizeMinMax("Features"))
                .Append(_mlContext.Transforms.CopyColumns("Label", nameof(ModelInput.PriceBidLocal)))
                .Append(_mlContext.Regression.Trainers.LightGbm(new LightGbmRegressionTrainer.Options
                {
                    NumberOfIterations = 100,
                    LearningRate = 0.1,
                    NumberOfLeaves = 25
                })));


            _logger.LogInformation("Обучаем модель цены...");
            _priceModel = pricePipeline.Fit(successfulDataView);
            _logger.LogInformation("Модель цены обучена");

            // 2. Модель принятия пассажиром
            var userAcceptancePipeline = CreateUserAcceptancePipeline();
            _logger.LogInformation("Обучаем модель принятия пассажиром...");
            _userAcceptanceModel = userAcceptancePipeline.Fit(allDataView);
            _logger.LogInformation("Модель принятия пассажиром обучена");

            // 3. Модель принятия водителем
            var driverAcceptancePipeline = CreateDriverAcceptancePipeline();
            _logger.LogInformation("Обучаем модель принятия водителем...");
            _driverAcceptanceModel = driverAcceptancePipeline.Fit(allDataView);
            _logger.LogInformation("Модель принятия водителем обучена");

            // Создаем PredictionEngine
            _userAcceptanceEngine = _mlContext.Model.CreatePredictionEngine<ModelInput, AcceptancePrediction>(_userAcceptanceModel);
            _driverAcceptanceEngine = _mlContext.Model.CreatePredictionEngine<ModelInput, AcceptancePrediction>(_driverAcceptanceModel);
            _logger.LogInformation("Все модели успешно обучены");

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при обучении моделей");
            throw;
        }
    }

    private IEstimator<ITransformer> CreateUserAcceptancePipeline()
    {
        return _mlContext.Transforms.Categorical.OneHotEncoding("PlatformEncoded", nameof(ModelInput.Platform))
            .Append(_mlContext.Transforms.Categorical.OneHotEncoding("CarNameEncoded", nameof(ModelInput.CarName)))
            .Append(_mlContext.Transforms.Concatenate("Features",
                // Параметры поездки
                nameof(ModelInput.DistanceInMeters),
                nameof(ModelInput.DurationInSeconds),
                nameof(ModelInput.DriverRating),
                nameof(ModelInput.PickupInMeters),
                nameof(ModelInput.DriverExperienceMonth),

                // Временные параметры
                nameof(ModelInput.HourOfDay),
                nameof(ModelInput.DayOfWeek),
                nameof(ModelInput.IsWeekend),
                nameof(ModelInput.IsPeakHour),

                // Ценовые параметры (фокус на отношение цены водителя к ожиданиям пользователя)
                nameof(ModelInput.PriceBidLocal),      // Цена водителя
                nameof(ModelInput.PriceStartLocal),    // Ожидания пользователя
                nameof(ModelInput.PriceRatio),         // Отношение цен
                nameof(ModelInput.PriceDifference),    // Разница цен

                // Категориальные признаки
                "PlatformEncoded",
                "CarNameEncoded"
            ))
            .Append(_mlContext.Transforms.NormalizeMinMax("Features"))
            .Append(_mlContext.Transforms.CopyColumns("Label", nameof(ModelInput.UserAccepted)))
            .Append(_mlContext.BinaryClassification.Trainers.LightGbm(new LightGbmBinaryTrainer.Options
            {
                NumberOfIterations = 100,
                LearningRate = 0.1,
                NumberOfLeaves = 20
            }));
    }

    private IEstimator<ITransformer> CreateDriverAcceptancePipeline()
    {
        return _mlContext.Transforms.Categorical.OneHotEncoding("PlatformEncoded", nameof(ModelInput.Platform))
            .Append(_mlContext.Transforms.Categorical.OneHotEncoding("CarNameEncoded", nameof(ModelInput.CarName)))
            .Append(_mlContext.Transforms.Concatenate("Features",
                // Параметры поездки
                nameof(ModelInput.DistanceInMeters),
                nameof(ModelInput.DurationInSeconds),
                nameof(ModelInput.DriverRating),
                nameof(ModelInput.PickupInMeters),
                nameof(ModelInput.DriverExperienceMonth),

                // Временные параметры
                nameof(ModelInput.HourOfDay),
                nameof(ModelInput.DayOfWeek),
                nameof(ModelInput.IsWeekend),
                nameof(ModelInput.IsPeakHour),

                // Ценовые параметры (фокус на выгодность поездки для водителя)
                nameof(ModelInput.PriceBidLocal),      // Предлагаемая цена
                nameof(ModelInput.PriceStartLocal),    // Минимальная приемлемая цена
                nameof(ModelInput.PriceRatio),         // Отношение к минимальной цене
                nameof(ModelInput.PriceDifference),    // Надбавка

                // Категориальные признаки
                "PlatformEncoded",
                "CarNameEncoded"
            ))
            .Append(_mlContext.Transforms.NormalizeMinMax("Features"))
            .Append(_mlContext.Transforms.CopyColumns("Label", nameof(ModelInput.DriverAccepted)))
            .Append(_mlContext.BinaryClassification.Trainers.LightGbm(new LightGbmBinaryTrainer.Options
            {
                NumberOfIterations = 100,
                LearningRate = 0.1,
                NumberOfLeaves = 20
            }));
    }

    private List<ModelInput> LoadAndProcessData(string csvFilePath)
    {
        _logger.LogInformation("Загружаем и обрабатываем данные...");

        var processedRides = new List<ModelInput>();
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

                // Парсим исходные данные
                var orderTimestamp = DateTime.Parse(parts[1]);
                var driverRegDate = DateTime.Parse(parts[7]);
                var driverRatingString = parts[8];
                var status = parts[17].ToLower().Trim();
                var isDone = status == "done";


                // Создаем обработанную запись с временными признаками
                var processedRide = new ModelInput
                {
                    // Базовые признаки
                    DistanceInMeters = float.Parse(parts[2]),
                    DurationInSeconds = float.Parse(parts[3]),
                    PickupInMeters = float.Parse(parts[12]),
                    PriceBidLocal = float.Parse(parts[16]),
                    PriceStartLocal = float.Parse(parts[15]),
                    DriverRating = ConvertDriverRating(driverRatingString),
                    DriverExperienceMonth = (float)Math.Round((orderTimestamp - driverRegDate).TotalDays/30),
                    Platform = parts[11],
                    CarName = parts[10],
                    IsDone = status == "done", 

                    // Временные признаки - ВСЕ как float
                    HourOfDay = (float)orderTimestamp.Hour,
                    DayOfWeek = (float)(int)orderTimestamp.DayOfWeek,
                    Month = (float)orderTimestamp.Month,

                    UserAccepted = isDone, // В успешных поездках пользователь принял
                    DriverAccepted = isDone // В успешных поездках водитель принял
                };

                processedRides.Add(processedRide);
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Ошибка обработки строки {i + 1}: {ex.Message}");
            }
        }

        _logger.LogInformation($"Успешно обработано {processedRides.Count} строк");
        return processedRides;
    }

    private string GetTimeOfDay(DateTime timestamp)
    {
        var hour = timestamp.Hour;
        if (hour >= 6 && hour < 12) return "Morning";
        if (hour >= 12 && hour < 18) return "Afternoon";
        if (hour >= 18 && hour < 24) return "Evening";
        return "Night";
    }

    private bool IsWeekend(DateTime timestamp)
    {
        return timestamp.DayOfWeek == DayOfWeek.Saturday ||
               timestamp.DayOfWeek == DayOfWeek.Sunday;
    }

    private float ConvertDriverRating(string ratingString)
    {
        if (string.IsNullOrEmpty(ratingString)) return 5.0f;
        try
        {
            // Используем культуру, где запятая - разделитель дробной части
            var culture = new System.Globalization.CultureInfo("ru-RU");
            return float.Parse(ratingString, culture);
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Не удалось распарсить рейтинг: '{ratingString}', используем 5.0. Ошибка: {ex.Message}");
            return 5.0f;
        }
    }

    public float GetRecommendedPrice(PricePredictionInput input)
    {
        if (!IsModelLoaded) throw new InvalidOperationException("Модель не загружена");

        // Создаем входные данные для модели
        var modelInput = new ModelInput
        {
            DistanceInMeters = input.DistanceInMeters,
            DurationInSeconds = input.DurationInSeconds,
            DriverRating = input.DriverRating,
            PickupInMeters = input.PickupInMeters,
            DriverExperienceMonth = input.DriverExperienceDays,
            TimeOfDay = input.TimeOfDay,
            Platform = input.Platform,
            CarName = input.CarName,

            // ВАЖНО: передаем временные признаки!
            HourOfDay = input.HourOfDay,
            DayOfWeek = input.DayOfWeek,
            Month = input.Month,
        };

        var predictionEngine = _mlContext.Model.CreatePredictionEngine<ModelInput, RecommendedPricePrediction>(_priceModel);
        var result = predictionEngine.Predict(modelInput);
        return result.RecommendedPrice;
    }

    public void SaveModels(string priceModelPath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(priceModelPath));

        // Создаем временные данные для схемы
        var tempData = _mlContext.Data.LoadFromEnumerable(new[] { new ModelInput() });

        _mlContext.Model.Save(_priceModel, tempData.Schema, priceModelPath);

        _logger.LogInformation("Модели сохранены");
    }

    public void LoadModels(string priceModelPath)
    {
        if (!File.Exists(priceModelPath))
        {
            throw new FileNotFoundException("Файлы моделей не найдены");
        }

        var tempData = _mlContext.Data.LoadFromEnumerable(new[] { new ModelInput() });

        _priceModel = _mlContext.Model.Load(priceModelPath, out var priceSchema);

        // Создаем PredictionEngine для загруженной модели

        _logger.LogInformation("Модели загружены из файлов");
    }
}



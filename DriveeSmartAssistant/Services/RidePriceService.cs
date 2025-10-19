using DriveeSmartAssistant.Interfaces;
using Microsoft.ML;
using Microsoft.ML.Trainers.LightGbm;
using Microsoft.Extensions.Options;
using DriveeSmartAssistant.Models.Inputs;
using DriveeSmartAssistant.Models.Predictions;
using DriveeSmartAssistant.Common.Configs;
using DriveeSmartAssistant.Common.Options;
using Mapster;

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
    private readonly RidePriceOptions _options;

    public bool IsModelLoaded => _priceModel != null && _userAcceptanceModel != null && _driverAcceptanceModel != null;

    public RidePriceService(ILogger<RidePriceService> logger, IOptions<RidePriceOptions> o)
    {
        _mlContext = new MLContext(seed: 0);
        _logger = logger;
        _options = o.Value;
    }

    public void TrainModels(string csvFilePath)
    {
            if (!File.Exists(csvFilePath))
            {
                throw new FileNotFoundException($"CSV файл не найден: {csvFilePath}");
            }

            var processedData = LoadAndProcessData(csvFilePath);

            var successfulRides = processedData.Where(r => r.IsDone).ToList();
            var allRides = processedData;

            if (successfulRides.Count == 0)
            {
                throw new InvalidOperationException("Нет успешных заказов для обучения модели цены");
            }

            var successfulDataView = _mlContext.Data.LoadFromEnumerable(successfulRides);
            var allDataView = _mlContext.Data.LoadFromEnumerable(allRides);

            var validSuccessfulRides = successfulRides.Where(r =>
                r.PriceBidLocal >= 50 &&
                r.PriceBidLocal <= 3000 && 
                r.DistanceInMeters > 0 &&
                r.DurationInSeconds > 0
            ).ToList();

            if (validSuccessfulRides.Count == 0)
            {
                throw new InvalidOperationException("Нет валидных успешных заказов для обучения");
            }

            var pricePipeline = RidePriceConfig.CreatePricePipeline(_mlContext, _options);
            _priceModel = pricePipeline.Fit(successfulDataView);
            _logger.LogInformation("Модель цены обучена");

            var userAcceptancePipeline = RidePriceConfig.CreateUserAcceptancePipeline(_mlContext, _options);
            _userAcceptanceModel = userAcceptancePipeline.Fit(allDataView);
            _logger.LogInformation("Модель принятия пассажиром обучена");

            var driverAcceptancePipeline = RidePriceConfig.CreateDriverAcceptancePipeline(_mlContext, _options);
            _driverAcceptanceModel = driverAcceptancePipeline.Fit(allDataView);
            _logger.LogInformation("Модель принятия водителем обучена");

            // Создаем PredictionEngine
            _userAcceptanceEngine = _mlContext.Model.CreatePredictionEngine<ModelInput, AcceptancePrediction>(_userAcceptanceModel);
            _driverAcceptanceEngine = _mlContext.Model.CreatePredictionEngine<ModelInput, AcceptancePrediction>(_driverAcceptanceModel);
            _logger.LogInformation("Все модели успешно обучены");
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

        var modelInput = input.Adapt<ModelInput>();
        modelInput.DriverExperienceMonth = input.DriverExperienceDays;

        var predictionEngine = _mlContext.Model.CreatePredictionEngine<ModelInput, RecommendedPricePrediction>(_priceModel);
        var result = predictionEngine.Predict(modelInput);
        return result.RecommendedPrice;
    }

    public void SaveModels(string priceModelPath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(priceModelPath));

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
        _logger.LogInformation("Модели загружены из файлов");
    }
}



using DriveeSmartAssistant.Common.Options;
using DriveeSmartAssistant.Models.Inputs;
using Microsoft.ML;
using Microsoft.ML.Trainers.LightGbm;

namespace DriveeSmartAssistant.Common.Configs
{
    public class RidePriceConfig
    {
        public static IEstimator<ITransformer> CreatePricePipeline(MLContext mlContext, RidePriceOptions options)
        {
            return mlContext.Transforms.Categorical.OneHotEncoding("PlatformEncoded", nameof(ModelInput.Platform))
                .Append(mlContext.Transforms.Categorical.OneHotEncoding("CarName", nameof(ModelInput.CarName))
                .Append(mlContext.Transforms.Concatenate("Features",
                    // Базовые числовые признаки
                    nameof(ModelInput.DistanceInMeters),
                    nameof(ModelInput.DurationInSeconds),
                    nameof(ModelInput.DriverRating),
                    nameof(ModelInput.PickupInMeters),
                    nameof(ModelInput.DriverExperienceMonth),

                    // Временные числовые признаки
                    nameof(ModelInput.HourOfDay),
                    nameof(ModelInput.DayOfWeek),
                    nameof(ModelInput.Month),
                    nameof(ModelInput.IsWeekend),

                    // Закодированные категориальные признаки
                    "PlatformEncoded",
                    "CarName"
                ))
                .Append(mlContext.Transforms.NormalizeMinMax("Features"))
                .Append(mlContext.Transforms.CopyColumns("Label", nameof(ModelInput.PriceBidLocal)))
                .Append(mlContext.Regression.Trainers.LightGbm(new LightGbmRegressionTrainer.Options
                {
                    NumberOfIterations = options.NumberOfIterations,
                    LearningRate = options.LearningRate,
                    NumberOfLeaves = options.NumberOfLeaves + 5
                })));
        }

        // Пайплайн для модели принятия пользователем (бинарная классификация)
        public static IEstimator<ITransformer> CreateUserAcceptancePipeline(MLContext mlContext, RidePriceOptions options)
        {
            return mlContext.Transforms.Categorical.OneHotEncoding("PlatformEncoded", nameof(ModelInput.Platform))
                .Append(mlContext.Transforms.Categorical.OneHotEncoding("CarNameEncoded", nameof(ModelInput.CarName)))
                .Append(mlContext.Transforms.Concatenate("Features",
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

                    // Ценовые параметры
                    nameof(ModelInput.PriceBidLocal),
                    nameof(ModelInput.PriceStartLocal),
                    nameof(ModelInput.PriceRatio),
                    nameof(ModelInput.PriceDifference),

                    // Категориальные признаки
                    "PlatformEncoded",
                    "CarNameEncoded"
                ))
                .Append(mlContext.Transforms.NormalizeMinMax("Features"))
                .Append(mlContext.Transforms.CopyColumns("Label", nameof(ModelInput.UserAccepted)))
                .Append(mlContext.BinaryClassification.Trainers.LightGbm(new LightGbmBinaryTrainer.Options
                {
                    NumberOfIterations = options.NumberOfIterations,
                    LearningRate = options.LearningRate,
                    NumberOfLeaves = options.NumberOfLeaves
                }));
        }

        // Пайплайн для модели принятия водителем (бинарная классификация)
        public static IEstimator<ITransformer> CreateDriverAcceptancePipeline(MLContext mlContext, RidePriceOptions options)
        {
            return mlContext.Transforms.Categorical.OneHotEncoding("PlatformEncoded", nameof(ModelInput.Platform))
                .Append(mlContext.Transforms.Categorical.OneHotEncoding("CarNameEncoded", nameof(ModelInput.CarName)))
                .Append(mlContext.Transforms.Concatenate("Features",
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

                    // Ценовые параметры
                    nameof(ModelInput.PriceBidLocal),
                    nameof(ModelInput.PriceStartLocal),
                    nameof(ModelInput.PriceRatio),
                    nameof(ModelInput.PriceDifference),

                    // Категориальные признаки
                    "PlatformEncoded",
                    "CarNameEncoded"
                ))
                .Append(mlContext.Transforms.NormalizeMinMax("Features"))
                .Append(mlContext.Transforms.CopyColumns("Label", nameof(ModelInput.DriverAccepted)))
                .Append(mlContext.BinaryClassification.Trainers.LightGbm(new LightGbmBinaryTrainer.Options
                {
                    NumberOfIterations = options.NumberOfIterations,
                    LearningRate = options.LearningRate,
                    NumberOfLeaves = options.NumberOfLeaves
                }));
        }
    }
}

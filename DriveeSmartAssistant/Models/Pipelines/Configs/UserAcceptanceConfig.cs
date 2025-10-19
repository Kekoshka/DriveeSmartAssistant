using DriveeSmartAssistant.Classes.Inputs;
using DriveeSmartAssistant.Models.Pipelines.Options;
using Microsoft.ML;
using Microsoft.ML.Trainers.LightGbm;

namespace DriveeSmartAssistant.Models.Pipelines.Configs
{
    public class UserAcceptanceConfig
    {
        public static IEstimator<ITransformer> CreateUserAcceptanceTrainingPipeline(MLContext mlContext, UserAcceptanceOptions options)
        {
            return mlContext.Transforms.Categorical.OneHotEncoding("PlatformEncoded", nameof(UserAcceptanceInput.Platform))
                .Append(mlContext.Transforms.Categorical.OneHotEncoding("CarNameEncoded", nameof(UserAcceptanceInput.CarName)))
                .Append(mlContext.Transforms.Concatenate("Features",
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
                .Append(mlContext.Transforms.NormalizeMinMax("Features"))
                .Append(mlContext.Transforms.CopyColumns("Label", nameof(UserAcceptanceInput.UserAccepted)))
                .Append(mlContext.BinaryClassification.Trainers.LightGbm(new LightGbmBinaryTrainer.Options
                {
                    NumberOfIterations = options.NumberOfIterations,
                    LearningRate = options.LearningRate,
                    NumberOfLeaves = options.NumberOfLeaves,
                    MinimumExampleCountPerLeaf = options.MinimumExampleCountPerLeaf,
                    UseCategoricalSplit = options.UseCategoricalSplit,
                    HandleMissingValue = options.HandleMissingValue,
                    Sigmoid = options.Sigmoid
                }));
        }
    }
}

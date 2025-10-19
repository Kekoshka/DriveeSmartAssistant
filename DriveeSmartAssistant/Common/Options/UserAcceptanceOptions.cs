namespace DriveeSmartAssistant.Common.Options
{
    public class UserAcceptanceOptions
    {
        public int NumberOfIterations {  get; set; }
        public float LearningRate { get; set; }
        public int NumberOfLeaves { get; set; }
        public int MinimumExampleCountPerLeaf { get; set; }
        public bool UseCategoricalSplit { get; set; }
        public bool HandleMissingValue { get; set; }
        public float Sigmoid { get; set; }
    }
}

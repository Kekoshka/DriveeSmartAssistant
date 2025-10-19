namespace DriveeSmartAssistant.Extensions
{
    public static class DateTimeExtensions
    {
        public static string GetTimeOfDay(this DateTime dateTime)
        {
            var hour = dateTime.Hour;
            if (hour >= 6 && hour < 12) return "Morning";
            if (hour >= 12 && hour < 18) return "Afternoon";
            if (hour >= 18 && hour < 24) return "Evening";
            return "Night";
        }
    }
}

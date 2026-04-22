namespace WebAPI.Services.Helper
{
    // Helpers/TimeHelper.cs
    public static class TimeHelper
    {
        public static DateTime NowVietnam()
        {
            var tz = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
        }
    }
}
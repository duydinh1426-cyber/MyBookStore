namespace WebAPI.Services.Helper
{
<<<<<<< HEAD
    // Helpers/TimeHelper.cs
=======
>>>>>>> f84b213 ( new chatbox)
    public static class TimeHelper
    {
        public static DateTime NowVietnam()
        {
            var tz = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
        }
    }
}
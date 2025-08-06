// Extensions/DateTimeExtensions.cs - Helper methods
namespace Berca_Backend.Extensions
{
    public static class DateTimeExtensions
    {
        private static readonly TimeZoneInfo IndonesiaTimeZone = GetIndonesiaTimeZone();

        private static TimeZoneInfo GetIndonesiaTimeZone()
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById("Asia/Jakarta");
            }
            catch
            {
                try
                {
                    return TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
                }
                catch
                {
                    return TimeZoneInfo.CreateCustomTimeZone(
                        "Indonesia Standard Time",
                        TimeSpan.FromHours(7),
                        "Indonesia Standard Time",
                        "Indonesia Standard Time"
                    );
                }
            }
        }

        public static DateTime ToIndonesiaTime(this DateTime utcDateTime)
        {
            if (utcDateTime.Kind != DateTimeKind.Utc)
                utcDateTime = DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);
            
            return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, IndonesiaTimeZone);
        }

        public static DateTime ToUtcFromIndonesia(this DateTime localDateTime)
        {
            if (localDateTime.Kind == DateTimeKind.Utc)
                return localDateTime;
            
            return TimeZoneInfo.ConvertTimeToUtc(localDateTime, IndonesiaTimeZone);
        }

        public static DateTime IndonesiaNow => DateTime.UtcNow.ToIndonesiaTime();
        
        public static DateTime IndonesiaToday => IndonesiaNow.Date;
    }
}
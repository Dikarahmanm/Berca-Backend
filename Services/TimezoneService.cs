// Services/TimezoneService.cs - Fixed constructor and implementation
using Berca_Backend.Services.Interfaces;
using System.Globalization;

namespace Berca_Backend.Services
{
    public class TimezoneService : ITimezoneService
    {
        public TimeZoneInfo IndonesiaTimeZone { get; }
        private readonly ILogger<TimezoneService> _logger;

        public TimezoneService(ILogger<TimezoneService> logger)
        {
            _logger = logger;
            
            // ✅ Indonesia Western Time (WIB) = UTC+7
            try
            {
                // Try Linux/Docker timezone name first
                IndonesiaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Jakarta");
                _logger.LogInformation("✅ Timezone: Using Asia/Jakarta");
            }
            catch (TimeZoneNotFoundException)
            {
                try
                {
                    // Fallback to Windows timezone name
                    IndonesiaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
                    _logger.LogInformation("✅ Timezone: Using SE Asia Standard Time (Windows)");
                }
                catch (TimeZoneNotFoundException)
                {
                    // Final fallback: Create custom UTC+7
                    IndonesiaTimeZone = TimeZoneInfo.CreateCustomTimeZone(
                        "Indonesia Standard Time",
                        TimeSpan.FromHours(7),
                        "Indonesia Standard Time",
                        "Indonesia Standard Time"
                    );
                    _logger.LogInformation("✅ Timezone: Using custom UTC+7");
                }
            }
        }

        public DateTime UtcToLocal(DateTime utcDateTime)
        {
            if (utcDateTime.Kind != DateTimeKind.Utc)
                utcDateTime = DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);
            
            return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, IndonesiaTimeZone);
        }

        public DateTime LocalToUtc(DateTime localDateTime)
        {
            if (localDateTime.Kind == DateTimeKind.Utc)
                return localDateTime;
            
            return TimeZoneInfo.ConvertTimeToUtc(localDateTime, IndonesiaTimeZone);
        }

        public DateTime Now => UtcToLocal(DateTime.UtcNow);

        public DateTime Today => Now.Date;

        public DateOnly TodayDate => DateOnly.FromDateTime(Today);

        public string FormatIndonesiaTime(DateTime dateTime, string format = "dd/MM/yyyy HH:mm:ss")
        {
            var localTime = dateTime.Kind == DateTimeKind.Utc ? UtcToLocal(dateTime) : dateTime;
            return localTime.ToString(format, new CultureInfo("id-ID"));
        }
    }
}
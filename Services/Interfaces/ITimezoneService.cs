// Services/Interfaces/ITimezoneService.cs
namespace Berca_Backend.Services.Interfaces
{
    public interface ITimezoneService
    {
        DateTime UtcToLocal(DateTime utcDateTime);
        DateTime LocalToUtc(DateTime localDateTime);
        DateTime Now { get; }
        DateTime Today { get; }
        DateOnly TodayDate { get; }
        TimeZoneInfo IndonesiaTimeZone { get; }
        string FormatIndonesiaTime(DateTime dateTime, string format = "dd/MM/yyyy HH:mm:ss");
    }
}
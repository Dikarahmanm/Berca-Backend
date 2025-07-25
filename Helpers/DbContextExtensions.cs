using Berca_Backend.Data;
using Berca_Backend.Models;

public static class DbContextExtensions
{
    public static async Task LogActivityAsync(this AppDbContext context, string username, string action)
    {
        try
        {
            var logActivity = new LogActivity
            {
                Username = username,
                Action = action,
                Timestamp = DateTime.UtcNow
            };

            context.LogActivities.Add(logActivity);
            await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            // Log error but don't throw to avoid breaking the main operation
            Console.WriteLine($"Failed to log activity: {ex.Message}");
        }
    }
}

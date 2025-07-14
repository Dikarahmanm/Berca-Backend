using Berca_Backend.Data;
using Berca_Backend.Models;

public static class DbContextExtensions
{
    public static async Task LogActivityAsync(this AppDbContext context, string adminUsername, string action)
    {
        context.LogActivities.Add(new LogActivity
        {
            Username = adminUsername,
            Action = action,
            Timestamp = DateTime.UtcNow
        });

        await context.SaveChangesAsync();
    }
}

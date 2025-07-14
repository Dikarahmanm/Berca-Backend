using Berca_Backend.Models;

namespace Berca_Backend.Services
{
    public interface IAuthService
    {
        Task<User?> AuthenticateAsync(string username, string password);
        Task<bool> RegisterAsync(string username, string password);
    }
}
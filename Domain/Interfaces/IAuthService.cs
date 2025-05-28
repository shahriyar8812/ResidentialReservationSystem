using Domain.Entities;
using System.Threading.Tasks;

namespace Domain.Interfaces
{
    public interface IAuthService
    {
        Task<User> LoginAsync(string username, string password);
        Task RegisterAsync(User user);
    }
}
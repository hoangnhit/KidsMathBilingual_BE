using Swd392.Api.Infrastructure.Database.Entities;
using System.Threading.Tasks;

namespace Swd392.Api.Domain.Repositories
{
    public interface IAccountRepository : IGenericRepository<Account>
    {
        Task<Account?> GetByUsernameAsync(string username);
        Task<Account?> GetByEmailAsync(string email);
        Task<Account?> GetWithRolesByUsernameAsync(string username);
    }
}

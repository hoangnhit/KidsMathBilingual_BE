using System.Threading.Tasks;

namespace Swd392.Api.Domain.Repositories
{
    public interface IUnitOfWork
    {
        IAccountRepository Accounts { get; }
        Task<int> SaveAsync();
    }
}

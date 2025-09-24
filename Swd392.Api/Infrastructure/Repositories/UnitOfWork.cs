using System.Threading.Tasks;
using Swd392.Api.Domain.Repositories;
using Swd392.Api.Infrastructure.Database;

namespace Swd392.Api.Infrastructure.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly KidsMathDbContext _context;
        public IAccountRepository Accounts { get; }

        // Cách 1: inject repo (khuyên dùng)
        public UnitOfWork(KidsMathDbContext context, IAccountRepository accounts)
        {
            _context = context;
            Accounts = accounts;
        }

        public Task<int> SaveAsync() => _context.SaveChangesAsync();
    }
}

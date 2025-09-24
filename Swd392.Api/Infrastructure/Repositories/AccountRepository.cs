using Microsoft.EntityFrameworkCore;
using Swd392.Api.Domain.Repositories;
using Swd392.Api.Infrastructure.Database;
using Swd392.Api.Infrastructure.Database.Entities;

namespace Swd392.Api.Infrastructure.Repositories
{
    public class AccountRepository : GenericRepository<Account>, IAccountRepository
    {
        private readonly KidsMathDbContext _db;
        public AccountRepository(KidsMathDbContext context) : base(context) => _db = context;

        public Task<Account?> GetByUsernameAsync(string username)
            => _db.Accounts.FirstOrDefaultAsync(a => a.username == username);

        public Task<Account?> GetByEmailAsync(string email)
            => _db.Accounts.FirstOrDefaultAsync(a => a.email == email);

        public Task<Account?> GetWithRolesByUsernameAsync(string username)
            => _db.Accounts.FirstOrDefaultAsync(a => a.username == username);
    }
}

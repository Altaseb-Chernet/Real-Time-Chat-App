using ChatApplication.Core.Modules.Authentication.Contracts;
using ChatApplication.Core.Modules.User.Models;
using ChatApplication.Infrastructure.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace ChatApplication.Infrastructure.Data.Repositories;

public class UserRepository : GenericRepository<AppUser>, IUserRepository
{
    public UserRepository(ApplicationDbContext context) : base(context) { }

    public Task<AppUser?> GetByEmailAsync(string email)
        => _dbSet.FirstOrDefaultAsync(u => u.Email == email);

    public new Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => _context.SaveChangesAsync(cancellationToken);
}

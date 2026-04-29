using ChatApplication.Core.Modules.User.Models;
using ChatApplication.Infrastructure.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace ChatApplication.Infrastructure.Data.Repositories;

public class UserRepository : GenericRepository<User>
{
    public UserRepository(ApplicationDbContext context) : base(context) { }

    public Task<User?> GetByEmailAsync(string email)
        => _dbSet.FirstOrDefaultAsync(u => u.Email == email);
}

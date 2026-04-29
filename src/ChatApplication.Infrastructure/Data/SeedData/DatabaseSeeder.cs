using ChatApplication.Infrastructure.Data.Context;

namespace ChatApplication.Infrastructure.Data.SeedData;

public class DatabaseSeeder
{
    private readonly ApplicationDbContext _context;

    public DatabaseSeeder(ApplicationDbContext context) => _context = context;

    public async Task SeedAsync()
    {
        await _context.Database.EnsureCreatedAsync();
    }
}

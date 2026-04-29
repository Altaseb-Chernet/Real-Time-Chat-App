using ChatApplication.Core.Modules.Chat.Models;
using ChatApplication.Core.Modules.User.Models;
using Microsoft.EntityFrameworkCore;

namespace ChatApplication.Infrastructure.Data.Context;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<ChatRoom> ChatRooms => Set<ChatRoom>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
    }
}

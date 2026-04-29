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
    public DbSet<ChatRoomMember> ChatRoomMembers => Set<ChatRoomMember>();
    public DbSet<PrivateMessage> PrivateMessages => Set<PrivateMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureUser(modelBuilder);
        ConfigureChatRoom(modelBuilder);
        ConfigureMessage(modelBuilder);
        ConfigureChatRoomMember(modelBuilder);
        ConfigurePrivateMessage(modelBuilder);
    }

    private static void ConfigureUser(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");
            entity.HasKey(u => u.Id);
            entity.Property(u => u.Id).HasMaxLength(36).IsRequired();
            entity.Property(u => u.Username).HasMaxLength(100).IsRequired();
            entity.Property(u => u.Email).HasMaxLength(255).IsRequired();
            entity.HasIndex(u => u.Email).IsUnique();
            entity.Property(u => u.PasswordHash).IsRequired();
            entity.Property(u => u.Role).HasMaxLength(50).IsRequired().HasDefaultValue("User");
            entity.Property(u => u.CreatedAt).IsRequired();
        });
    }

    private static void ConfigureChatRoom(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ChatRoom>(entity =>
        {
            entity.ToTable("ChatRooms");
            entity.HasKey(r => r.Id);
            entity.Property(r => r.Id).HasMaxLength(36).IsRequired();
            entity.Property(r => r.Name).HasMaxLength(100).IsRequired();
            entity.Property(r => r.CreatedByUserId).HasMaxLength(36).IsRequired();
            entity.Property(r => r.CreatedAt).IsRequired();

            // ChatRoom -> User (creator)
            entity.HasOne(r => r.CreatedBy)
                  .WithMany(u => u.CreatedRooms)
                  .HasForeignKey(r => r.CreatedByUserId)
                  .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureMessage(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Message>(entity =>
        {
            entity.ToTable("Messages");
            entity.HasKey(m => m.Id);
            entity.Property(m => m.Id).HasMaxLength(36).IsRequired();
            entity.Property(m => m.Content).HasMaxLength(2000).IsRequired();
            entity.Property(m => m.SenderId).HasMaxLength(36).IsRequired();
            entity.Property(m => m.RoomId).HasMaxLength(36).IsRequired();
            entity.Property(m => m.SentAt).IsRequired();
            entity.Property(m => m.IsDeleted).HasDefaultValue(false);

            // Message -> User (sender)
            entity.HasOne(m => m.Sender)
                  .WithMany(u => u.Messages)
                  .HasForeignKey(m => m.SenderId)
                  .OnDelete(DeleteBehavior.Restrict);

            // Message -> ChatRoom
            entity.HasOne(m => m.Room)
                  .WithMany(r => r.Messages)
                  .HasForeignKey(m => m.RoomId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(m => m.RoomId);
            entity.HasIndex(m => m.SentAt);
        });
    }

    private static void ConfigureChatRoomMember(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ChatRoomMember>(entity =>
        {
            entity.ToTable("ChatRoomMembers");
            entity.HasKey(m => new { m.UserId, m.RoomId });
            entity.Property(m => m.UserId).HasMaxLength(36).IsRequired();
            entity.Property(m => m.RoomId).HasMaxLength(36).IsRequired();
            entity.Property(m => m.JoinedAt).IsRequired();

            // Member -> User
            entity.HasOne(m => m.User)
                  .WithMany(u => u.ChatRoomMemberships)
                  .HasForeignKey(m => m.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Member -> ChatRoom
            entity.HasOne(m => m.Room)
                  .WithMany(r => r.Members)
                  .HasForeignKey(m => m.RoomId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigurePrivateMessage(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PrivateMessage>(entity =>
        {
            entity.ToTable("PrivateMessages");
            entity.HasKey(m => m.Id);
            entity.Property(m => m.Id).HasMaxLength(36).IsRequired();
            entity.Property(m => m.Content).HasMaxLength(2000).IsRequired();
            entity.Property(m => m.SenderId).HasMaxLength(36).IsRequired();
            entity.Property(m => m.RecipientId).HasMaxLength(36).IsRequired();
            entity.Property(m => m.SentAt).IsRequired();
            entity.Property(m => m.IsDeleted).HasDefaultValue(false);

            entity.HasOne(m => m.Sender)
                  .WithMany()
                  .HasForeignKey(m => m.SenderId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(m => m.Recipient)
                  .WithMany()
                  .HasForeignKey(m => m.RecipientId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(m => new { m.SenderId, m.RecipientId });
            entity.HasIndex(m => m.SentAt);
        });
    }
}

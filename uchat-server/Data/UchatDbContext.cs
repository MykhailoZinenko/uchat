using Microsoft.EntityFrameworkCore;
using uchat_server.Data.Entities;

namespace uchat_server.Data;

public class UchatDbContext : DbContext
{
    public DbSet<User> Users { get; set; }
    public DbSet<Message> Messages { get; set; }
    public DbSet<Session> Sessions { get; set; }
    public DbSet<Room> Rooms { get; set; }
    public DbSet<RoomMember> RoomMembers { get; set; }

    public UchatDbContext(DbContextOptions<UchatDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Username).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.IsOnline);
            entity.Property(e => e.Username).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Email).HasMaxLength(256);
            entity.Property(e => e.PasswordHash).IsRequired();
            entity.Property(e => e.DisplayName).HasMaxLength(100);
            entity.Property(e => e.Bio).HasMaxLength(500);
            entity.Property(e => e.AvatarUrl).HasMaxLength(500);
            entity.Property(e => e.StatusText).HasMaxLength(200);
            entity.Property(e => e.CreatedAt).IsRequired();
        });

        modelBuilder.Entity<Room>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.RoomType).HasConversion<string>().HasMaxLength(20).IsRequired();
            entity.Property(e => e.RoomName).HasMaxLength(100);
            entity.Property(e => e.RoomDescription).HasMaxLength(500);
            entity.Property(e => e.AvatarUrl).HasMaxLength(500);
            entity.Property(e => e.CreatedAt).IsRequired();

            entity.HasOne(e => e.CreatedBy)
                .WithMany(u => u.CreatedRooms)
                .HasForeignKey(e => e.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RoomMember>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.RoomId, e.UserId }).IsUnique();
            entity.Property(e => e.MemberRole).HasConversion<string>().HasMaxLength(20).IsRequired();
            entity.Property(e => e.JoinedAt).IsRequired();

            entity.HasOne(e => e.Room)
                .WithMany(r => r.Members)
                .HasForeignKey(e => e.RoomId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.User)
                .WithMany(u => u.RoomMemberships)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.LastReadMessage)
                .WithMany()
                .HasForeignKey(e => e.LastReadMessageId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Message>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.MessageType).HasConversion<string>().HasMaxLength(20).IsRequired();
            entity.Property(e => e.ServiceAction).HasConversion<string>().HasMaxLength(30);
            entity.Property(e => e.Content).IsRequired();
            entity.Property(e => e.SentAt).IsRequired();

            entity.HasOne(e => e.Room)
                .WithMany(r => r.Messages)
                .HasForeignKey(e => e.RoomId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Sender)
                .WithMany(u => u.SentMessages)
                .HasForeignKey(e => e.SenderUserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.ReplyToMessage)
                .WithMany()
                .HasForeignKey(e => e.ReplyToMessageId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.ForwardedFromMessage)
                .WithMany()
                .HasForeignKey(e => e.ForwardedFromMessageId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Session>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.SessionToken).IsUnique();
            entity.Property(e => e.SessionToken).HasMaxLength(512).IsRequired();
            entity.Property(e => e.DeviceInfo).HasMaxLength(256);
            entity.Property(e => e.IpAddress).HasMaxLength(45);

            entity.HasOne(e => e.User)
                .WithMany(u => u.Sessions)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}

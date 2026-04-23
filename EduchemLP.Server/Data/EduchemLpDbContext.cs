using EduchemLP.Server.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace EduchemLP.Server.Data;

public class EduchemLpDbContext(DbContextOptions<EduchemLpDbContext> options) : DbContext(options) {
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Account.AccountAccessToken> AccountAccessTokens => Set<Account.AccountAccessToken>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<Computer> Computers => Set<Computer>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
    public DbSet<Reservation> Reservations => Set<Reservation>();
    public DbSet<AppSetting> AppSettings => Set<AppSetting>();
    public DbSet<Log> Logs => Set<Log>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        modelBuilder.Entity<Account>(entity => {
            entity.ToTable("users", "account");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Id).HasColumnName("id").ValueGeneratedOnAdd();
            entity.Property(x => x.DisplayName).HasColumnName("display_name").HasMaxLength(128).IsRequired();
            entity.Property(x => x.Email).HasColumnName("email").HasMaxLength(255).IsRequired();
            entity.Property(x => x.Password).HasColumnName("password").HasMaxLength(255).IsRequired();
            entity.Property(x => x.Class).HasColumnName("class").HasMaxLength(64);
            entity.Property(x => x.Avatar).HasColumnName("avatar").HasMaxLength(2048);
            entity.Property(x => x.Banner).HasColumnName("banner").HasMaxLength(2048);
            entity.Property(x => x.Type).HasColumnName("account_type").HasColumnType("account.account_type_enum").HasDefaultValue(Account.AccountType.STUDENT);
            entity.Property(x => x.Gender).HasColumnName("gender").HasColumnType("account.account_gender_enum");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(x => x.LastUpdated).HasColumnName("last_updated").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(x => x.LastLoggedIn).HasColumnName("last_logged_in");
            entity.Property(x => x.EnableReservation).HasColumnName("enable_reservation").HasDefaultValue(false);

            entity.HasMany(x => x.AccessTokens)
                .WithOne(x => x.User)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Account.AccountAccessToken>(entity => {
            entity.ToTable("users_access_tokens", "account");
            entity.HasKey(x => new { x.Platform, x.UserId });

            entity.Property(x => x.UserId).HasColumnName("user_id");
            entity.Property(x => x.Platform).HasColumnName("platform").HasColumnType("account.account_access_token_platform_enum");
            entity.Property(x => x.AccessToken).HasColumnName("access_token").HasMaxLength(4096);
            entity.Property(x => x.RefreshToken).HasColumnName("refresh_token").HasMaxLength(4096);
            entity.Property(x => x.Type).HasColumnName("token_type").HasColumnType("account.account_access_token_type_enum").HasDefaultValue(Account.AccountAccessToken.AccountAccessTokenType.BEARER);
        });

        modelBuilder.Entity<Room>(entity => {
            entity.ToTable("rooms", "reservations");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Id).HasColumnName("id").HasMaxLength(12).ValueGeneratedNever();
            entity.Property(x => x.Label).HasColumnName("label").HasMaxLength(128).IsRequired();
            entity.Property(x => x.Image).HasColumnName("image").HasMaxLength(2048);
            entity.Property(x => x.LimitOfSeats).HasColumnName("limit_of_seats").HasDefaultValue((ushort)0);
            entity.Property(x => x.Available).HasColumnName("available").HasDefaultValue(true);
        });

        modelBuilder.Entity<Computer>(entity => {
            entity.ToTable("computers", "reservations");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Id).HasColumnName("id").HasMaxLength(12).ValueGeneratedNever();
            entity.Property(x => x.RoomId).HasColumnName("room_id").HasMaxLength(12);
            entity.Property(x => x.IsTeacherPC).HasColumnName("is_teachers_pc").HasDefaultValue(false);
            entity.Property(x => x.Available).HasColumnName("available").HasDefaultValue(true);

            entity.Ignore(x => x.Image);

            entity.HasOne(x => x.Room)
                .WithMany(x => x.Computers)
                .HasForeignKey(x => x.RoomId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<ChatMessage>(entity => {
            entity.ToTable("chat");
            entity.HasKey(x => x.Uuid);
            entity.Property(x => x.Uuid).HasColumnName("uuid").HasMaxLength(36);
            entity.Property(x => x.UserId).HasColumnName("user_id");
            entity.Property(x => x.Message).HasColumnName("message").IsRequired();
            entity.Property(x => x.Date).HasColumnName("date");
            entity.Property(x => x.Deleted).HasColumnName("deleted");
            entity.Property(x => x.ReplyingToUuid).HasColumnName("replying_to_uuid");

            entity.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Reservation>(entity => {
            entity.ToTable("reservations");
            entity.HasKey(x => x.UserId);
            entity.Property(x => x.UserId).HasColumnName("user_id");
            entity.Property(x => x.RoomId).HasColumnName("room_id").HasMaxLength(12);
            entity.Property(x => x.ComputerId).HasColumnName("computer_id").HasMaxLength(12);
            entity.Property(x => x.Note).HasColumnName("note");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");

            entity.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.Room)
                .WithMany()
                .HasForeignKey(x => x.RoomId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(x => x.Computer)
                .WithMany()
                .HasForeignKey(x => x.ComputerId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<AppSetting>(entity => {
            entity.ToTable("settings");
            entity.HasKey(x => x.Property);
            entity.Property(x => x.Property).HasColumnName("property");
            entity.Property(x => x.Value).HasColumnName("value");
        });

        modelBuilder.Entity<Log>(entity => {
            entity.ToTable("logs");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.Type).HasColumnName("type");
            entity.Property(x => x.ExactType).HasColumnName("exact_type");
            entity.Property(x => x.Message).HasColumnName("message");
            entity.Property(x => x.Date).HasColumnName("date");
        });
    }
}

using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using TicketReservation.Domain;

namespace TicketReservation.Common.Persistence;

/// <summary>Yazma tarafı (Command'lar) EF Core + bu context ile çalışır.</summary>
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Seat> Seats => Set<Seat>();
    public DbSet<Reservation> Reservations => Set<Reservation>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Event>(e =>
        {
            e.ToTable("Events");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.Venue).HasMaxLength(200).IsRequired();
            e.HasMany(x => x.Seats).WithOne().HasForeignKey(s => s.EventId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<Seat>(s =>
        {
            s.ToTable("Seats");
            s.HasKey(x => x.Id);
            s.Property(x => x.RowVersion).IsRowVersion();
            s.HasIndex(x => new { x.EventId, x.Number }).IsUnique();
        });

        b.Entity<Reservation>(r =>
        {
            r.ToTable("Reservations");
            r.HasKey(x => x.Id);
            r.Property(x => x.CustomerEmail).HasMaxLength(320).IsRequired();
            r.HasIndex(x => x.SeatId);
            r.HasOne<Seat>().WithMany().HasForeignKey(x => x.SeatId).OnDelete(DeleteBehavior.NoAction);
            r.HasOne<Event>().WithMany().HasForeignKey(x => x.EventId).OnDelete(DeleteBehavior.NoAction);
        });
    }
}

/// <summary>Okuma tarafı (Query'ler) aynı DB'ye Dapper ile ham SQL atar; EF change tracking maliyeti yok.</summary>
public interface ISqlConnectionFactory
{
    SqlConnection Create();
}

public sealed class SqlConnectionFactory(IConfiguration configuration) : ISqlConnectionFactory
{
    private readonly string _connectionString = configuration.GetConnectionString("Default")
        ?? throw new InvalidOperationException("ConnectionStrings:Default bulunamadı.");

    public SqlConnection Create() => new(_connectionString);
}

/// <summary>Birden fazla query'nin paylaştığı SQL parçası: süresi dolmuş "Reserved" koltuk, okumada "Available" sayılır.</summary>
public static class SeatSql
{
    public static string EffectiveStatus(string alias) =>
        $"CASE WHEN {alias}.Status = {(int)SeatStatus.Reserved} AND {alias}.ReservedUntil <= @Now " +
        $"THEN {(int)SeatStatus.Available} ELSE {alias}.Status END";
}

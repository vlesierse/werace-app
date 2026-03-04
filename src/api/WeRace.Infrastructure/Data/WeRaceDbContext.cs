using Microsoft.EntityFrameworkCore;
using WeRace.Domain.Entities;

namespace WeRace.Infrastructure.Data;

public class WeRaceDbContext(DbContextOptions<WeRaceDbContext> options) : DbContext(options)
{
    public DbSet<Season> Seasons => Set<Season>();
    public DbSet<Circuit> Circuits => Set<Circuit>();
    public DbSet<Race> Races => Set<Race>();
    public DbSet<Driver> Drivers => Set<Driver>();
    public DbSet<Constructor> Constructors => Set<Constructor>();
    public DbSet<Status> Statuses => Set<Status>();
    public DbSet<Result> Results => Set<Result>();
    public DbSet<Qualifying> Qualifyings => Set<Qualifying>();
    public DbSet<SprintResult> SprintResults => Set<SprintResult>();
    public DbSet<PitStop> PitStops => Set<PitStop>();
    public DbSet<LapTime> LapTimes => Set<LapTime>();
    public DbSet<DriverStanding> DriverStandings => Set<DriverStanding>();
    public DbSet<ConstructorStanding> ConstructorStandings => Set<ConstructorStanding>();
    public DbSet<ConstructorResult> ConstructorResults => Set<ConstructorResult>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(WeRaceDbContext).Assembly);
    }
}

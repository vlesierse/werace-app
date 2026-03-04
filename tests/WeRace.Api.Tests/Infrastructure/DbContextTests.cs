using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using WeRace.Domain.Entities;
using WeRace.Infrastructure.Data;

namespace WeRace.Api.Tests.Infrastructure;

/// <summary>
/// Tests that <see cref="WeRaceDbContext"/> is configured correctly:
/// all DbSets registered, table names, composite keys, FK relationships, and indexes.
/// Uses Npgsql with a fake connection string to build the model without connecting.
/// </summary>
public class DbContextTests : IDisposable
{
    private readonly WeRaceDbContext _context;
    private readonly IModel _model;

    public DbContextTests()
    {
        var options = new DbContextOptionsBuilder<WeRaceDbContext>()
            .UseNpgsql("Host=fake;Database=fake")
            .UseSnakeCaseNamingConvention()
            .Options;

        _context = new WeRaceDbContext(options);
        _model = _context.Model;
    }

    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    // ── DbSet registration ─────────────────────────────────────────────

    [Fact]
    public void DbContext_AllEntitiesRegisteredAsDbSets()
    {
        _context.Seasons.Should().NotBeNull();
        _context.Circuits.Should().NotBeNull();
        _context.Races.Should().NotBeNull();
        _context.Drivers.Should().NotBeNull();
        _context.Constructors.Should().NotBeNull();
        _context.Statuses.Should().NotBeNull();
        _context.Results.Should().NotBeNull();
        _context.Qualifyings.Should().NotBeNull();
        _context.SprintResults.Should().NotBeNull();
        _context.PitStops.Should().NotBeNull();
        _context.LapTimes.Should().NotBeNull();
        _context.DriverStandings.Should().NotBeNull();
        _context.ConstructorStandings.Should().NotBeNull();
        _context.ConstructorResults.Should().NotBeNull();
    }

    [Fact]
    public void DbContext_Has14EntityTypes()
    {
        var entityTypes = _model.GetEntityTypes()
            .Where(e => !e.IsOwned())
            .ToList();

        entityTypes.Should().HaveCount(14);
    }

    // ── Table names (snake_case) ───────────────────────────────────────

    [Theory]
    [InlineData(typeof(Season), "seasons")]
    [InlineData(typeof(Circuit), "circuits")]
    [InlineData(typeof(Race), "races")]
    [InlineData(typeof(Driver), "drivers")]
    [InlineData(typeof(Constructor), "constructors")]
    [InlineData(typeof(Status), "status")]
    [InlineData(typeof(Result), "results")]
    [InlineData(typeof(Qualifying), "qualifying")]
    [InlineData(typeof(SprintResult), "sprint_results")]
    [InlineData(typeof(PitStop), "pit_stops")]
    [InlineData(typeof(LapTime), "lap_times")]
    [InlineData(typeof(DriverStanding), "driver_standings")]
    [InlineData(typeof(ConstructorStanding), "constructor_standings")]
    [InlineData(typeof(ConstructorResult), "constructor_results")]
    public void EntityConfiguration_TableName_IsSnakeCase(Type entityType, string expectedTableName)
    {
        var entityTypeModel = _model.FindEntityType(entityType);

        entityTypeModel.Should().NotBeNull($"{entityType.Name} should be in the model");
        entityTypeModel!.GetTableName().Should().Be(expectedTableName);
    }

    // ── Composite primary keys ─────────────────────────────────────────

    [Fact]
    public void PitStop_HasCompositeKey_RaceIdDriverIdStop()
    {
        var entityType = _model.FindEntityType(typeof(PitStop))!;
        var primaryKey = entityType.FindPrimaryKey()!;

        var keyPropertyNames = primaryKey.Properties.Select(p => p.GetColumnName()).ToList();

        keyPropertyNames.Should().BeEquivalentTo(["race_id", "driver_id", "stop"]);
    }

    [Fact]
    public void LapTime_HasCompositeKey_RaceIdDriverIdLap()
    {
        var entityType = _model.FindEntityType(typeof(LapTime))!;
        var primaryKey = entityType.FindPrimaryKey()!;

        var keyPropertyNames = primaryKey.Properties.Select(p => p.GetColumnName()).ToList();

        keyPropertyNames.Should().BeEquivalentTo(["race_id", "driver_id", "lap"]);
    }

    [Fact]
    public void SingleKeyEntities_HaveIdPrimaryKey()
    {
        var singleKeyTypes = new[]
        {
            typeof(Season), typeof(Circuit), typeof(Race), typeof(Driver),
            typeof(Constructor), typeof(Status), typeof(Result), typeof(Qualifying),
            typeof(SprintResult), typeof(DriverStanding), typeof(ConstructorStanding),
            typeof(ConstructorResult)
        };

        foreach (var type in singleKeyTypes)
        {
            var entityType = _model.FindEntityType(type)!;
            var pk = entityType.FindPrimaryKey()!;

            pk.Properties.Should().ContainSingle(because: $"{type.Name} should have a single-column PK");
            pk.Properties[0].GetColumnName().Should().Be("id", because: $"{type.Name} PK column should be 'id'");
        }
    }

    // ── FK relationships ───────────────────────────────────────────────

    [Theory]
    [InlineData(typeof(Race), "season_id", typeof(Season))]
    [InlineData(typeof(Race), "circuit_id", typeof(Circuit))]
    [InlineData(typeof(Result), "race_id", typeof(Race))]
    [InlineData(typeof(Result), "driver_id", typeof(Driver))]
    [InlineData(typeof(Result), "constructor_id", typeof(Constructor))]
    [InlineData(typeof(Result), "status_id", typeof(Status))]
    [InlineData(typeof(DriverStanding), "race_id", typeof(Race))]
    [InlineData(typeof(DriverStanding), "driver_id", typeof(Driver))]
    [InlineData(typeof(ConstructorStanding), "race_id", typeof(Race))]
    [InlineData(typeof(ConstructorStanding), "constructor_id", typeof(Constructor))]
    [InlineData(typeof(PitStop), "race_id", typeof(Race))]
    [InlineData(typeof(PitStop), "driver_id", typeof(Driver))]
    [InlineData(typeof(LapTime), "race_id", typeof(Race))]
    [InlineData(typeof(LapTime), "driver_id", typeof(Driver))]
    [InlineData(typeof(Qualifying), "race_id", typeof(Race))]
    [InlineData(typeof(Qualifying), "driver_id", typeof(Driver))]
    [InlineData(typeof(Qualifying), "constructor_id", typeof(Constructor))]
    [InlineData(typeof(SprintResult), "race_id", typeof(Race))]
    [InlineData(typeof(SprintResult), "driver_id", typeof(Driver))]
    [InlineData(typeof(SprintResult), "constructor_id", typeof(Constructor))]
    [InlineData(typeof(SprintResult), "status_id", typeof(Status))]
    [InlineData(typeof(ConstructorResult), "race_id", typeof(Race))]
    [InlineData(typeof(ConstructorResult), "constructor_id", typeof(Constructor))]
    public void ForeignKey_IsConfigured(Type childEntity, string fkColumn, Type parentEntity)
    {
        var entityType = _model.FindEntityType(childEntity)!;
        var foreignKeys = entityType.GetForeignKeys().ToList();

        var fk = foreignKeys.FirstOrDefault(f =>
            f.Properties.Any(p => p.GetColumnName() == fkColumn) &&
            f.PrincipalEntityType.ClrType == parentEntity);

        fk.Should().NotBeNull(
            $"{childEntity.Name} should have a FK on '{fkColumn}' pointing to {parentEntity.Name}");
    }

    // ── Indexes ────────────────────────────────────────────────────────

    [Theory]
    [InlineData(typeof(Race), "season_id")]
    [InlineData(typeof(Race), "circuit_id")]
    [InlineData(typeof(Race), "date")]
    [InlineData(typeof(Result), "race_id")]
    [InlineData(typeof(Result), "driver_id")]
    [InlineData(typeof(Result), "constructor_id")]
    [InlineData(typeof(Driver), "driver_ref")]
    [InlineData(typeof(Driver), "code")]
    [InlineData(typeof(Circuit), "circuit_ref")]
    [InlineData(typeof(PitStop), "race_id")]
    [InlineData(typeof(PitStop), "driver_id")]
    [InlineData(typeof(LapTime), "race_id")]
    [InlineData(typeof(LapTime), "driver_id")]
    [InlineData(typeof(DriverStanding), "race_id")]
    [InlineData(typeof(DriverStanding), "driver_id")]
    public void Index_ExistsOnColumn(Type entityType, string columnName)
    {
        var entity = _model.FindEntityType(entityType)!;
        var indexes = entity.GetIndexes().ToList();

        var hasIndex = indexes.Any(idx =>
            idx.Properties.Any(p => p.GetColumnName() == columnName));

        hasIndex.Should().BeTrue(
            $"{entityType.Name} should have an index involving column '{columnName}'");
    }

    [Fact]
    public void Race_HasUniqueIndexOnSeasonIdAndRound()
    {
        var raceEntity = _model.FindEntityType(typeof(Race))!;
        var indexes = raceEntity.GetIndexes().ToList();

        var compositeIndex = indexes.FirstOrDefault(idx =>
            idx.Properties.Count == 2 &&
            idx.Properties.Any(p => p.GetColumnName() == "season_id") &&
            idx.Properties.Any(p => p.GetColumnName() == "round"));

        compositeIndex.Should().NotBeNull("Race should have a composite index on (season_id, round)");
        compositeIndex!.IsUnique.Should().BeTrue("the (season_id, round) index should be unique");
    }

    [Fact]
    public void Season_HasUniqueIndexOnYear()
    {
        var seasonEntity = _model.FindEntityType(typeof(Season))!;
        var indexes = seasonEntity.GetIndexes().ToList();

        var yearIndex = indexes.FirstOrDefault(idx =>
            idx.Properties.Count == 1 &&
            idx.Properties[0].GetColumnName() == "year");

        yearIndex.Should().NotBeNull("Season should have an index on year");
        yearIndex!.IsUnique.Should().BeTrue("the year index should be unique");
    }

    [Fact]
    public void Driver_HasUniqueIndexOnDriverRef()
    {
        var driverEntity = _model.FindEntityType(typeof(Driver))!;
        var indexes = driverEntity.GetIndexes().ToList();

        var refIndex = indexes.FirstOrDefault(idx =>
            idx.Properties.Count == 1 &&
            idx.Properties[0].GetColumnName() == "driver_ref");

        refIndex.Should().NotBeNull("Driver should have an index on driver_ref");
        refIndex!.IsUnique.Should().BeTrue("the driver_ref index should be unique");
    }

    [Fact]
    public void Circuit_HasUniqueIndexOnCircuitRef()
    {
        var circuitEntity = _model.FindEntityType(typeof(Circuit))!;
        var indexes = circuitEntity.GetIndexes().ToList();

        var refIndex = indexes.FirstOrDefault(idx =>
            idx.Properties.Count == 1 &&
            idx.Properties[0].GetColumnName() == "circuit_ref");

        refIndex.Should().NotBeNull("Circuit should have an index on circuit_ref");
        refIndex!.IsUnique.Should().BeTrue("the circuit_ref index should be unique");
    }

    // ── Column names are snake_case ────────────────────────────────────

    [Fact]
    public void AllColumns_UseSnakeCaseNaming()
    {
        foreach (var entityType in _model.GetEntityTypes())
        {
            var tableName = entityType.GetTableName();
            foreach (var property in entityType.GetProperties())
            {
                var columnName = property.GetColumnName();
                columnName.Should().MatchRegex(
                    @"^[a-z][a-z0-9_]*$",
                    $"column '{columnName}' on table '{tableName}' should be snake_case");
            }
        }
    }
}

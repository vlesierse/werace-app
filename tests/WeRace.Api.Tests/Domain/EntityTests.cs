using System.Reflection;
using WeRace.Domain.Entities;

namespace WeRace.Api.Tests.Domain;

/// <summary>
/// Tests that domain entity classes have the expected properties, navigation properties,
/// and correct nullability types.
/// </summary>
public class EntityTests
{
    // ── Season entity ──────────────────────────────────────────────────

    [Fact]
    public void Season_HasExpectedProperties()
    {
        var properties = typeof(Season).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        properties.Should().Contain(p => p.Name == "Id" && p.PropertyType == typeof(int));
        properties.Should().Contain(p => p.Name == "Year" && p.PropertyType == typeof(int));
        properties.Should().Contain(p => p.Name == "WikipediaUrl");
    }

    [Fact]
    public void Season_HasRacesNavigationCollection()
    {
        var season = new Season();

        season.Races.Should().NotBeNull();
        season.Races.Should().BeEmpty();
    }

    // ── Race entity ────────────────────────────────────────────────────

    [Fact]
    public void Race_HasExpectedProperties()
    {
        var properties = typeof(Race).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        properties.Should().Contain(p => p.Name == "Id" && p.PropertyType == typeof(int));
        properties.Should().Contain(p => p.Name == "SeasonId" && p.PropertyType == typeof(int));
        properties.Should().Contain(p => p.Name == "Round" && p.PropertyType == typeof(int));
        properties.Should().Contain(p => p.Name == "Name" && p.PropertyType == typeof(string));
        properties.Should().Contain(p => p.Name == "CircuitId" && p.PropertyType == typeof(int));
        properties.Should().Contain(p => p.Name == "Date" && p.PropertyType == typeof(DateOnly));
        properties.Should().Contain(p => p.Name == "WikipediaUrl");
    }

    [Fact]
    public void Race_TimeIsNullable()
    {
        typeof(Race).GetProperty("Time")!.PropertyType
            .Should().Be(typeof(TimeOnly?));
    }

    [Fact]
    public void Race_DateIsNotNullable()
    {
        typeof(Race).GetProperty("Date")!.PropertyType
            .Should().Be(typeof(DateOnly));
    }

    [Fact]
    public void Race_HasSeasonNavigation()
    {
        typeof(Race).GetProperty("Season")!.PropertyType
            .Should().Be(typeof(Season));
    }

    [Fact]
    public void Race_HasCircuitNavigation()
    {
        typeof(Race).GetProperty("Circuit")!.PropertyType
            .Should().Be(typeof(Circuit));
    }

    [Fact]
    public void Race_HasChildCollections()
    {
        var race = new Race();

        race.Results.Should().NotBeNull();
        race.Qualifyings.Should().NotBeNull();
        race.SprintResults.Should().NotBeNull();
        race.PitStops.Should().NotBeNull();
        race.LapTimes.Should().NotBeNull();
        race.DriverStandings.Should().NotBeNull();
        race.ConstructorStandings.Should().NotBeNull();
        race.ConstructorResults.Should().NotBeNull();
    }

    [Fact]
    public void Race_SessionDatesAreNullable()
    {
        var nullableDateProperties = new[] { "Fp1Date", "Fp2Date", "Fp3Date", "QualiDate", "SprintDate" };
        var nullableTimeProperties = new[] { "Fp1Time", "Fp2Time", "Fp3Time", "QualiTime", "SprintTime" };

        foreach (var prop in nullableDateProperties)
        {
            typeof(Race).GetProperty(prop)!.PropertyType
                .Should().Be(typeof(DateOnly?), because: $"{prop} should be nullable");
        }

        foreach (var prop in nullableTimeProperties)
        {
            typeof(Race).GetProperty(prop)!.PropertyType
                .Should().Be(typeof(TimeOnly?), because: $"{prop} should be nullable");
        }
    }

    // ── Driver entity ──────────────────────────────────────────────────

    [Fact]
    public void Driver_HasExpectedProperties()
    {
        var properties = typeof(Driver).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        properties.Should().Contain(p => p.Name == "Id" && p.PropertyType == typeof(int));
        properties.Should().Contain(p => p.Name == "DriverRef" && p.PropertyType == typeof(string));
        properties.Should().Contain(p => p.Name == "Forename" && p.PropertyType == typeof(string));
        properties.Should().Contain(p => p.Name == "Surname" && p.PropertyType == typeof(string));
    }

    [Fact]
    public void Driver_DateOfBirthIsNullable()
    {
        typeof(Driver).GetProperty("DateOfBirth")!.PropertyType
            .Should().Be(typeof(DateOnly?));
    }

    [Fact]
    public void Driver_NumberIsNullable()
    {
        typeof(Driver).GetProperty("Number")!.PropertyType
            .Should().Be(typeof(int?));
    }

    [Fact]
    public void Driver_CodeIsNullable()
    {
        typeof(Driver).GetProperty("Code")!.PropertyType
            .Should().Be(typeof(string));
        // Code being nullable is conveyed via NRT annotations; property type is string

        var driver = new Driver();
        driver.Code.Should().BeNull("Code defaults to null");
    }

    [Fact]
    public void Driver_HasNavigationCollections()
    {
        var driver = new Driver();

        driver.Results.Should().NotBeNull();
        driver.Qualifyings.Should().NotBeNull();
        driver.SprintResults.Should().NotBeNull();
        driver.PitStops.Should().NotBeNull();
        driver.LapTimes.Should().NotBeNull();
        driver.DriverStandings.Should().NotBeNull();
    }

    // ── Result entity ──────────────────────────────────────────────────

    [Fact]
    public void Result_HasExpectedProperties()
    {
        var properties = typeof(Result).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        properties.Should().Contain(p => p.Name == "Id" && p.PropertyType == typeof(int));
        properties.Should().Contain(p => p.Name == "RaceId" && p.PropertyType == typeof(int));
        properties.Should().Contain(p => p.Name == "DriverId" && p.PropertyType == typeof(int));
        properties.Should().Contain(p => p.Name == "ConstructorId" && p.PropertyType == typeof(int));
        properties.Should().Contain(p => p.Name == "Grid" && p.PropertyType == typeof(int));
        properties.Should().Contain(p => p.Name == "Points" && p.PropertyType == typeof(decimal));
        properties.Should().Contain(p => p.Name == "Laps" && p.PropertyType == typeof(int));
        properties.Should().Contain(p => p.Name == "StatusId" && p.PropertyType == typeof(int));
    }

    [Fact]
    public void Result_PositionIsNullable()
    {
        typeof(Result).GetProperty("Position")!.PropertyType
            .Should().Be(typeof(int?));
    }

    [Fact]
    public void Result_GridIsNotNullable()
    {
        typeof(Result).GetProperty("Grid")!.PropertyType
            .Should().Be(typeof(int));
    }

    [Fact]
    public void Result_HasNavigationProperties()
    {
        typeof(Result).GetProperty("Race")!.PropertyType.Should().Be(typeof(Race));
        typeof(Result).GetProperty("Driver")!.PropertyType.Should().Be(typeof(Driver));
        typeof(Result).GetProperty("Constructor")!.PropertyType.Should().Be(typeof(Constructor));
        typeof(Result).GetProperty("Status")!.PropertyType.Should().Be(typeof(Status));
    }

    [Fact]
    public void Result_TimeIsNullable()
    {
        var result = new Result();
        result.Time.Should().BeNull("Time defaults to null");
    }

    [Fact]
    public void Result_MillisecondsIsNullable()
    {
        typeof(Result).GetProperty("Milliseconds")!.PropertyType
            .Should().Be(typeof(int?));
    }

    // ── DriverStanding entity ──────────────────────────────────────────

    [Fact]
    public void DriverStanding_HasNavigationProperties()
    {
        typeof(DriverStanding).GetProperty("Race")!.PropertyType.Should().Be(typeof(Race));
        typeof(DriverStanding).GetProperty("Driver")!.PropertyType.Should().Be(typeof(Driver));
    }

    [Fact]
    public void DriverStanding_PositionIsNullable()
    {
        typeof(DriverStanding).GetProperty("Position")!.PropertyType
            .Should().Be(typeof(int?));
    }

    // ── ConstructorStanding entity ─────────────────────────────────────

    [Fact]
    public void ConstructorStanding_HasNavigationProperties()
    {
        typeof(ConstructorStanding).GetProperty("Race")!.PropertyType.Should().Be(typeof(Race));
        typeof(ConstructorStanding).GetProperty("Constructor")!.PropertyType.Should().Be(typeof(Constructor));
    }

    // ── PitStop entity ─────────────────────────────────────────────────

    [Fact]
    public void PitStop_HasCompositeKeyProperties()
    {
        var properties = typeof(PitStop).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        properties.Should().Contain(p => p.Name == "RaceId" && p.PropertyType == typeof(int));
        properties.Should().Contain(p => p.Name == "DriverId" && p.PropertyType == typeof(int));
        properties.Should().Contain(p => p.Name == "Stop" && p.PropertyType == typeof(int));
    }

    [Fact]
    public void PitStop_HasNavigationProperties()
    {
        typeof(PitStop).GetProperty("Race")!.PropertyType.Should().Be(typeof(Race));
        typeof(PitStop).GetProperty("Driver")!.PropertyType.Should().Be(typeof(Driver));
    }

    // ── LapTime entity ─────────────────────────────────────────────────

    [Fact]
    public void LapTime_HasCompositeKeyProperties()
    {
        var properties = typeof(LapTime).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        properties.Should().Contain(p => p.Name == "RaceId" && p.PropertyType == typeof(int));
        properties.Should().Contain(p => p.Name == "DriverId" && p.PropertyType == typeof(int));
        properties.Should().Contain(p => p.Name == "Lap" && p.PropertyType == typeof(int));
    }

    [Fact]
    public void LapTime_HasNavigationProperties()
    {
        typeof(LapTime).GetProperty("Race")!.PropertyType.Should().Be(typeof(Race));
        typeof(LapTime).GetProperty("Driver")!.PropertyType.Should().Be(typeof(Driver));
    }

    // ── Qualifying entity ──────────────────────────────────────────────

    [Fact]
    public void Qualifying_HasExpectedProperties()
    {
        var properties = typeof(Qualifying).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        properties.Should().Contain(p => p.Name == "Id" && p.PropertyType == typeof(int));
        properties.Should().Contain(p => p.Name == "RaceId" && p.PropertyType == typeof(int));
        properties.Should().Contain(p => p.Name == "DriverId" && p.PropertyType == typeof(int));
        properties.Should().Contain(p => p.Name == "ConstructorId" && p.PropertyType == typeof(int));
        properties.Should().Contain(p => p.Name == "Position" && p.PropertyType == typeof(int));
    }

    [Fact]
    public void Qualifying_SessionTimesAreNullableStrings()
    {
        var quali = new Qualifying();

        quali.Q1.Should().BeNull();
        quali.Q2.Should().BeNull();
        quali.Q3.Should().BeNull();
    }

    // ── SprintResult entity ────────────────────────────────────────────

    [Fact]
    public void SprintResult_PositionIsNullable()
    {
        typeof(SprintResult).GetProperty("Position")!.PropertyType
            .Should().Be(typeof(int?));
    }

    [Fact]
    public void SprintResult_HasNavigationProperties()
    {
        typeof(SprintResult).GetProperty("Race")!.PropertyType.Should().Be(typeof(Race));
        typeof(SprintResult).GetProperty("Driver")!.PropertyType.Should().Be(typeof(Driver));
        typeof(SprintResult).GetProperty("Constructor")!.PropertyType.Should().Be(typeof(Constructor));
        typeof(SprintResult).GetProperty("Status")!.PropertyType.Should().Be(typeof(Status));
    }

    // ── Constructor entity ─────────────────────────────────────────────

    [Fact]
    public void Constructor_HasExpectedProperties()
    {
        var properties = typeof(Constructor).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        properties.Should().Contain(p => p.Name == "Id" && p.PropertyType == typeof(int));
        properties.Should().Contain(p => p.Name == "ConstructorRef" && p.PropertyType == typeof(string));
        properties.Should().Contain(p => p.Name == "Name" && p.PropertyType == typeof(string));
    }

    [Fact]
    public void Constructor_HasNavigationCollections()
    {
        var ctor = new Constructor();

        ctor.Results.Should().NotBeNull();
        ctor.Qualifyings.Should().NotBeNull();
        ctor.SprintResults.Should().NotBeNull();
        ctor.ConstructorStandings.Should().NotBeNull();
        ctor.ConstructorResults.Should().NotBeNull();
    }

    // ── ConstructorResult entity ───────────────────────────────────────

    [Fact]
    public void ConstructorResult_PointsIsNullable()
    {
        typeof(ConstructorResult).GetProperty("Points")!.PropertyType
            .Should().Be(typeof(decimal?));
    }

    // ── Status entity ──────────────────────────────────────────────────

    [Fact]
    public void Status_HasExpectedProperties()
    {
        var properties = typeof(Status).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        properties.Should().Contain(p => p.Name == "Id" && p.PropertyType == typeof(int));
        properties.Should().Contain(p => p.Name == "StatusText" && p.PropertyType == typeof(string));
    }

    [Fact]
    public void Status_HasNavigationCollections()
    {
        var status = new Status();

        status.Results.Should().NotBeNull();
        status.SprintResults.Should().NotBeNull();
    }

    // ── Circuit entity ─────────────────────────────────────────────────

    [Fact]
    public void Circuit_HasExpectedProperties()
    {
        var properties = typeof(Circuit).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        properties.Should().Contain(p => p.Name == "Id" && p.PropertyType == typeof(int));
        properties.Should().Contain(p => p.Name == "CircuitRef" && p.PropertyType == typeof(string));
        properties.Should().Contain(p => p.Name == "Name" && p.PropertyType == typeof(string));
        properties.Should().Contain(p => p.Name == "Latitude" && p.PropertyType == typeof(decimal?));
        properties.Should().Contain(p => p.Name == "Longitude" && p.PropertyType == typeof(decimal?));
        properties.Should().Contain(p => p.Name == "Altitude" && p.PropertyType == typeof(int?));
    }

    [Fact]
    public void Circuit_HasRacesCollection()
    {
        var circuit = new Circuit();

        circuit.Races.Should().NotBeNull();
    }
}

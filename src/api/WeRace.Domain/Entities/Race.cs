namespace WeRace.Domain.Entities;

public class Race
{
    public int Id { get; set; }
    public int SeasonId { get; set; }
    public int Round { get; set; }
    public string Name { get; set; } = string.Empty;
    public int CircuitId { get; set; }
    public DateOnly Date { get; set; }
    public TimeOnly? Time { get; set; }
    public DateOnly? Fp1Date { get; set; }
    public TimeOnly? Fp1Time { get; set; }
    public DateOnly? Fp2Date { get; set; }
    public TimeOnly? Fp2Time { get; set; }
    public DateOnly? Fp3Date { get; set; }
    public TimeOnly? Fp3Time { get; set; }
    public DateOnly? QualiDate { get; set; }
    public TimeOnly? QualiTime { get; set; }
    public DateOnly? SprintDate { get; set; }
    public TimeOnly? SprintTime { get; set; }
    public string? WikipediaUrl { get; set; }

    public Season Season { get; set; } = null!;
    public Circuit Circuit { get; set; } = null!;

    public ICollection<Result> Results { get; set; } = [];
    public ICollection<Qualifying> Qualifyings { get; set; } = [];
    public ICollection<SprintResult> SprintResults { get; set; } = [];
    public ICollection<PitStop> PitStops { get; set; } = [];
    public ICollection<LapTime> LapTimes { get; set; } = [];
    public ICollection<DriverStanding> DriverStandings { get; set; } = [];
    public ICollection<ConstructorStanding> ConstructorStandings { get; set; } = [];
    public ICollection<ConstructorResult> ConstructorResults { get; set; } = [];
}

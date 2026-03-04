namespace WeRace.Domain.Entities;

public class Driver
{
    public int Id { get; set; }
    public string DriverRef { get; set; } = string.Empty;
    public int? Number { get; set; }
    public string? Code { get; set; }
    public string Forename { get; set; } = string.Empty;
    public string Surname { get; set; } = string.Empty;
    public DateOnly? DateOfBirth { get; set; }
    public string? Nationality { get; set; }
    public string? WikipediaUrl { get; set; }

    public ICollection<Result> Results { get; set; } = [];
    public ICollection<Qualifying> Qualifyings { get; set; } = [];
    public ICollection<SprintResult> SprintResults { get; set; } = [];
    public ICollection<PitStop> PitStops { get; set; } = [];
    public ICollection<LapTime> LapTimes { get; set; } = [];
    public ICollection<DriverStanding> DriverStandings { get; set; } = [];
}

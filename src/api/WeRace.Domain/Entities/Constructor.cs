namespace WeRace.Domain.Entities;

public class Constructor
{
    public int Id { get; set; }
    public string ConstructorRef { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Nationality { get; set; }
    public string? WikipediaUrl { get; set; }

    public ICollection<Result> Results { get; set; } = [];
    public ICollection<Qualifying> Qualifyings { get; set; } = [];
    public ICollection<SprintResult> SprintResults { get; set; } = [];
    public ICollection<ConstructorStanding> ConstructorStandings { get; set; } = [];
    public ICollection<ConstructorResult> ConstructorResults { get; set; } = [];
}

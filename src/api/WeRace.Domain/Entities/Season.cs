namespace WeRace.Domain.Entities;

public class Season
{
    public int Id { get; set; }
    public int Year { get; set; }
    public string? WikipediaUrl { get; set; }

    public ICollection<Race> Races { get; set; } = [];
}

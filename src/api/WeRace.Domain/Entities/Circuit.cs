namespace WeRace.Domain.Entities;

public class Circuit
{
    public int Id { get; set; }
    public string CircuitRef { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Location { get; set; }
    public string? Country { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public int? Altitude { get; set; }
    public string? WikipediaUrl { get; set; }

    public ICollection<Race> Races { get; set; } = [];
}

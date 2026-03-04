namespace WeRace.Domain.Entities;

public class Status
{
    public int Id { get; set; }
    public string StatusText { get; set; } = string.Empty;

    public ICollection<Result> Results { get; set; } = [];
    public ICollection<SprintResult> SprintResults { get; set; } = [];
}

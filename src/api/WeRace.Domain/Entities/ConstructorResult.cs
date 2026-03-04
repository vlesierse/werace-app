namespace WeRace.Domain.Entities;

public class ConstructorResult
{
    public int Id { get; set; }
    public int RaceId { get; set; }
    public int ConstructorId { get; set; }
    public decimal? Points { get; set; }
    public string? Status { get; set; }

    public Race Race { get; set; } = null!;
    public Constructor Constructor { get; set; } = null!;
}

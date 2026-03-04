namespace WeRace.Domain.Entities;

public class ConstructorStanding
{
    public int Id { get; set; }
    public int RaceId { get; set; }
    public int ConstructorId { get; set; }
    public decimal Points { get; set; }
    public int? Position { get; set; }
    public string? PositionText { get; set; }
    public int Wins { get; set; }

    public Race Race { get; set; } = null!;
    public Constructor Constructor { get; set; } = null!;
}

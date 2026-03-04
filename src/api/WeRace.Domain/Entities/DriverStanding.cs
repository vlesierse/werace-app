namespace WeRace.Domain.Entities;

public class DriverStanding
{
    public int Id { get; set; }
    public int RaceId { get; set; }
    public int DriverId { get; set; }
    public decimal Points { get; set; }
    public int? Position { get; set; }
    public string? PositionText { get; set; }
    public int Wins { get; set; }

    public Race Race { get; set; } = null!;
    public Driver Driver { get; set; } = null!;
}

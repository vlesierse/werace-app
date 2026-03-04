namespace WeRace.Domain.Entities;

public class SprintResult
{
    public int Id { get; set; }
    public int RaceId { get; set; }
    public int DriverId { get; set; }
    public int ConstructorId { get; set; }
    public int? Number { get; set; }
    public int Grid { get; set; }
    public int? Position { get; set; }
    public string PositionText { get; set; } = string.Empty;
    public int PositionOrder { get; set; }
    public decimal Points { get; set; }
    public int Laps { get; set; }
    public string? Time { get; set; }
    public int? Milliseconds { get; set; }
    public int? FastestLap { get; set; }
    public string? FastestLapTime { get; set; }
    public int StatusId { get; set; }

    public Race Race { get; set; } = null!;
    public Driver Driver { get; set; } = null!;
    public Constructor Constructor { get; set; } = null!;
    public Status Status { get; set; } = null!;
}

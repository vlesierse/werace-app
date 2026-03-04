namespace WeRace.Domain.Entities;

public class LapTime
{
    public int RaceId { get; set; }
    public int DriverId { get; set; }
    public int Lap { get; set; }
    public int? Position { get; set; }
    public string? Time { get; set; }
    public int? Milliseconds { get; set; }

    public Race Race { get; set; } = null!;
    public Driver Driver { get; set; } = null!;
}

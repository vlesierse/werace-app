namespace WeRace.Domain.Entities;

public class PitStop
{
    public int RaceId { get; set; }
    public int DriverId { get; set; }
    public int Stop { get; set; }
    public int Lap { get; set; }
    public TimeOnly? Time { get; set; }
    public string? Duration { get; set; }
    public int? Milliseconds { get; set; }

    public Race Race { get; set; } = null!;
    public Driver Driver { get; set; } = null!;
}

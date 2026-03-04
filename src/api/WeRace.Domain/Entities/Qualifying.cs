namespace WeRace.Domain.Entities;

public class Qualifying
{
    public int Id { get; set; }
    public int RaceId { get; set; }
    public int DriverId { get; set; }
    public int ConstructorId { get; set; }
    public int Number { get; set; }
    public int Position { get; set; }
    public string? Q1 { get; set; }
    public string? Q2 { get; set; }
    public string? Q3 { get; set; }

    public Race Race { get; set; } = null!;
    public Driver Driver { get; set; } = null!;
    public Constructor Constructor { get; set; } = null!;
}

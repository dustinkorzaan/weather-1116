namespace Core.Data.Domain;

public class UserCity
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public double Latitude { get; set; }

    public double Longitude { get; set; }

    public string LocationName { get; set; } = string.Empty;

    public User User { get; set; } = null!;
}

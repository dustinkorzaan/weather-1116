namespace Core.Data.Domain;

public class User
{
    public Guid Id { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public ICollection<UserPin> UserPins { get; set; } = new List<UserPin>();
}

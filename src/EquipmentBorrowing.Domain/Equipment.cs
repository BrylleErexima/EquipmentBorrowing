namespace EquipmentBorrowing.Domain;
public class Equipment
{
    public int Id { get; private set; }
    public string Identifier { get; private set; } = "";
    public string Name { get; private set; } = "";
    public string? Description { get; private set; }
    public bool IsAvailable { get; private set; } = true;
    public int Version { get; private set; }
    private Equipment() { }
    public Equipment(string identifier, string name, string? description = null) => Update(identifier, name, description);
    public void Update(string identifier, string name, string? description)
    {
        Identifier = identifier.Trim(); Name = name.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(); Version++;
    }
    public void SetAvailability(bool available) { IsAvailable = available; Version++; }
    public override string ToString() => $"{Identifier} · {Name} · {(IsAvailable ? "Available" : "On loan")} · {Description}";
}

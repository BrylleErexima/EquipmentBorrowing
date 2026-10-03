namespace EquipmentBorrowing.Domain;

public class Equipment
{
    public int Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public bool IsAvailable { get; set; }

    // Required by EF Core
    private Equipment() { }

    public Equipment(int id, string name, bool isAvailable = true)
    {
        Id = id;
        Name = name;
        IsAvailable = isAvailable;
    }
}
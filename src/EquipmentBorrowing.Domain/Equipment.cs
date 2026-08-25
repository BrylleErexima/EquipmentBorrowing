namespace EquipmentBorrowing.Domain;

public class Equipment
{
    public int Id { get; }
    public string Name { get; }
    public bool IsAvailable { get; set; }

    public Equipment(int id, string name, bool isAvailable = true)
    {
        Id = id;
        Name = name;
        IsAvailable = isAvailable;
    }
}

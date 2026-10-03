namespace EquipmentBorrowing.Domain;

public class Student
{
    public int Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public bool CanBorrow { get; set; }
    public int MaxActiveBorrowings { get; private set; }

    // Required by EF Core
    private Student() { }

    public Student(int id, string name, bool canBorrow = true, int maxActiveBorrowings = 2)
    {
        Id = id;
        Name = name;
        CanBorrow = canBorrow;
        MaxActiveBorrowings = maxActiveBorrowings;
    }
}
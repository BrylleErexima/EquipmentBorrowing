namespace EquipmentBorrowing.Domain;

public class Student
{
    public int Id { get; }
    public string Name { get; }
    public bool CanBorrow { get; set; }
    public int MaxActiveBorrowings { get; }

    public Student(int id, string name, bool canBorrow = true, int maxActiveBorrowings = 2)
    {
        Id = id;
        Name = name;
        CanBorrow = canBorrow;
        MaxActiveBorrowings = maxActiveBorrowings;
    }
}

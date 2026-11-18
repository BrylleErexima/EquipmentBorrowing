namespace EquipmentBorrowing.Domain;
public class Student
{
    public int Id { get; private set; }
    public string Name { get; private set; } = "";
    public string StudentNumber { get; private set; } = "";
    public string? Contact { get; private set; }
    public bool CanBorrow { get; private set; } = true;
    public int Version { get; private set; }
    private Student() { }
    public Student(string number, string name, string? contact = null) => Update(number, name, contact);
    public void Update(string number, string name, string? contact)
    {
        StudentNumber = number.Trim(); Name = name.Trim();
        Contact = string.IsNullOrWhiteSpace(contact) ? null : contact.Trim(); Version++;
    }
    public void SetEligibility(bool allowed) { CanBorrow = allowed; Version++; }
    public override string ToString() => $"{StudentNumber} · {Name} ({(CanBorrow ? "Eligible" : "Restricted")})";
}

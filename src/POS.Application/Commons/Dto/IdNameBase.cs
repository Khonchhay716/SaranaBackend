namespace POS.Application.Common.Dto;

public class IdBase
{
    public int Id { get; set; }
    public string Name { get; set; }
    public IdBase() { }
    public IdBase(int id, string name)
    {
        Id = id;
        Name = name;
    }
}
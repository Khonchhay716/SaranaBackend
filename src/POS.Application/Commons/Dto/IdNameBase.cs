namespace Insura.Application.Common;


public class IdBase
{
    public int Id { get; set; }
    public IdBase() { }
    public IdBase(int id)
    {
        Id = id;
    }
}

public class IdNameBase : IdBase
{
    public string Name { get; set; }

    public IdNameBase() { }

    public IdNameBase(int id, string name) : base(id)
    {
        Name = name;
    }
}

public class IdCodeNameBase : IdNameBase
{
    public string Code { get; set; }
    public IdCodeNameBase() { }
    public IdCodeNameBase(int id, string name, string code) : base(id, name)
    {
        Code = code;
    }
}

public class CodeNameBase
{
    public string Code { get; set; }
    public string Name { get; set; }
    public CodeNameBase() { }
    public CodeNameBase(string name, string code)
    {
        Name = name;
        Code = code;
    }
}
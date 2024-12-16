namespace DocDB.Contracts;

public class DdbForeignKeyReference : NamedDdbObject
{
    public string TableName { get; set; } = null!;
}

namespace DocDB.Contracts;

public interface INamedDdbRef : IDdbRef
{
    string Name { get; }
}

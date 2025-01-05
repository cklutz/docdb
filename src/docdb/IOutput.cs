namespace DocDB;

public interface IOutput
{
    bool IsDebugEnabled { get; }
    void Debug(string message);
    void Error(string message);
    void Message(string message);
    void Warning(string message);
}

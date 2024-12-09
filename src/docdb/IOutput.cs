namespace DocDB;

public interface IOutput
{
    void Debug(string message);
    void Error(string message);
    void Message(string message);
    void Warning(string message);
}
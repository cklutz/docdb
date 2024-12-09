using System;

namespace DocDB;

public class Output : IOutput
{
    public void Error(string message) => Console.Error.WriteLine($"docdb: error: {message}");
    public void Warning(string message) => Console.Error.WriteLine($"docdb: warning: {message}");
    public void Message(string message) => Console.WriteLine($"docdb: {message}");
    public void Debug(string message) => Console.WriteLine($"docdb: {message}");
}
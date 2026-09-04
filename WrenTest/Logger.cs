using System.Diagnostics;
using Wren;

namespace WrenTest;

[WrenClass]
public class Logger
{
    [WrenMethod]
    public static void Log(string message)
    {
        Console.Write("[WREN]: ");
        Console.WriteLine(message);
    }
    
    [WrenMethod]
    public static void Error(string message)
    {
        Console.Error.Write("[WREN]: ");
        Console.Error.WriteLine(message);
    }
}
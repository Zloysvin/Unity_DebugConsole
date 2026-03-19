using Unity.VisualScripting;
using UnityEngine;

public class DefaultCommands
{
    [ConsoleCommand("help", "Shows the lost of all avaliable commands", "Help")]
    private static void ConsoleHelp()
    {
        foreach(var c in DebugConsole.Commands)
        {
            DebugConsole.AddLog($"{c.CommandFormat} - {c.CommandDescription}");
        }
    }

    [ConsoleCommand("debugcount", "Counts number to N. Params: (int) N", "DebugCount")]
    private static void ConsoleDebugCount(int N)
    {
        for (int i = 0; i < N; i++)
        {
            DebugConsole.AddLog((i + 1).ToString());
        }
    }
}

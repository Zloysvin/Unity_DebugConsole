using System;
using UnityEngine;

public class DebugCommandBase
{
    public string CommandID { private set; get; }
    public string CommandDescription { private set; get; }
    public string CommandFormat { private set; get; }

    public DebugCommandBase(string id, string description, string format)
    {
        CommandID = id;
        CommandDescription = description;
        CommandFormat = format;
    }
}

public class DebugCommand : DebugCommandBase
{
    private Action command;

    public DebugCommand(string id, string description, string format, Action command) : base(id, description, format)
    {
        this.command = command;
    }

    public void Invoke()
    {
        command.Invoke();
    }
}

public class DebugCommandMulti : DebugCommandBase
{
    private Action<string[]> _command;

    public DebugCommandMulti(string id, string description, string format, Action<string[]> command)
        : base(id, description, format)
    {
        _command = command;
    }

    public void Invoke(string[] args) => _command.Invoke(args);
}
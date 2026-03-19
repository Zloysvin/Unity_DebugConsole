using System;

/// <summary>
/// Custom Attribute to mark a method as a console command
/// </summary>

[AttributeUsage(AttributeTargets.Method)]
public class ConsoleCommandAttribute : Attribute
{
    public string CommandID { get; }
    public string Description { get; }
    public string Format { get; }

    public ConsoleCommandAttribute(string id, string description, string format)
    {
        CommandID = id;
        Description = description;
        Format = format;
    }
}

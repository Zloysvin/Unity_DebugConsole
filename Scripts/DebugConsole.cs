using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Windows;

public static class DebugConsole
{
    private static readonly List<string> _log = new List<string>();
    public static IReadOnlyList<string> Log => _log; 

    public static List<DebugCommandBase> Commands { get; private set; } = new List<DebugCommandBase>();

    public static readonly List<string> CommandHistory = new List<string>();
    public static int HistoryIndex = -1;

    [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.AfterAssembliesLoaded)]
    public static void Initialize()
    {
        Commands = new List<DebugCommandBase>();

        var methods = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(x => x.GetTypes())
            .SelectMany(x => x.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
            .Where(m => m.GetCustomAttributes(typeof(ConsoleCommandAttribute), false).Length > 0);

        foreach (var method in methods)
        {
            var attr = (ConsoleCommandAttribute)method.GetCustomAttributes(typeof(ConsoleCommandAttribute), false)[0];
            var methodParams = method.GetParameters();

            Commands.Add(new DebugCommandMulti(attr.CommandID, attr.Description, attr.Format, (string[] passedArgs) =>
            {
                object[] finalParams = new object[methodParams.Length];

                for (int i = 0; i < methodParams.Length; i++)
                {
                    if (i < passedArgs.Length)
                    {
                        finalParams[i] = Convert.ChangeType(passedArgs[i], methodParams[i].ParameterType);
                    }
                    else if (methodParams[i].HasDefaultValue)
                    {
                        finalParams[i] = methodParams[i].DefaultValue;
                    }
                    else
                    {
                        throw new Exception($"Missing required parameter: {methodParams[i].Name}");
                    }
                }

                method.Invoke(null, finalParams);
            }));
        }
    }

    public static void AddLog(string message)
    {
        _log.Add(message);
        if (_log.Count > 100) _log.RemoveAt(0);
    }

    public static void AddErrorLog(string message)
    {
        AddLog($"<color=red>{message}</color>");
    }

    public static void Execute(string input)
    {
        _log.Add($"> {input}");

        if (CommandHistory.Count == 0 || CommandHistory[CommandHistory.Count - 1] != input)
        {
            CommandHistory.Add(input);
        }
        HistoryIndex = -1;

        string[] parts = input.Split(' ');
        string commandId = parts[0].ToLower();
        string[] args = parts.Skip(1).ToArray();

        var command = Commands.FirstOrDefault(c => c.CommandID.ToLower() == commandId);

        if (command == null)
        {
            AddErrorLog($"Command '{commandId}' not found.");
            return;
        }

        try
        {
            if (command is DebugCommandMulti multiCmd)
            {
                multiCmd.Invoke(args);
            }
            else if (command is DebugCommand simpleCmd)
            {
                simpleCmd.Invoke();
            }
        }
        catch (TargetParameterCountException)
        {
            AddErrorLog($"Error: Command '{commandId}' expected a different number of parameters.");
        }
        catch (FormatException)
        {
            AddErrorLog($"Error: Parameter type mismatch. Please check your input.");
        }
        catch (Exception ex)
        {
            // InnerException usually contains the actual error from your game logic method
            string errorMsg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
            AddErrorLog($"Error executing '{commandId}': {errorMsg}");
            UnityEngine.Debug.LogException(ex); // Still log to Unity console for the developer
        }
    }

    [ConsoleCommand("clearconsole", "Clears the console", "ClearConsole")]
    private static void ClearConsole()
    {
        _log.Clear();
        AddLog("Console cleared!");
    }
}

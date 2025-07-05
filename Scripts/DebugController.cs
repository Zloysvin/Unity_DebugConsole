using System;
using System.Collections.Generic;
using UnityEngine;

public class DebugController : MonoBehaviour
{
    public bool DebugEnabled { get; private set; }
    public GUISkin Skin;

    public static DebugController Instance;

    private static readonly float _inputHeight = 40f;
    private float _relativeInputHeight = Screen.height / 1080f * _inputHeight;
    private string _input;
    private bool _shouldFocus;
    private readonly List<string> _log = new List<string>();

    public List<object> CommandList;

    public static DebugCommand ClearConsole;
    public static DebugCommand HelpCommand;
    public static DebugCommand<int> DebugCount;

    void Awake()
    {
        if(Instance == null)
            Instance = this;

        // Commands Declaration
        ClearConsole = new DebugCommand("clearconsole", "Clears all console lines", "ClearConsole", ConsoleClear);
        HelpCommand = new DebugCommand("help", "Shows information about all avaliable commands", "Help", () =>
        {
            foreach (var debugCommand in CommandList)
            {
                var commandBase = (DebugCommandBase)debugCommand;
                AddLog($"{commandBase.CommandFormat} - {commandBase.CommandDescription}");
            }
        });

        DebugCount = new DebugCommand<int>("debugcount", "Counts number to N. Params: (int) N", "DebugCount", (x) =>
        {
            for (int i = 0; i < x; i++)
            {
                AddErrorLog((i + 1).ToString());
            }
        });

        // Adds Commands to the list
        CommandList = new List<object>
        {
            ClearConsole,
            HelpCommand,
            DebugCount,
        };
    }

    void Start()
    {
        DebugEnabled = false;
    }
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.BackQuote))
        {
            ToggleConsole();
        }
    }

    void OnGUI()
    {
        if(!DebugEnabled)
            return;

        Event e = Event.current;
        if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
        {
            ToggleConsole();
            return;
        }

        if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Return)
        {
            if (!string.IsNullOrWhiteSpace(_input))
            {
                HandleInput();
                _input = "";
            }
            _shouldFocus = true;
        }

        float y = Screen.height - Screen.height / 1080f * _inputHeight;

        GUI.Box(new Rect(0f, 0f, Screen.width, Screen.height - _relativeInputHeight * 1.2f), "");

        int logCounter = 1;
        for (int i = _log.Count - 1; i >= 0; i--)
        {
            var labelRect = new Rect(10f,
                Screen.height - _relativeInputHeight * 1.35f - logCounter * Skin.label.fontSize, Screen.width - 20f, Skin.label.fontSize*1.3f);

            GUI.Label(labelRect, _log[i], Skin.label);
            logCounter++;
        }

        GUI.Box(new Rect(0, y, Screen.width, _relativeInputHeight), "");
        GUI.backgroundColor = new Color(0f, 0f, 0f, 0f);
        GUI.SetNextControlName("ConsoleInput");
        _input = GUI.TextField(new Rect(10f, y, Screen.width - 20f, _relativeInputHeight), _input, Skin.textField);
        GUI.backgroundColor = new Color(0f, 0f, 0f, 1f);

        if (_shouldFocus)
        {
            GUI.FocusControl("ConsoleInput");
            _shouldFocus = false;
        }
    }

    private void ToggleConsole()
    {
        DebugEnabled = !DebugEnabled;
        if (DebugEnabled)
        {
            _shouldFocus = true;
        }
    }

    public void AddLog(string line)
    {
        _log.Add(line);
        if (Screen.height - _relativeInputHeight * 1.35f - (_log.Count) * Skin.label.fontSize < 0)
        {
            _log.RemoveAt(0);
        }
    }

    public void AddErrorLog(string line)
    {
        AddLog("<color=red>" + line + "</color>");
    }

    private void HandleInput()
    {
        _log.Add($"> {_input}");

        string[] splittedInput = _input.Split(" ");

        for (int i = 0; i < CommandList.Count; i++)
        {
            var commandBase = CommandList[i] as DebugCommandBase;
            if (splittedInput[0].ToLower().Contains(commandBase.CommandID))
            {
                if (CommandList[i] is DebugCommand)
                {
                    (CommandList[i] as DebugCommand).Invoke();
                }
                else if (CommandList[i] is DebugCommand<int>)
                {
                    if(splittedInput.Length > 1)
                    {
                        try
                        {
                            Convert.ToInt32(splittedInput[1]);
                        }
                        catch (Exception e)
                        {
                            AddErrorLog($"Parameter {splittedInput[1]} was not of type Int");
                            return;
                        }

                        (CommandList[i] as DebugCommand<int>).Invoke(Convert.ToInt32(splittedInput[1]));
                    }
                    else
                    {
                        AddErrorLog($"Command {commandBase.CommandFormat} takes 1 argument, but was given 0");
                    }
                }
                else if (CommandList[i] is DebugCommand<string>)
                {
                    if (splittedInput.Length > 1)
                    {
                        (CommandList[i] as DebugCommand<string>).Invoke(splittedInput[1]);
                    }
                    else
                    {
                        AddErrorLog($"Command {commandBase.CommandFormat} takes 1 argument, but was given 0");
                    }
                }
                return;
            }
        }

        AddErrorLog($"Command {_input} wasn't found in the command directory");
    }

    private void ConsoleClear()
    {
        _log.Clear();
        AddLog("Console Cleared");
    }
}

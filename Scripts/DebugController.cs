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

    private List<DebugCommandBase> _filteredCommands = new List<DebugCommandBase>();
    private int _selectedSuggestionIndex = 0;
    private bool _showSuggestions = false;

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
        if (!DebugEnabled) return;

        Event e = Event.current;
        float y = Screen.height - _relativeInputHeight;

        // Filter the command list with those that conatin input
        UpdateFilteredCommands();

        // Handle Key Presses
        HandleInputKeys(e);

        // Draw "Layers" in Order
        DrawLogs();
        DrawSuggestionBox(y);
        DrawInput(y);

        bool isFocused = GUI.GetNameOfFocusedControl() == "ConsoleInput";

        // Handle Unity's GUI TextBox issue of highlighting text every time when focused
        if (_shouldFocus || !isFocused)
        {
            GUI.FocusControl("ConsoleInput");

            StopAllCoroutines();
            StartCoroutine(ClearHighlightNextFrame());

            _shouldFocus = false;
        }
    }

    private System.Collections.IEnumerator ClearHighlightNextFrame()
    {
        yield return new WaitForEndOfFrame();

        TextEditor te = (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);
        if (te != null)
        {
            te.MoveTextEnd();
            te.SelectNone();
        }
    }

    private void HandleInputKeys(Event e)
    {
        if (e.type == EventType.KeyDown)
        {
            if (e.keyCode == KeyCode.Return)
            {
                if (!string.IsNullOrWhiteSpace(_input))
                {
                    HandleInputText();
                    _input = "";
                    _filteredCommands.Clear();
                    _showSuggestions = false;
                }
                _shouldFocus = true;
                e.Use();
            }
            else if (e.keyCode == KeyCode.Escape)
            {
                ToggleConsole();
                return;
            }
            else if (_showSuggestions)
            {
                if (e.keyCode == KeyCode.DownArrow)
                {
                    _selectedSuggestionIndex = (_selectedSuggestionIndex + 1) % _filteredCommands.Count;
                    e.Use();
                }
                else if (e.keyCode == KeyCode.UpArrow)
                {
                    _selectedSuggestionIndex = (_selectedSuggestionIndex - 1 + _filteredCommands.Count) % _filteredCommands.Count;
                    e.Use();
                }
                else if (e.keyCode == KeyCode.Tab)
                {
                    _input = _filteredCommands[_selectedSuggestionIndex].CommandID;
                    _shouldFocus = true;
                    e.Use();
                }
            }
        }
    }

    private void HandleInputText()
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
                    if (splittedInput.Length > 1)
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

    private void DrawLogs()
    {
        GUI.Box(new Rect(0f, 0f, Screen.width, Screen.height - _relativeInputHeight * 1.2f), "");

        int logCounter = 1;
        for (int i = _log.Count - 1; i >= 0; i--)
        {
            var labelRect = new Rect(10f,
                Screen.height - _relativeInputHeight * 1.35f - logCounter * Skin.label.fontSize,
                Screen.width - 20f, Skin.label.fontSize * 1.3f);

            GUI.Label(labelRect, _log[i], Skin.label);
            logCounter++;
        }
    }

    private void DrawSuggestionBox(float inputY)
    {
        if (!_showSuggestions) return;

        float suggestionHeight = Skin.label.fontSize * 1.5f;
        float boxHeight = _filteredCommands.Count * suggestionHeight;
        Rect suggestionRect = new Rect(10f, inputY - boxHeight - 5f, Screen.width - 20f, boxHeight);

        GUI.Box(suggestionRect, "");

        for (int i = 0; i < _filteredCommands.Count; i++)
        {
            Rect itemRect = new Rect(15f, suggestionRect.y + (i * suggestionHeight), suggestionRect.width - 10f, suggestionHeight);

            if (i == _selectedSuggestionIndex)
            {
                GUI.color = Color.yellow;
                GUI.Box(itemRect, ">");
                GUI.color = Color.white;
            }

            string highlightedText = HighlightMatch(_filteredCommands[i].CommandID, _input);
            GUI.Label(itemRect, highlightedText, Skin.label);
        }
    }

    private void DrawInput(float y)
    {
        GUI.Box(new Rect(0, y, Screen.width, _relativeInputHeight), "");
        GUI.backgroundColor = new Color(0f, 0f, 0f, 0f);
        GUI.SetNextControlName("ConsoleInput");

        string newStr = GUI.TextField(new Rect(10f, y, Screen.width - 20f, _relativeInputHeight), _input, Skin.textField);

        if (newStr != _input)
        {
            _input = newStr;
            _selectedSuggestionIndex = 0;
        }

        GUI.backgroundColor = Color.white;
    }

    private void UpdateFilteredCommands()
    {
        if (string.IsNullOrWhiteSpace(_input))
        {
            _showSuggestions = false;
            return;
        }

        _filteredCommands.Clear();
        foreach (var cmd in CommandList)
        {
            var cb = (DebugCommandBase)cmd;
            if (cb.CommandID.ToLower().Contains(_input.ToLower()))
            {
                _filteredCommands.Add(cb);
            }
        }

        _showSuggestions = _filteredCommands.Count > 0;
        _selectedSuggestionIndex = Mathf.Clamp(_selectedSuggestionIndex, 0, Mathf.Max(0, _filteredCommands.Count - 1));
    }

    private string HighlightMatch(string fullText, string input)
    {
        if (string.IsNullOrEmpty(input)) return fullText;

        int index = fullText.ToLower().IndexOf(input.ToLower());
        if (index == -1) return fullText;

        string match = fullText.Substring(index, input.Length);
        return fullText.Replace(match, $"<color=#FFD700><b>{match}</b></color>");
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

    private void ConsoleClear()
    {
        _log.Clear();
        AddLog("Console Cleared");
    }
}

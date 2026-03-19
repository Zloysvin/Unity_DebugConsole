using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controller class for the Console. Responsible for all user interactions with a console
/// </summary>
public class DebugController : MonoBehaviour
{
/// <summary>
/// Marks if debug mode is enabled in the scene.
/// </summary>
    public bool DebugEnabled { get; private set; }
    public GUISkin Skin;

    private static readonly float inputHeight = 40f;
    private float _relativeInputHeight = Screen.height / 1080f * inputHeight;
    private string input;
    private bool _shouldFocus;
    private List<string> _log = new List<string>();

    private List<DebugCommandBase> _filteredCommands = new List<DebugCommandBase>();
    private int _selectedSuggestionIndex = 0;
    private bool _showSuggestions = false;

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

        _log = (List<string>)DebugConsole.Log;

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
/// <summary>
/// Courutine which sole purpose is to clear the selection of an GUI input field on the end of the frame. It's done this way to simulate 
/// similar solution for Canvas TextField, where people create custom class and remove selection in the LateUpdate().
/// </summary>
/// <returns></returns>
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
/// <summary>
/// Method that handles all keyboard inputs
/// </summary>
/// <param name="e"></param>
    private void HandleInputKeys(Event e)
    {
        var CommandHistory = DebugConsole.CommandHistory;
        var _historyIndex = DebugConsole.HistoryIndex;

        if (e.type != EventType.KeyDown) return;

        if (e.keyCode == KeyCode.Return)
        {
            if (!string.IsNullOrWhiteSpace(input))
            {
                DebugConsole.Execute(input);
                input = "";
                _filteredCommands.Clear();
                _showSuggestions = false;
            }
            _shouldFocus = true;
            e.Use();
        }
        else if (e.keyCode == KeyCode.Escape)
        {
            ToggleConsole();
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
                input = _filteredCommands[_selectedSuggestionIndex].CommandID;
                _shouldFocus = true;
                e.Use();
            }
        }
        else if (!_showSuggestions && CommandHistory.Count > 0)
        {
            if (e.keyCode == KeyCode.UpArrow)
            {
                if (_historyIndex == -1) _historyIndex = CommandHistory.Count - 1;
                else if (_historyIndex > 0) _historyIndex--;

                input = CommandHistory[_historyIndex];
                _shouldFocus = true;
                e.Use();
            }
            else if (e.keyCode == KeyCode.DownArrow)
            {
                if (_historyIndex != -1)
                {
                    if (_historyIndex < CommandHistory.Count - 1)
                    {
                        _historyIndex++;
                        input = CommandHistory[_historyIndex];
                    }
                    else
                    {
                        _historyIndex = -1;
                        input = "";
                    }
                    _shouldFocus = true;
                    e.Use();
                }
            }
        }
    }
/// <summary>
/// Method that displays the console output contents. 
/// </summary>
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
/// <summary>
/// Method that displays the suggestion box.
/// </summary>
/// <param name="inputY"></param>
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

            string highlightedText = HighlightMatch(_filteredCommands[i].CommandID, input);
            GUI.Label(itemRect, highlightedText, Skin.label);
        }
    }
/// <summary>
/// Method that draws the console input field.
/// </summary>
/// <param name="y"></param>
    private void DrawInput(float y)
    {
        GUI.Box(new Rect(0, y, Screen.width, _relativeInputHeight), "");
        GUI.backgroundColor = new Color(0f, 0f, 0f, 0f);
        GUI.SetNextControlName("ConsoleInput");

        string newStr = GUI.TextField(new Rect(10f, y, Screen.width - 20f, _relativeInputHeight), input, Skin.textField);

        if (newStr != input)
        {
            input = newStr;
            _selectedSuggestionIndex = 0;
        }

        GUI.backgroundColor = Color.white;
    }
/// <summary>
/// Method that updates the list of suggested commands based on the current input.
/// </summary>
    private void UpdateFilteredCommands()
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            _showSuggestions = false;
            return;
        }

        _filteredCommands.Clear();
        foreach (var cmd in DebugConsole.Commands)
        {
            var cb = (DebugCommandBase)cmd;
            if (cb.CommandID.ToLower().Contains(input.ToLower()) && 
                !cb.CommandID.ToLower().Equals(input.ToLower()))
            {
                _filteredCommands.Add(cb);
            }
        }

        _showSuggestions = _filteredCommands.Count > 0;
        _selectedSuggestionIndex = Mathf.Clamp(_selectedSuggestionIndex, 0, Mathf.Max(0, _filteredCommands.Count - 1));
    }
/// <summary>
/// Helper method that returns a string with highlighted matching letters.
/// </summary>
/// <param name="fullText"></param>
/// <param name="input"></param>
/// <returns></returns>
    private string HighlightMatch(string fullText, string input)
    {
        if (string.IsNullOrEmpty(input)) return fullText;

        int index = fullText.ToLower().IndexOf(input.ToLower());
        if (index == -1) return fullText;

        string match = fullText.Substring(index, input.Length);
        return fullText.Replace(match, $"<color=#FFD700><b>{match}</b></color>");
    }
/// <summary>
/// Method to show/hide the console.
/// </summary>
    private void ToggleConsole()
    {
        DebugEnabled = !DebugEnabled;
        if (DebugEnabled)
        {
            _shouldFocus = true;
        }
    }
}

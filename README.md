# Debug Console

 A simple implementation of a debug console for Unity. Main goal for this console was to have a simple debug tool where you can execute some specific behaviour during runtime for testing purposes, like loading some levels directly, or changing player currency.

## Usage

Everything that you have to do is just add an attribute ```[ConsoleCommand("id", "description", "formated name")]``` to any of your methods. The only limitation is that method should be static. You can have any number of attributes. Code will identify a command, it's parameterers on initialize and add it to the pool. In the file "DefaultCommands.cs" I have added some basic commands to show the functionality and how to implement your custom own commands. 

Console supports command suggestions when you type symbols. Use your Up/Down arrow keys to navigate suggestions and press Tab to choose. It also supports command history - just use Up/Down arrow keys when there's no suggestions displayed. Console commands can have unlimited number of parameters, and you have to enter them with a space in between in the cosnole.

DebugConsole class has static methods for your own outputs into the console - ```AddLog(string message)``` and `AddErrorLog(string message)`, use them to display information or inform user about the error.

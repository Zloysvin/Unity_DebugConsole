# Debug Console

 A simple implementation of a debug console for Unity. Main goal for this console was to have a simple debug tool where you can execute some specific behaviour during runtime for testing purposes, like loading some levels directly, or changing player currency.

 This project consists of 2 files:
 
 ## DebugCommand.cs
 
 It is a base class for a debug command. It has 3 fields: CommandID, CommandDescription and CommandFormat. Description and Format are responsible for an information of the command, and has no influence on the logic. CommandId, on the other hand, is what DebugController will look for when you will type the command in the console. It should be in lowercase.

 ## DebugController.cs
 
 it's a class responsible for handling all the input and outup to the console. Console is made using OnGUI. Console will be automatically adapted to your screen resolution. HOWEVER, to make console work, you would need to create GUISkin (Right click in Project -> Create -> GUI Skin), and set size and font for label and textfield. You can leave font field empty to use default Unity font, but fields for size should be greater than zero. From my experience, size 28 is perfect. Controller will adapt to your screen size and sizes of label and textfield and will display only lines that will fit inside your screen.
 In this repository I provided a base GUI Skin that I am using, however, you are free to modify it however you want - Controller will handle all of the rest.

 To actually add command to the list, you need to create a new DebugCommand, and describe it's information in constructor, as well as, provide method or simply a lambda statement, that this command will execute. You also can use generics as parameters for your commands.
 DebugController has 3 example commands that utilize all 3 methods of declaring commands, see it for more info.

 DebugController is also a singleton with static instance. Main purpose for it - provide access to output information to the console not only in the controller, but outside. There are 2 methods for output - AddLog and AddErrorLog.

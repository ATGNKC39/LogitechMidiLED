# LogitechMidiLED_PublicDist

A simple C# program that's used to control the LED lighting on any Logitech Per Key RGB Capable Keyboard (e.g. G910, G810) based on Midi inputs.

#NOTE: 
If you encounter the following error while running the program:
>System.DllNotFoundException: 'Unable to load DLL 'LogitechLedEnginesWrapper ': The specified module could not be found.
This can be solved by copying the included LogitechLedEnginesWrapper.dll from the solution directory to the folder where your executable is located. (which is usually located under "\LogitechMidiLED\bin\Debug\")
if the folder or executable doesn't exist, you may have to build and run the solution first.

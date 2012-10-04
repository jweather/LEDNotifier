LEDNotifier
===========

AutoIt, C#, and Netduino code from my LED notification strip project

ShellHookWindow.au3 (AutoIt): uses RegisterShellHookWindow to watch for WM_FLASH messages,
relays them to TCP localhost:8003

Notifier (C#): listens to localhost:8003 and checks Outlook unread messages via
MS Office Interop class, sends messages to TCP 192.168.1.10:23 (Netduino)

NetduinoNotifier (C# for Netduino): pulses LEDs slowly, receives messages on TCP port 23 to cycle
or flash LEDs.
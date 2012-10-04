; Jeremy Weatherford
; no rights reserved
; adapted from ShellHookWindow.au3: http://www.autoitscript.com/forum/topic/56536-easy-shell-hooking-example/
; by "Siao"
#include <GuiConstants.au3>
#include <Misc.au3>
#include <WindowsConstants.au3>
#include <ListboxConstants.au3>
#include <Array.au3>

#NoTrayIcon
Opt("GUICloseOnESC", 0)
Opt("GUIOnEventMode", 1)
Opt("WinWaitDelay", 0)

Global Const $SC_MOVE = 0xF010
Global Const $SC_SIZE = 0xF000
Global Const $SC_CLOSE = 0xF060

;ShellHook notification codes:
Global Const $HSHELL_FLASH = 32774;

Global $bHook = 1
;GUI stuff:

Global $iGuiW = 400, $iGuiH = 50, $sTitle = "Shell Hook", $aBtnText[2] = ["START", "STOP"]
$hGui = GUICreate($sTitle, $iGuiW, $iGuiH, -1, 0, $WS_POPUP+$WS_BORDER+$WS_MINIMIZE, $WS_EX_TOPMOST)
GUISetOnEvent($GUI_EVENT_CLOSE, "SysEvents")
GUISetOnEvent($GUI_EVENT_PRIMARYDOWN, "SysEvents")
GUIRegisterMsg($WM_SYSCOMMAND, "On_WM_SYSCOMMAND")
$cBtnMini = GUICtrlCreateButton("v", $iGuiW-$iGuiH, 0, $iGuiH/2, $iGuiH/2)
GUICtrlSetOnEvent(-1, "CtrlEvents")
GUICtrlSetTip(-1, "Minimize")
$cBtnClose = GUICtrlCreateButton("X", $iGuiW-$iGuiH/2, 0, $iGuiH/2, $iGuiH/2)
GUICtrlSetOnEvent(-1, "CtrlEvents")
GUICtrlSetTip(-1, "Exit")
$cBtnHook = GUICtrlCreateButton("", $iGuiW-$iGuiH, $iGuiH/2, $iGuiH, $iGuiH/2)
GUICtrlSetData(-1, $aBtnText[$bHook])
GUICtrlSetTip(-1, "Hook/Unhook Shell")
GUICtrlSetOnEvent(-1, "CtrlEvents")
$cList = GUICtrlCreateList("", 0, 0, $iGuiW-$iGuiH-1, $iGuiH, $LBS_NOINTEGRALHEIGHT+$WS_VSCROLL)
GUICtrlSetOnEvent(-1, "CtrlEvents")

;Hook stuff:
GUIRegisterMsg(RegisterWindowMessage("SHELLHOOK"), "HShellWndProc")
ShellHookWindow($hGui, $bHook)

GUISetState()

TCPStartup()

while 1
   sleep(1000)
wend

Func SysEvents()
	Switch @GUI_CtrlId
		Case $GUI_EVENT_CLOSE
			Exit
		Case $GUI_EVENT_PRIMARYDOWN
			;CTRL + Left click to drag GUI
			If _IsPressed("11") Then
				DllCall("user32.dll", "int", "ReleaseCapture")
				DllCall("user32.dll", "int", "SendMessage", "hWnd", $hGui, "int", 0xA1, "int", 2, "int", 0)
			EndIf
	EndSwitch
EndFunc
Func CtrlEvents()
	Switch @GUI_CtrlId
		Case $cBtnMini
			GUISetState(@SW_MINIMIZE)
		Case $cBtnClose
			_SendMessage($hGui, $WM_SYSCOMMAND, $SC_CLOSE, 0)
		Case $cBtnHook
			$bHook = BitXOR($bHook, 1)
			ShellHookWindow($hGui, $bHook)
			GUICtrlSetData($cBtnHook, $aBtnText[$bHook])
	EndSwitch	
 EndFunc

Func sendData($msg)
   Local $sock
   $sock = TCPConnect("127.0.0.1", 8003)
   if $sock == -1 then return
   TCPSend($sock, $msg & @CRLF)
   TCPCloseSocket($sock)
endfunc
	  
Func HShellWndProc($hWnd, $Msg, $wParam, $lParam)
   Local $title, $i
	Switch $wParam
		Case $HSHELL_FLASH
		   $title = WinGetTitle($lParam)
			MsgPrint("Window flash " & $wParam & ": " & $lParam & " (" & $title & ")")
			sendData("flash " & $title)
	EndSwitch
EndFunc

;register/unregister ShellHook
Func ShellHookWindow($hWnd, $bFlag)
	Local $sFunc = 'DeregisterShellHookWindow'
	If $bFlag Then $sFunc = 'RegisterShellHookWindow'
	Local $aRet = DllCall('user32.dll', 'int', $sFunc, 'hwnd', $hWnd)
	MsgPrint($sFunc & ' = ' & $aRet[0])
	Return $aRet[0]
EndFunc
Func MsgPrint($sText)
	ConsoleWrite($sText & @CRLF)
	GUICtrlSendMsg($cList, $LB_SETCURSEL, GUICtrlSendMsg($cList, $LB_ADDSTRING, 0, $sText), 0)
EndFunc
;register window message
Func RegisterWindowMessage($sText)
	Local $aRet = DllCall('user32.dll', 'int', 'RegisterWindowMessage', 'str', $sText)
	Return $aRet[0]
EndFunc
Func On_WM_SYSCOMMAND($hWnd, $Msg, $wParam, $lParam)
    Switch BitAND($wParam, 0xFFF0)
        Case $SC_MOVE, $SC_SIZE
		Case $SC_CLOSE
			ShellHookWindow($hGui, 0)
			Return $GUI_RUNDEFMSG
			;Exit
	EndSwitch
EndFunc
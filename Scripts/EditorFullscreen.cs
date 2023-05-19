#if UNITY_EDITOR
using System;
using System.Runtime.InteropServices;
using UnityEditor;
using UnityEngine;

public static class EditorFullscreen
{
	#region Imports
	[DllImport("user32.dll")]
	static extern IntPtr GetActiveWindow();

	[DllImport("user32.dll")]
	static extern IntPtr GetMenu(IntPtr hWnd);

	[DllImport("user32.dll")]
	static extern bool DrawMenuBar(IntPtr hWnd);

	[DllImport("user32.dll")]
	static extern int GetMenuItemCount(IntPtr hmenu);

	[DllImport("user32.dll")]
	static extern bool SetMenu( IntPtr hWnd, IntPtr hMenu);

	[DllImport("user32.dll")]
	static extern bool RemoveMenu(IntPtr hMenu,	uint uPosition,	uint uFlags);

	[DllImport("user32.dll")]
	static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);
	
	[DllImport("user32.dll")]
	static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

	[DllImport("user32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
	#endregion

	#region Constants
	const string lastMenuPtr = "toggleFullscreen_lastMenuPtr";
	const string isFullscreen = "toggleFullscreen_isFullscreen";
	const long WS_CAPTION = 0x00C00000;
	const long WS_SYSMENU = 0x00080000;
	const long WS_THICKFRAME = 0x00400000;
	const int GWL_STYLE = -16;
	const int MF_BYPOSITION = 0x40;
	const int MF_REMOVE = 0x10;
	const int SW_MAXIMIZE = 3;
	const int SW_MINIMAZE = 6;
	#endregion

	[MenuItem("Tools/Toggle Fullscreen _F11")]
	static void ToggleFullscreen(){
		var unity = GetActiveWindow();
		var menu = GetMenu(unity);
		var style = GetWindowLongPtr(unity, GWL_STYLE);

		if(menu != IntPtr.Zero){
			PlayerPrefs.SetString(lastMenuPtr, menu.ToString());
			PlayerPrefs.SetInt(isFullscreen, 1);

			int count = GetMenuItemCount(menu);
			for (int i = 0; i < count; i++) RemoveMenu(menu, 0, (MF_BYPOSITION | MF_REMOVE));

			SetWindowLongPtr(unity, GWL_STYLE, new IntPtr(style.ToInt64() & ~(WS_CAPTION | WS_SYSMENU | WS_THICKFRAME)));

			SetMenu(unity, IntPtr.Zero);

			ShowWindow(unity, SW_MINIMAZE);
			ShowWindow(unity, SW_MAXIMIZE);
		} else {
			var lastMenuPtr = long.Parse(PlayerPrefs.GetString(EditorFullscreen.lastMenuPtr));
			PlayerPrefs.SetInt(isFullscreen, 0);

			SetWindowLongPtr(unity, GWL_STYLE, new IntPtr(style.ToInt64() | WS_CAPTION | WS_SYSMENU | WS_THICKFRAME));

			SetMenu(unity, new IntPtr(lastMenuPtr));

			ShowWindow(unity, SW_MAXIMIZE);
		}

		DrawMenuBar(unity);
	}

 	[UnityEditor.Callbacks.DidReloadScripts]
	static void Reload(){
		if (EditorApplication.isPlayingOrWillChangePlaymode) return;

		if (PlayerPrefs.GetInt(isFullscreen, 0) == 1){
			ToggleFullscreen();
		}
	} 	
}
#endif

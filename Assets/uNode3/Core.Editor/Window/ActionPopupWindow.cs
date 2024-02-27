using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MaxyGames.UNode.Editors {
	public delegate void ActionRef<T>(ref T obj);
	/// <summary>
	/// Provide useful function for show custom in popup window.
	/// </summary>
	public class ActionPopupWindow : EditorWindow {
		static List<ActionPopupWindow> windows = new List<ActionPopupWindow>();

		/// <summary>
		/// The name for show header.
		/// </summary>
		public string headerName;
		/// <summary>
		/// The user variable.
		/// </summary>
		public object variable;
		/// <summary>
		/// Called in OnGUI in the Top.
		/// </summary>
		public ActionRef<object> onGUITop;
		/// <summary>
		/// Called in OnGUI in the middle with scroolbar.
		/// </summary>
		public ActionRef<object> onGUI;
		/// <summary>
		/// Called in OnGUI in the bottom.
		/// </summary>
		public ActionRef<object> onGUIBottom;
		/// <summary>
		/// Called when performing Undo or Redo.
		/// </summary>
		public Action onUndoOrRedo;
		public float width = 250, height = 100;
		public bool autoSize;

		private Vector2 scrollPos;

		#region ShowWindow
		public static ActionPopupWindow ShowWindow(
			Rect position, 
			object startValue, 
			ActionRef<object> onGUI,
			float width = 250,
			float height = 100,
			ActionRef<object> onGUITop = null, 
			ActionRef<object> onGUIBottom = null) {
			ActionPopupWindow window = CreateInstance(typeof(ActionPopupWindow)) as ActionPopupWindow;
			window.variable = startValue;
			window.onGUI = onGUI;
			window.onGUITop = onGUITop;
			window.onGUIBottom = onGUIBottom;
			window.width = width;
			window.height = height;
			window.Init(position);
			return window;
		}

		public static ActionPopupWindow ShowWindow(
			Rect position, 
			Action onGUI,
			float width = 250,
			float height = 100,
			Action onGUITop = null, 
			Action onGUIBottom = null) {
			ActionPopupWindow window = CreateInstance(typeof(ActionPopupWindow)) as ActionPopupWindow;
			if(onGUI != null)
				window.onGUI = delegate(ref object obj) { onGUI(); };
			if(onGUITop != null)
				window.onGUITop = delegate(ref object obj) { onGUITop(); };
			if(onGUIBottom != null)
				window.onGUIBottom = delegate(ref object obj) { onGUIBottom(); };
			window.width = width;
			window.height = height;
			window.Init(position);
			return window;
		}

		public static ActionPopupWindow ShowWindow(
			Vector2 position,
			object startValue,
			ActionRef<object> onGUI,
			float width = 250,
			float height = 100,
			ActionRef<object> onGUITop = null,
			ActionRef<object> onGUIBottom = null) {
			ActionPopupWindow window = CreateInstance(typeof(ActionPopupWindow)) as ActionPopupWindow;
			window.variable = startValue;
			window.onGUI = onGUI;
			window.onGUITop = onGUITop;
			window.onGUIBottom = onGUIBottom;
			window.width = width;
			window.height = height;
			window.Init(position);
			return window;
		}

		public static ActionPopupWindow ShowWindow(
			Vector2 position,
			Action onGUI,
			float width = 250,
			float height = 100,
			Action onGUITop = null,
			Action onGUIBottom = null) {
			ActionPopupWindow window = CreateInstance(typeof(ActionPopupWindow)) as ActionPopupWindow;
			if(onGUI != null)
				window.onGUI = delegate (ref object obj) { onGUI(); };
			if(onGUITop != null)
				window.onGUITop = delegate (ref object obj) { onGUITop(); };
			if(onGUIBottom != null)
				window.onGUIBottom = delegate (ref object obj) { onGUIBottom(); };
			window.width = width;
			window.height = height;
			window.Init(position);
			return window;
		}
		#endregion

		public static void CloseAll() {
			foreach(ActionPopupWindow window in windows) {
				if(window != null) {
					window.Close();
				}
			}
			windows.RemoveAll(item => item == null);
		}

		public static void CloseLast() {
			for(int i = windows.Count - 1; i >= 0; i--) {
				if(windows[i] != null) {
					windows[i].Close();
					break;
				}
			}
			windows.RemoveAll(item => item == null);
		}

		public static List<ActionPopupWindow> GetAll() {
			windows.RemoveAll(item => item == null);
			return windows;
		}

		public static ActionPopupWindow GetLast() {
			for(int i = windows.Count - 1; i >= 0; i--) {
				if(windows[i] != null) {
					return windows[i];
				}
			}
			return null;
		}

		private void Init(Vector2 POS) {
			windows.Add(this);
			Rect rect = new Rect(new Vector2(POS.x + width, POS.y), new Vector2(width, height));
			ShowAsDropDown(rect, new Vector2(width, height));
			wantsMouseMove = true;
			Focus();
		}

		private void Init(Rect rect) {
			windows.Add(this);
			rect = uNodeGUIUtility.GUIToScreenRect(rect);
			rect.width = width;
			ShowAsDropDown(rect, new Vector2(rect.width, height));
			wantsMouseMove = true;
			Focus();
			Undo.undoRedoPerformed -= UndoRedoCallback;
			Undo.undoRedoPerformed += UndoRedoCallback;
		}

		static void UndoRedoCallback() {
			var window = GetAll();
			if(window != null && window.Count > 0) {
				for(int i = 0; i < window.Count; i++) {
					if(window[i] != null && window[i].onUndoOrRedo != null) {
						window[i].onUndoOrRedo();
					}
				}
			}
		}

		bool _hasFocus = false;

		void OnGUI() {
			HandleKeyboard();
			if(!string.IsNullOrEmpty(headerName))
				EditorGUILayout.LabelField(headerName, EditorStyles.toolbarButton);
			if (!_hasFocus) {
				EditorGUI.FocusTextInControl("act");
				if (Event.current.type == EventType.Repaint) {
					_hasFocus = true;
				}
			}
			if(onGUITop != null) {
				onGUITop(ref variable);
			}
			scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
			GUI.SetNextControlName("act");
			if(onGUI != null) {
				onGUI(ref variable);
			}
			EditorGUILayout.EndScrollView();
			GUILayout.FlexibleSpace();
			if(onGUIBottom != null) {
				onGUIBottom(ref variable);
			}
		}

		void Update() {
			Repaint();
		}

		void HandleKeyboard() {
			Event current = Event.current;
			if(current.type == EventType.KeyDown) {
				if(current.keyCode == KeyCode.Escape) {
					Close();
					return;
				}
			}
			if(current.type == EventType.KeyDown) {
				Focus();
			}
		}
	}
}
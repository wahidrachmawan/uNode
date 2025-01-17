using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace MaxyGames.UNode.Editors {
	/// <summary>
	/// Provide useful function for show custom window.
	/// </summary>
	public class ActionWindow : EditorWindow {
		static List<ActionWindow> windows = new List<ActionWindow>();

		/// <summary>
		/// The name for show header.
		/// </summary>
		public string headerName;
		/// <summary>
		/// The user variable.
		/// </summary>
		[SerializeReference]
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

		private Vector2 scrollPos;

		#region ShowWindow
		public static ActionWindow Show(object startValue) {
			ActionWindow window = GetWindow(typeof(ActionWindow), true) as ActionWindow;
			window.variable = startValue;
			window.Init();
			return window;
		}

		public static ActionWindow Show(object startValue, ActionRef<object> onGUI) {
			ActionWindow window = GetWindow(typeof(ActionWindow), true) as ActionWindow;
			window.variable = startValue;
			window.onGUI = onGUI;
			window.Init();
			return window;
		}

		public static ActionWindow Show(object startValue, ActionRef<object> onGUI,
			ActionRef<object> onGUITop, ActionRef<object> onGUIBottom) {
			ActionWindow window = GetWindow(typeof(ActionWindow), true) as ActionWindow;
			window.variable = startValue;
			window.onGUI = onGUI;
			window.onGUITop = onGUITop;
			window.onGUIBottom = onGUIBottom;
			window.Init();
			return window;
		}

		public static ActionWindow Show(object startValue, ActionRef<object> onGUI,
			Action onGUITop, Action onGUIBottom) {
			ActionWindow window = GetWindow(typeof(ActionWindow), true) as ActionWindow;
			window.variable = startValue;
			window.onGUI = onGUI;
			if(onGUITop != null)
				window.onGUITop = delegate(ref object obj) { onGUITop(); };
			if(onGUIBottom != null)
				window.onGUIBottom = delegate(ref object obj) { onGUIBottom(); };
			window.Init();
			return window;
		}

		public static ActionWindow Show(Action onGUI,
			Action onGUITop = null, 
			Action onGUIBottom = null) {
			ActionWindow window = GetWindow(typeof(ActionWindow), true) as ActionWindow;
			if(onGUI != null)
				window.onGUI = delegate(ref object obj) { onGUI(); };
			if(onGUITop != null)
				window.onGUITop = delegate(ref object obj) { onGUITop(); };
			if(onGUIBottom != null)
				window.onGUIBottom = delegate(ref object obj) { onGUIBottom(); };
			window.Init();
			return window;
		}
		#endregion

		public static void CloseAll() {
			foreach(ActionWindow window in windows) {
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

		public static List<ActionWindow> GetAll() {
			windows.RemoveAll(item => item == null);
			return windows;
		}

		public static ActionWindow GetLast() {
			for(int i = windows.Count - 1; i >= 0; i--) {
				if(windows[i] != null) {
					return windows[i];
				}
			}
			return null;
		}

		private void Init() {
			windows.Add(this);
			wantsMouseMove = true;
			this.minSize = new Vector2(300, 200);
			this.titleContent = new GUIContent("Editor");
			//this.ShowModal();
			Show();
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

		private void OnEnable() {
			var container = new IMGUIContainer(DrawGUI);
			rootVisualElement.Add(container);
		}

		[SerializeField]
		bool _hasFocus = false;

		void DrawGUI() {
			HandleKeyboard();
			EditorGUILayout.BeginVertical();
			if(!string.IsNullOrEmpty(headerName))
				EditorGUILayout.LabelField(headerName, EditorStyles.toolbarButton);
			if(!_hasFocus) {
				EditorGUI.FocusTextInControl("act");
				if(Event.current.type == EventType.Repaint) {
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
			EditorGUILayout.EndVertical();
			if(GUI.changed) {
				uNodeEditor.GUIChanged();
			}
		}

		void Update() {
			Repaint();
		}

		void HandleKeyboard() {
			Event current = Event.current;
			if(current.type == EventType.KeyDown) {
				Focus();
				if(current.keyCode == KeyCode.Escape) {
					Close();
					return;
				}
			}
		}
	}
}
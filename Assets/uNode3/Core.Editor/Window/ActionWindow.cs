using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace MaxyGames.UNode.Editors {
	/// <summary>
	/// Provide useful function for show custom window.
	/// </summary>
	public class ActionWindow : BaseActionWindow<ActionWindow> {
		protected override void Initialize() {
			this.minSize = new Vector2(300, 200);
			this.titleContent = new GUIContent("Editor");
			ShowUtility();
		}
	}

	public delegate void ActionRef<T>(ref T obj);
	public abstract class BaseActionWindow<T> : EditorWindow where T : BaseActionWindow<T> {
		protected static List<BaseActionWindow<T>> windows = new List<BaseActionWindow<T>>();
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
		public bool autoFocus = true;
		[SerializeField]
		protected bool hasWindowFocus = false;
		[SerializeField]
		protected Vector2 scrollPos;

		#region ShowWindow
		static T Craete(bool createNew) {
			if(createNew) {
				return CreateInstance<T>();
			}
			else {
				return windows.FirstOrDefault(f => f != null) as T ?? CreateInstance<T>();
			}
		}

		public static T Show(object startValue, ActionRef<object> onGUI) {
			T window = Craete(false);
			window.variable = startValue;
			window.onGUI = onGUI;
			window.onGUITop = null;
			window.onGUIBottom = null;
			window.Initialize();
			return window;
		}

		public static T Show(object startValue, ActionRef<object> onGUI,
			Action onGUITop, Action onGUIBottom) {
			T window = Craete(false);
			window.variable = startValue;
			window.onGUI = onGUI;
			if(onGUITop != null)
				window.onGUITop = delegate (ref object obj) { onGUITop(); };
			else
				window.onGUITop = null;
			if(onGUIBottom != null)
				window.onGUIBottom = delegate (ref object obj) { onGUIBottom(); };
			else
				window.onGUIBottom = null;
			window.Initialize();
			return window;
		}

		public static T Show(
			object startValue,
			ActionRef<object> onGUI,
			ActionRef<object> onGUITop = null,
			ActionRef<object> onGUIBottom = null) {
			T window = Craete(false);
			window.variable = startValue;
			window.onGUI = onGUI;
			window.onGUITop = onGUITop;
			window.onGUIBottom = onGUIBottom;
			window.Initialize();
			return window;
		}

		public static T Show(
			Action onGUI,
			Action onGUITop = null,
			Action onGUIBottom = null) {
			T window = Craete(false);
			if(onGUI != null)
				window.onGUI = delegate (ref object obj) { onGUI(); };
			else
				window.onGUI = null;
			if(onGUITop != null)
				window.onGUITop = delegate (ref object obj) { onGUITop(); };
			else
				window.onGUITop = null;
			if(onGUIBottom != null)
				window.onGUIBottom = delegate (ref object obj) { onGUIBottom(); };
			else
				window.onGUIBottom = null;
			window.Initialize();
			return window;
		}

		public static T ShowAsNew(object startValue, ActionRef<object> onGUI) {
			T window = Craete(true);
			window.variable = startValue;
			window.onGUI = onGUI;
			window.onGUITop = null;
			window.onGUIBottom = null;
			window.Initialize();
			return window;
		}

		public static T ShowAsNew(object startValue, ActionRef<object> onGUI,
			Action onGUITop, Action onGUIBottom) {
			T window = Craete(true);
			window.variable = startValue;
			window.onGUI = onGUI;
			if(onGUITop != null)
				window.onGUITop = delegate (ref object obj) { onGUITop(); };
			else
				window.onGUITop = null;
			if(onGUIBottom != null)
				window.onGUIBottom = delegate (ref object obj) { onGUIBottom(); };
			else
				window.onGUIBottom = null;
			window.Initialize();
			return window;
		}

		public static T ShowAsNew(
			object startValue,
			ActionRef<object> onGUI,
			ActionRef<object> onGUITop = null,
			ActionRef<object> onGUIBottom = null) {
			T window = Craete(true);
			window.variable = startValue;
			window.onGUI = onGUI;
			window.onGUITop = onGUITop;
			window.onGUIBottom = onGUIBottom;
			window.Initialize();
			return window;
		}

		public static T ShowAsNew(
			Action onGUI,
			Action onGUITop = null,
			Action onGUIBottom = null) {
			T window = Craete(true);
			if(onGUI != null)
				window.onGUI = delegate (ref object obj) { onGUI(); };
			else
				window.onGUI = null;
			if(onGUITop != null)
				window.onGUITop = delegate (ref object obj) { onGUITop(); };
			else
				window.onGUITop = null;
			if(onGUIBottom != null)
				window.onGUIBottom = delegate (ref object obj) { onGUIBottom(); };
			else
				window.onGUIBottom = null;
			window.Initialize();
			return window;
		}
		#endregion

		public static void CloseAll() {
			foreach(T window in windows) {
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

		public static List<BaseActionWindow<T>> GetAll() {
			windows.RemoveAll(item => item == null);
			return windows;
		}

		public static BaseActionWindow<T> GetLast() {
			for(int i = windows.Count - 1; i >= 0; i--) {
				if(windows[i] != null) {
					return windows[i];
				}
			}
			return null;
		}

		protected abstract void Initialize();

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

		protected IMGUIContainer container;
		public virtual void OnEnable() {
			windows.Add(this);
			Undo.undoRedoPerformed -= UndoRedoCallback;
			Undo.undoRedoPerformed += UndoRedoCallback;
			container = new IMGUIContainer(DrawGUI);
			container.style.borderLeftWidth = 1;
			container.style.borderRightWidth = 1;
			container.style.borderTopWidth = 1;
			container.style.borderBottomWidth = 1;
			container.style.top = 2;
			container.style.borderLeftColor = uNodeGUIStyle.Colors.BorderColor;
			container.style.borderRightColor = uNodeGUIStyle.Colors.BorderColor;
			container.style.borderTopColor = uNodeGUIStyle.Colors.BorderColor;
			container.style.borderBottomColor = uNodeGUIStyle.Colors.BorderColor;
			container.StretchToParentSize();
			rootVisualElement.Add(container);
		}

		protected virtual void DrawGUI() {
			HandleKeyboard();
			EditorGUILayout.BeginVertical();
			if(!string.IsNullOrEmpty(headerName))
				EditorGUILayout.LabelField(headerName, EditorStyles.toolbarButton);
			if(!hasWindowFocus && autoFocus) {
				EditorGUI.FocusTextInControl("act");
				if(Event.current.type == EventType.Repaint) {
					hasWindowFocus = true;
				}
			}
			if(onGUITop != null) {
				onGUITop(ref variable);
			}
			if(onGUI != null) {
				scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
				GUI.SetNextControlName("act");
				onGUI(ref variable);
				EditorGUILayout.EndScrollView();
			}
			if(onGUIBottom != null) {
				GUILayout.FlexibleSpace();
				onGUIBottom(ref variable);
			}
			EditorGUILayout.EndVertical();
			if(GUI.changed) {
				uNodeEditor.GUIChanged();
			}
		}

		protected void HandleKeyboard() {
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
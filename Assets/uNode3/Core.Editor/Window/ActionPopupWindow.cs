using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

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
		public float width = 350, height = 200, maxWidth = 400, maxHeight = 600;
		public bool autoSize = true, autoFocus = true;

		private Vector2 scrollPos;

		#region ShowWindow
		public static ActionPopupWindow Show(
			Rect position, 
			object startValue, 
			ActionRef<object> onGUI,
			ActionRef<object> onGUITop = null, 
			ActionRef<object> onGUIBottom = null) {
			ActionPopupWindow window = CreateInstance(typeof(ActionPopupWindow)) as ActionPopupWindow;
			window.variable = startValue;
			window.onGUI = onGUI;
			window.onGUITop = onGUITop;
			window.onGUIBottom = onGUIBottom;
			window.Init(position);
			return window;
		}

		public static ActionPopupWindow Show(
			Rect position, 
			Action onGUI,
			Action onGUITop = null, 
			Action onGUIBottom = null) {
			ActionPopupWindow window = CreateInstance(typeof(ActionPopupWindow)) as ActionPopupWindow;
			if(onGUI != null)
				window.onGUI = delegate(ref object obj) { onGUI(); };
			if(onGUITop != null)
				window.onGUITop = delegate(ref object obj) { onGUITop(); };
			if(onGUIBottom != null)
				window.onGUIBottom = delegate(ref object obj) { onGUIBottom(); };
			window.Init(position);
			return window;
		}

		public static ActionPopupWindow Show(
			Vector2 position,
			object startValue,
			ActionRef<object> onGUI,
			ActionRef<object> onGUITop = null,
			ActionRef<object> onGUIBottom = null) {
			ActionPopupWindow window = CreateInstance(typeof(ActionPopupWindow)) as ActionPopupWindow;
			window.variable = startValue;
			window.onGUI = onGUI;
			window.onGUITop = onGUITop;
			window.onGUIBottom = onGUIBottom;
			window.Init(position);
			return window;
		}

		public static ActionPopupWindow Show(
			Vector2 position,
			Action onGUI,
			Action onGUITop = null,
			Action onGUIBottom = null) {
			ActionPopupWindow window = CreateInstance(typeof(ActionPopupWindow)) as ActionPopupWindow;
			if(onGUI != null)
				window.onGUI = delegate (ref object obj) { onGUI(); };
			if(onGUITop != null)
				window.onGUITop = delegate (ref object obj) { onGUITop(); };
			if(onGUIBottom != null)
				window.onGUIBottom = delegate (ref object obj) { onGUIBottom(); };
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

		private void OnEnable() {
			var container = new IMGUIContainer(DrawGUI);
			container.style.borderLeftWidth = 1;
			container.style.borderRightWidth = 1;
			container.style.borderTopWidth = 1;
			container.style.borderBottomWidth = 1;
			container.style.top = 2;
			container.style.borderLeftColor = uNodeGUIStyle.Colors.BorderColor;
			container.style.borderRightColor = uNodeGUIStyle.Colors.BorderColor;
			container.style.borderTopColor = uNodeGUIStyle.Colors.BorderColor;
			container.style.borderBottomColor = uNodeGUIStyle.Colors.BorderColor;
			rootVisualElement.Add(container);
			var layout = container.layout;
			var oldAutosize = !autoSize;

			Vector2 startPosition = position.position;
			container.ExecuteAndScheduleAction(() => {
				if(autoSize && layout != container.layout) {
					if(startPosition == Vector2.zero) {
						startPosition = position.position;
					}
					layout = container.layout;
					if(float.IsNaN(layout.width) || float.IsNaN(layout.height)) {
						return;
					}
					var pos = position;
					pos.width = Mathf.Min(maxWidth, layout.width);
					pos.height = Mathf.Min(maxHeight, layout.height) + 4;
					if(pos.width > position.width || pos.height > position.height) {
						pos.width = maxWidth;
						pos.height = maxHeight;
					}
					position = pos;
					pos.x = startPosition.x;
					pos.y = startPosition.y;
					this.ChangePosition(pos);
				}
				if(oldAutosize != autoSize) {
					oldAutosize = autoSize;
					if(autoSize) {
						IStyle style = container.style;
						style.position = new(StyleKeyword.Null);
						style.left = new(StyleKeyword.Null);
						style.top = new(StyleKeyword.Null);
						style.right = new(StyleKeyword.Null);
						style.bottom = new(StyleKeyword.Null);
					}
					else {
						container.StretchToParentSize();
					}
				}
			}, 1);
		}

		void DrawGUI() {
			HandleKeyboard();
			if(!string.IsNullOrEmpty(headerName))
				EditorGUILayout.LabelField(headerName, EditorStyles.toolbarButton);
			if (!_hasFocus && autoFocus) {
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
using UnityEngine;
using UnityEditor;
using System.Linq;

namespace MaxyGames.UNode.Editors {
	public class FieldsEditorWindow : EditorWindow {
		public static FieldsEditorWindow window;

		public object targetField;
		public string propertyPath;
		public Object targetObject;
		public int actionIndex;
		public System.Func<object, object> GetTargetField;
		public object userObject;

		public event System.Action<object> onChanged;

		private Vector2 scrollPos;
		private bool focus;

		#region ShowWindow
		public static FieldsEditorWindow ShowWindow(object targetField, Object targetObject, string propertyPath = null) {
			FieldsEditorWindow window = ShowWindow();
			window.targetField = targetField;
			window.targetObject = targetObject;
			window.propertyPath = propertyPath;
			return window;
		}

		public static FieldsEditorWindow ShowWindow(object targetField, Object targetObject, System.Func<object> GetTargetField, object userObject = null) {
			FieldsEditorWindow window = ShowWindow();
			window.propertyPath = "";
			window.targetField = targetField;
			window.targetObject = targetObject;
			window.GetTargetField = delegate (object obj) {
				return GetTargetField();
			};
			window.userObject = userObject;
			return window;
		}

		public static FieldsEditorWindow ShowWindow(object targetField, Object targetObject, System.Func<object, object> GetTargetField, object userObject = null) {
			FieldsEditorWindow window = ShowWindow();
			window.propertyPath = "";
			window.targetField = targetField;
			window.targetObject = targetObject;
			window.GetTargetField = GetTargetField;
			window.userObject = userObject;
			return window;
		}

		public static FieldsEditorWindow ShowWindow() {
			Undo.undoRedoPerformed -= UndoRedoCallback;
			window = GetWindow(typeof(FieldsEditorWindow), true) as FieldsEditorWindow;
			window.propertyPath = "";
			window.minSize = new Vector2(250, 300);
			window.focus = true;
			window.Show();
			Undo.undoRedoPerformed += UndoRedoCallback;
			window.titleContent = new GUIContent("Fields Editor");
			return window;
		}
		#endregion

		void OnGUI() {
			if(targetField == null && targetObject != null) {
				FindTargetField();
			}
			if(targetField == null || targetObject == null) {
				ShowNotification(new GUIContent("No target"));
				return;
			}
			if(targetField != null && targetObject != null) {
				scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
				if(targetField is UnityEngine.Object) {
					uNodeGUIUtility.EditUnityObject(targetField as UnityEngine.Object);
				} else {
					uNodeGUIUtility.ShowFields(targetField, targetObject);
				}
				//if(targetField is EventAction) {
				//	System.Reflection.FieldInfo field = targetField.GetType().GetField("Name");
				//	uNodeEditorUtility.ShowField(field, targetField, targetObject);
				//}
				EditorGUILayout.EndScrollView();
				uNodeEditorUtility.MarkDirty(targetObject);
				if(!focus) {
					GUI.FocusControl(null);
					EditorGUI.FocusTextInControl(null);
					focus = true;
				}
				if(GUI.changed) {
					if(onChanged != null) {
						onChanged(targetField);
					}
					uNodeGUIUtility.GUIChanged(targetObject);
				}
			}
		}

		void FindTargetField() {
			//if(!string.IsNullOrEmpty(propertyPath)) {
			//	object data = PropertyDrawerUtility.GetActualObjectFromPath<object>(propertyPath, targetObject);
			//	if(data is EventData) {
			//		if(data != null && actionIndex < (data as EventData).blocks.Count) {
			//			targetField = (data as EventData).blocks[actionIndex].block;
			//		}
			//	} else {
			//		targetField = data;
			//	}
			//} else if(GetTargetField != null) {
			//	targetField = GetTargetField(userObject);
			//}
		}

		static void UndoRedoCallback() {
			if(window == null || window.targetObject == null)
				return;
			window.FindTargetField();
			window.Repaint();
		}
	}
}
using UnityEditor;
using UnityEngine;

namespace MaxyGames.UNode.Editors {
	public class GraphInspectorWindow : EditorWindow {
		public static GraphInspectorWindow window;
		CustomInspector inspectorWrapper;
		Editor inspectorEditor;

		[MenuItem("Tools/uNode/Graph Inspector", false, 100)]
		public static void ShowWindow() {
			window = (GraphInspectorWindow)GetWindow(typeof(GraphInspectorWindow), false);
			window.minSize = new Vector2(300, 250);
			window.autoRepaintOnSceneChange = true;
			window.wantsMouseMove = true;
			window.titleContent = new GUIContent("Graph Inspector");
			window.Show();
			if(uNodeEditor.SavedData.rightVisibility) {
				uNodeEditor.SavedData.rightVisibility = false;
			}
		}

		private void OnGUI() {
			inspectorEditor?.OnInspectorGUI();
		}

		private void OnGraphEditorChanged(GraphEditorData graphData) {
			inspectorWrapper.editorData = graphData;
			inspectorEditor = CustomInspector.GetEditor(inspectorWrapper);
			inspectorEditor?.Repaint();
			Repaint();
		}

		private void OnEnable() {
			window = this;
			inspectorWrapper = ScriptableObject.CreateInstance<CustomInspector>();
			if(uNodeEditor.window != null) {
				OnGraphEditorChanged(uNodeEditor.window.graphData);
			}
			uNodeEditor.onSelectionChanged -= OnGraphEditorChanged;
			uNodeEditor.onSelectionChanged += OnGraphEditorChanged;
		}

		private void OnDisable() {
			if(window == this)
				window = null;
			uNodeEditor.onSelectionChanged -= OnGraphEditorChanged;
		}
	}
}
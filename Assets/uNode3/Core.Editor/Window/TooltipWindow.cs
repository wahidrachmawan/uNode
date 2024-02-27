using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace MaxyGames.UNode.Editors {
	public class TooltipWindow : EditorWindow {
		private float width = 300, height = 300;
		private IList<GUIContent> contents;
		private Vector2 scrollPos;
		private bool isOver = false, isInitailzed;

		private static TooltipWindow window;

		private static System.Reflection.MethodInfo _showTooltipMethod;
		private static System.Reflection.MethodInfo showTooltipMethod {
			get {
				if(_showTooltipMethod == null) {
					_showTooltipMethod = typeof(EditorWindow).GetMethod("ShowTooltip",
						System.Reflection.BindingFlags.NonPublic |
						System.Reflection.BindingFlags.Instance);
				}
				return _showTooltipMethod;
			}
		}

		public static TooltipWindow Show(Vector2 pos, IList<GUIContent> contents, float width = 300, float height = 300) {
			if(window == null)
				window = CreateInstance(typeof(TooltipWindow)) as TooltipWindow;
			window.width = width;
			window.height = height;
			window.contents = contents;
			window.isInitailzed = false;
			window.isOver = false;
			window.position = new Rect(pos.x, pos.y, width, height);
			window.Repaint();
			if(showTooltipMethod != null) {//Hack internal method so other window will not lose focus like an build in tooltip.
				showTooltipMethod.Invoke(window, null);
			} else {
				window.ShowPopup();
			}
			//window.ShowAsDropDown(WindowUtility.MousePosToRect(pos, Vector2.zero), new Vector2(width, height));
			return window;
		}

		private void OnGUI() {
			if(contents != null) {
				GUI.Box(new Rect(0, 0, position.width, position.height), "");
				Rect rect = EditorGUILayout.BeginVertical();
				if(isOver)
					scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
				for(int i = 0; i < contents.Count; i++) {
					if(i == 0 && contents[i].image != null) {
						if(contents.Count > 1) {
							EditorGUI.LabelField(uNodeGUIUtility.GetRect(2), contents[i], uNodeGUIStyle.RichLabel);
						} else {
							EditorGUI.LabelField(uNodeGUIUtility.GetRect(2), new GUIContent(contents[i].image), uNodeGUIStyle.CenterRichLabel);
							EditorGUILayout.LabelField(contents[i].text, uNodeGUIStyle.RichLabel);
						}
						continue;
					}
					if(contents[i].image != null) {
						EditorGUI.LabelField(uNodeGUIUtility.GetRect(), contents[i], uNodeGUIStyle.RichLabel2);
					} else {
						EditorGUILayout.LabelField(contents[i], uNodeGUIStyle.RichLabel);
					}
				}
				if(isOver)
					EditorGUILayout.EndScrollView();
				EditorGUILayout.EndVertical();
				if(!isInitailzed && Event.current.type == EventType.Repaint) {
					if(rect.height > height) {
						isOver = true;
						window.position = new Rect(window.position.x, window.position.y, position.width, height);
						if(showTooltipMethod != null) {//Hack internal method so other window will not lose focus like an build in tooltip.
							showTooltipMethod.Invoke(window, null);
						} else {
							window.ShowPopup();
						}
					} else {
						window.position = new Rect(window.position.x, window.position.y, position.width, rect.height);
						if(showTooltipMethod != null) {//Hack internal method so other window will not lose focus like an build in tooltip.
							showTooltipMethod.Invoke(window, null);
						} else {
							window.ShowPopup();
						}
					}
				}
			} else if(Event.current.type == EventType.Repaint) {
				Close();
			}
		}
	}
}
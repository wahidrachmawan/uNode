using System;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;

namespace MaxyGames.UNode.Editors {
	public enum DisplayValueKind {
		Outside,
		Inside,
	}

	[System.Serializable]
	public class UIElementEditorTheme : EditorTheme {
		public bool coloredPortBorder = true;
		public bool coloredReference;
		public bool coloredNodeBorder = true;
		public DisplayValueKind preferredDisplayValue;

		[Header("Minimap")]
		public bool enableMinimap = true;
		[Range(0.1f, 1)]
		public float minimapOpacity = 0.6f;
		public Color minimapViewportColor = new Color(1f, 1f, 0f, 0.35f);
		public Color minimapSelectedNodeColor = new Color(1f, 1f, 1f, 0.5f);

		public enum MinimapType {
			Floating,
			Anchored,
			UpperLeft,
			UpperRight,
			BottomLeft,
			BottomRight,
		}
		[HideInInspector]
		public MinimapType minimapType;
		[HideInInspector]
		public Rect minimapPosition = new Rect(0, 0, 200, 180);
		
		[Header("Debug")]
		public Color breakpointColor = Color.red;
		public Color nodeSuccessColor = Color.green;
		public Color nodeFailureColor = Color.red;
		public Color nodeRunningColor = Color.blue;

		[Header("Styles")]
		public StyleSheet graphStyle;
		public StyleSheet nodeStyle;
		// public StyleSheet controlStyle;
		public StyleSheet blockStyle;
		public StyleSheet transitionStyle;
		public StyleSheet graphPanelStyle;
		public StyleSheet portStyle;
		public StyleSheet tabbarStyle;

		public override Type GetGraphType() {
			return typeof(UIElementGraph);
		}
	}
}
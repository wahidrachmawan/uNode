using System.Collections;
using UnityEngine;
using UnityEditor;

namespace MaxyGames.UNode.Editors {
	[InitializeOnLoad]
	public static class ColorPalette {
        static ColorPalette() {
			if(EditorGUIUtility.isProSkin) {
				unityColor = new Color(0.2196079f, 0.2196079f, 0.2196079f);
			}
			else {
				unityColor = new Color(0.7607844f, 0.7607844f, 0.7607844f);
			}
			IsDarkTheme = EditorGUIUtility.isProSkin;
		}

		public static Color snappingLineColor => new Color(0.2666667f, 0.7529412f, 1, 0.2666667f);

		public static readonly Color unityColor;

		public static readonly bool IsDarkTheme;
	}
}
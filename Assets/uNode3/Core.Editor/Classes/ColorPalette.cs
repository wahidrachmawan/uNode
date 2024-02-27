using System.Collections;
using UnityEngine;
using UnityEditor;

namespace MaxyGames.UNode.Editors {
	public static class ColorPalette {
		public static Color snappingLineColor => new Color(0.2666667f, 0.7529412f, 1, 0.2666667f);

		public static Color unityColor {
            get {
                if(EditorGUIUtility.isProSkin) {
                    return new Color(0.2196079f, 0.2196079f, 0.2196079f);
                } else {
                    return new Color(0.7607844f, 0.7607844f, 0.7607844f);
                }
            }
        }
	}
}
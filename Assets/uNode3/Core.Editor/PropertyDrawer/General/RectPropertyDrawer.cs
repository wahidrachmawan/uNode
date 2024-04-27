using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace MaxyGames.UNode.Editors.Drawer {
	class RectPropertyDrawer : UPropertyDrawer<Rect> {
		private static GUIContent[] contents = new GUIContent[] { new GUIContent("X"), new GUIContent("Y"), new GUIContent("W"), new GUIContent("H") };

		public override void Draw(Rect position, DrawerOption option) {
			EditorGUI.BeginChangeCheck();
			var fieldValue = GetValue(option.property);
			var arr = new[] { fieldValue.x, fieldValue.y, fieldValue.width, fieldValue.height };
			EditorGUI.MultiFloatField(position, option.label, contents, arr);
			if(EditorGUI.EndChangeCheck()) {
				option.value = new Rect(arr[0], arr[1], arr[2], arr[3]);
			}
		}
	}
}
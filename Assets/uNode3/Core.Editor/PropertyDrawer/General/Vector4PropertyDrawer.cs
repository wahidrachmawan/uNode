using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace MaxyGames.UNode.Editors.Drawer {
	class Vector4PropertyDrawer : UPropertyDrawer<Vector4> {
		public override void Draw(Rect position, DrawerOption option) {
			EditorGUI.BeginChangeCheck();
			var fieldValue = GetValue(option.property);
			fieldValue = EditorGUI.Vector4Field(position, option.label, fieldValue);
			if(EditorGUI.EndChangeCheck()) {
				option.value = fieldValue;
			}
		}

		public override void DrawLayouted(DrawerOption option) {
			EditorGUI.BeginChangeCheck();
			var fieldValue = GetValue(option.property);
			fieldValue = EditorGUILayout.Vector4Field(option.label, fieldValue);
			if(EditorGUI.EndChangeCheck()) {
				option.value = fieldValue;
			}
		}
	}
}
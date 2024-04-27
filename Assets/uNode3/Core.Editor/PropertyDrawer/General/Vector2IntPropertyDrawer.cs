using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace MaxyGames.UNode.Editors.Drawer {
	class Vector2IntPropertyDrawer : UPropertyDrawer<Vector2Int> {
		public override void Draw(Rect position, DrawerOption option) {
			EditorGUI.BeginChangeCheck();
			var fieldValue = GetValue(option.property);
			fieldValue = EditorGUI.Vector2IntField(position, option.label, fieldValue);
			if(EditorGUI.EndChangeCheck()) {
				option.value = fieldValue;
			}
		}

		public override void DrawLayouted(DrawerOption option) {
			EditorGUI.BeginChangeCheck();
			var fieldValue = GetValue(option.property);
			fieldValue = EditorGUILayout.Vector2IntField(option.label, fieldValue);
			if(EditorGUI.EndChangeCheck()) {
				option.value = fieldValue;
			}
		}
	}
}
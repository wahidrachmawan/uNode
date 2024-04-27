using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace MaxyGames.UNode.Editors.Drawer {
	class QuaternionPropertyDrawer : UPropertyDrawer<Quaternion> {
		public override void Draw(Rect position, DrawerOption option) {
			EditorGUI.BeginChangeCheck();
			var fieldValue = GetValue(option.property);
			fieldValue = Quaternion.Euler(EditorGUI.Vector3Field(position, option.label, fieldValue.eulerAngles));
			if(EditorGUI.EndChangeCheck()) {
				option.value = fieldValue;
			}
		}

		public override void DrawLayouted(DrawerOption option) {
			EditorGUI.BeginChangeCheck();
			var fieldValue = GetValue(option.property);
			fieldValue = Quaternion.Euler(EditorGUILayout.Vector3Field(option.label, fieldValue.eulerAngles));
			if(EditorGUI.EndChangeCheck()) {
				option.value = fieldValue;
			}
		}
	}
}
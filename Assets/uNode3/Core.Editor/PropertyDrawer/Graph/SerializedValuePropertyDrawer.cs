using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace MaxyGames.UNode.Editors.Drawer {
	class SerializedValuePropertyDrawer : UPropertyDrawer<SerializedValue> {
		public override void Draw(Rect position, DrawerOption option) {
			EditorGUI.BeginChangeCheck();
			var fieldValue = GetValue(option.property, false);
			if(fieldValue != null) {
				uNodeGUIUtility.EditSerializedValue(fieldValue, option.label, fieldValue.type, option.unityObject);
			}
			if(EditorGUI.EndChangeCheck()) {
				option.value = fieldValue;
			}
		}
	}
}
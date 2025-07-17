using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace MaxyGames.UNode.Editors.Drawer {
	class SerializedValueFieldControl : FieldControl<SerializedValue> {
		public override bool IsValidControl(Type type, bool layouted) {
			return base.IsValidControl(type, layouted) && layouted;
		}

		public override void DrawLayouted(object value, GUIContent label, Type type, Action<object> onChanged, uNodeUtility.EditValueSettings settings) {
			EditorGUI.BeginChangeCheck();
			var fieldValue = GetValue(value, false);
			if(fieldValue != null) {
				uNodeGUIUtility.EditSerializedValue(fieldValue, label, fieldValue.type, settings.unityObject);
			}
			if(EditorGUI.EndChangeCheck()) {
				onChanged?.Invoke(fieldValue);
			}
		}
	}
}
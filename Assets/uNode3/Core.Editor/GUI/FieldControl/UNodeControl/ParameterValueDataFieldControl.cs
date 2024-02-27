using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace MaxyGames.UNode.Editors.Control {
	class ParameterValueDataFieldControl : FieldControl<ParameterValueData> {
		public override bool IsValidControl(Type type, bool layouted) {
			return layouted && base.IsValidControl(type, layouted);
		}

		public override void DrawLayouted(object value, GUIContent label, Type type, Action<object> onChanged, uNodeUtility.EditValueSettings settings) {
			EditorGUI.BeginChangeCheck();
			ValidateValue(ref value, false);
			var fieldValue = value as ParameterValueData;
			Type t = fieldValue.type;
			if(t != null) {
				if(t.IsValueType && fieldValue.value == null) {
					fieldValue.value = ReflectionUtils.CreateInstance(t);
					GUI.changed = true;
				}
				uNodeGUIUtility.EditValueLayouted(label, fieldValue.value, t, delegate (object val) {
					fieldValue.value = val;
					onChanged(fieldValue);
				}, new uNodeUtility.EditValueSettings() { nullable = true, unityObject = settings.unityObject });
			}
			if(EditorGUI.EndChangeCheck()) {
				onChanged(value);
			}
		}
	}
}
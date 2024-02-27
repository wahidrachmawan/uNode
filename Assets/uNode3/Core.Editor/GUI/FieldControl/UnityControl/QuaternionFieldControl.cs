using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace MaxyGames.UNode.Editors.Control {
	class QuaternionFieldControl : FieldControl<Quaternion> {
		public override void Draw(Rect position, GUIContent label, object value, Type type, Action<object> onChanged, uNodeUtility.EditValueSettings settings) {
			EditorGUI.BeginChangeCheck();
			ValidateValue(ref value);
			var oldValue = (Quaternion)value;
			var newValue = Quaternion.Euler(EditorGUI.Vector3Field(position, label, oldValue.eulerAngles));
			if(EditorGUI.EndChangeCheck()) {
				onChanged(newValue);
			}
		}

		public override void DrawLayouted(object value, GUIContent label, Type type, Action<object> onChanged, uNodeUtility.EditValueSettings settings) {
			DrawDecorators(settings);
			EditorGUI.BeginChangeCheck();
			ValidateValue(ref value);
			var oldValue = (Quaternion)value;
			var newValue = Quaternion.Euler(EditorGUILayout.Vector3Field(label, oldValue.eulerAngles));
			if(EditorGUI.EndChangeCheck()) {
				onChanged(newValue);
			}
		}
	}
}
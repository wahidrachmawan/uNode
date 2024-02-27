using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace MaxyGames.UNode.Editors.Control {
	class EnumFieldControl : FieldControl {
		public override bool IsValidControl(Type type, bool layouted) {
			if(type.IsEnum) {
				return true;
			}
			return false;
		}

		public override void Draw(Rect position, GUIContent label, object value, Type type, Action<object> onChanged, uNodeUtility.EditValueSettings settings) {
			EditorGUI.BeginChangeCheck();
			if(value == null) {
				value = ReflectionUtils.CreateInstance(type) as Enum;
				GUI.changed = true;
			}
			else if(value is int) {
				value = Enum.ToObject(type, value);
			}
			else if(value.GetType() != type) {
				value = ReflectionUtils.CreateInstance(type);
			}
			var oldValue = (Enum)value;
			var newValue = EditorGUI.EnumPopup(position, label, oldValue);
			if(EditorGUI.EndChangeCheck()) {
				onChanged(newValue);
			}
		}
	}
}
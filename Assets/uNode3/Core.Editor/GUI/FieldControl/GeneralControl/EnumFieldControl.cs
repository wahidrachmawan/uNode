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
			else if(ReflectionUtils.IsTypeEqual(value.GetType(), type) == false) {
				value = ReflectionUtils.CreateInstance(type);
			}
			var val = (Enum)value;
			if(type.IsDefinedAttribute<System.FlagsAttribute>()) {
				val = EditorGUI.EnumFlagsField(position, label, val);
			}
			else {
				val = EditorGUI.EnumPopup(position, label, val);
			}
			if(EditorGUI.EndChangeCheck()) {
				onChanged(val);
			}
		}
	}
}
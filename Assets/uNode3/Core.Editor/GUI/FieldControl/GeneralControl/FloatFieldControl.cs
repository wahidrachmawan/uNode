using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace MaxyGames.UNode.Editors.Control {
	class FloatFieldControl : FieldControl<float> {
		public override void Draw(Rect position, GUIContent label, object value, Type type, Action<object> onChanged, uNodeUtility.EditValueSettings settings) {
			EditorGUI.BeginChangeCheck();
			ValidateValue(ref value);
			var fieldValue = (float)value;
			var att = ReflectionUtils.GetAttribute<RangeAttribute>(settings.attributes);
			if(att != null) {
				fieldValue = EditorGUI.Slider(position, label, fieldValue, att.min, att.max);
			}
			else {
				fieldValue = EditorGUI.FloatField(position, label, fieldValue);
			}
			if(EditorGUI.EndChangeCheck()) {
				onChanged(fieldValue);
			}
		}
	}
}
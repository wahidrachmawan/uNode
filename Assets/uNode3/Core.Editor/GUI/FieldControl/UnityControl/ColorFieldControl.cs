using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace MaxyGames.UNode.Editors.Control {
	class ColorFieldControl : FieldControl<Color> {
		public override void Draw(Rect position, GUIContent label, object value, Type type, Action<object> onChanged, uNodeUtility.EditValueSettings settings) {
			EditorGUI.BeginChangeCheck();
			ValidateValue(ref value);
			var fieldValue = (Color)value;
			var usage = ReflectionUtils.GetAttribute<ColorUsageAttribute>(settings.attributes);
			if(usage != null) {
				fieldValue = EditorGUI.ColorField(position, label, fieldValue, true, usage.showAlpha, usage.hdr);
			}
			else {
				fieldValue = EditorGUI.ColorField(position, label, fieldValue);
			}
			if(EditorGUI.EndChangeCheck()) {
				onChanged(fieldValue);
			}
		}
	}
}
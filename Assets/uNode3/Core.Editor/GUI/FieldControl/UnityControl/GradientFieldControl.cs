using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace MaxyGames.UNode.Editors.Control {
	class GradientFieldControl : FieldControl<Gradient> {
		public override void Draw(Rect position, GUIContent label, object value, Type type, Action<object> onChanged, uNodeUtility.EditValueSettings settings) {
			EditorGUI.BeginChangeCheck();
			ValidateValue(ref value);
			var fieldValue = value as Gradient;
			var usage = ReflectionUtils.GetAttribute<GradientUsageAttribute>(settings.attributes);
			if(usage != null) {
				fieldValue = EditorGUI.GradientField(position, label, fieldValue, usage.hdr);
			}
			else {
				fieldValue = EditorGUI.GradientField(position, label, fieldValue);
			}
			if(EditorGUI.EndChangeCheck()) {
				onChanged(fieldValue);
			}
		}
	}
}
using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace MaxyGames.UNode.Editors.Control {
	class AnimationCurveFieldControl : FieldControl<AnimationCurve> {
		public override void Draw(Rect position, GUIContent label, object value, Type type, Action<object> onChanged, uNodeUtility.EditValueSettings settings) {
			EditorGUI.BeginChangeCheck();
			ValidateValue(ref value, settings != null ? settings.nullable : false);
			var fieldValue = value as AnimationCurve;
			if(value != null) {
				if(settings.nullable)
					position.width -= 16;
				fieldValue = EditorGUI.CurveField(position, label, fieldValue);
				if(settings.nullable) {
					position.x += position.width;
					position.width = 16;
					if(GUI.Button(position, GUIContent.none) && Event.current.button == 0) {
						fieldValue = null;
						GUI.changed = true;
					}
				}
			}
			else {
				uNodeGUIUtility.DrawNullValue(position, label, type, delegate (object o) {
					fieldValue = o as AnimationCurve;
					onChanged(o);
				});
			}
			if(EditorGUI.EndChangeCheck()) {
				onChanged(fieldValue);
			}
		}
	}
}
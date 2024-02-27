using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace MaxyGames.UNode.Editors.Control {
	class CharFieldControl : FieldControl<char> {
		public override void Draw(Rect position, GUIContent label, object value, Type type, Action<object> onChanged, uNodeUtility.EditValueSettings settings) {
			EditorGUI.BeginChangeCheck();
			ValidateValue(ref value);
			var oldValue = (char)value;
			var newValue = EditorGUI.TextField(position, label, oldValue.ToString());
			if(!string.IsNullOrEmpty(newValue)) {
				oldValue = newValue[0];
			}
			else {
				oldValue = new char();
			}
			if(EditorGUI.EndChangeCheck()) {
				onChanged(oldValue);
			}
		}
	}
}
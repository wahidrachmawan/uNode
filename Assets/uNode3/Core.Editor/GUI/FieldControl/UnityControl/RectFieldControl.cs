using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace MaxyGames.UNode.Editors.Control {
	class RectFieldControl : FieldControl<Rect> {
		private static GUIContent[] contents = new GUIContent[] { new GUIContent("X"), new GUIContent("Y"), new GUIContent("W"), new GUIContent("H") };

		public override void Draw(Rect position, GUIContent label, object value, Type type, Action<object> onChanged, uNodeUtility.EditValueSettings settings) {
			EditorGUI.BeginChangeCheck();
			ValidateValue(ref value);
			var oldValue = (Rect)value;
			var arr = new[] { oldValue.x, oldValue.y, oldValue.width, oldValue.height };
			EditorGUI.MultiFloatField(position, label, contents, arr);
			if(EditorGUI.EndChangeCheck()) {
				onChanged(new Rect(arr[0], arr[1], arr[2], arr[3]));
			}
		}
	}
}
using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace MaxyGames.UNode.Editors.Control {
	class UGraphElementFieldControl : FieldControl {
		public override bool IsValidControl(Type type, bool layouted) {
			return type.IsCastableTo(typeof(UGraphElement)) || type.IsCastableTo(typeof(Node));
		}

		public override void Draw(Rect position, GUIContent label, object value, Type type, Action<object> onChanged, uNodeUtility.EditValueSettings settings) {
			position = EditorGUI.PrefixLabel(position, label);
			uNodeGUI.DrawReference(position, value, type);
		}
	}
	class UGraphElementRefFieldControl : FieldControl {
		public override bool IsValidControl(Type type, bool layouted) {
			return type == typeof(UGraphElementRef);
		}

		public override void Draw(Rect position, GUIContent label, object value, Type type, Action<object> onChanged, uNodeUtility.EditValueSettings settings) {
			position = EditorGUI.PrefixLabel(position, label);
			uNodeGUI.DrawReference(position, value, type);
		}
	}
}
using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace MaxyGames.UNode.Editors.Drawer {
	class UnityObjectPropertyDrawer : UPropertyDrawer<Object> {
		public override void Draw(Rect position, DrawerOption option) {
			EditorGUI.BeginChangeCheck();
			var fieldValue = GetValue(option.property, true);
			fieldValue = EditorGUI.ObjectField(position, option.label, fieldValue, option.type, !EditorUtility.IsPersistent(option.unityObject));
			if(EditorGUI.EndChangeCheck()) {
				option.value = fieldValue;
			}
		}

		public override bool IsValid(System.Type type, bool layouted) {
			return typeof(Object).IsAssignableFrom(type);
		}
	}
}
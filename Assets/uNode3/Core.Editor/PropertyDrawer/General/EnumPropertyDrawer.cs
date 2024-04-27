using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace MaxyGames.UNode.Editors.Drawer {
	class EnumPropertyDrawer : UPropertyDrawer {
		public override void Draw(Rect position, DrawerOption option) {
			EditorGUI.BeginChangeCheck();
			var fieldValue = GetValue(option.property, option.type) as Enum;
			fieldValue = EditorGUI.EnumPopup(position, option.label, fieldValue);
			if(EditorGUI.EndChangeCheck()) {
				option.value = fieldValue;
			}
		}

		public override bool IsValid(Type type, bool layouted) {
			return type.IsEnum;
		}
	}
}
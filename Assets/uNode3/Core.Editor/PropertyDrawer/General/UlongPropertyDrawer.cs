using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace MaxyGames.UNode.Editors.Drawer {
	class UlongPropertyDrawer : UPropertyDrawer<ulong> {
		public override void Draw(Rect position, DrawerOption option) {
			EditorGUI.BeginChangeCheck();
			var fieldValue = GetValue(option.property);
			fieldValue = (ulong)EditorGUI.LongField(position, option.label, (long)fieldValue);
			if(EditorGUI.EndChangeCheck()) {
				option.value = fieldValue;
			}
		}
	}
}
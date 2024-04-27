using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace MaxyGames.UNode.Editors {
	public class CharPropertyDrawer : UPropertyDrawer<char> {
		public override void Draw(Rect position, DrawerOption option) {
			EditorGUI.BeginChangeCheck();
			var fieldValue = GetValue(option.property);
			var newValue = EditorGUI.TextField(position, option.label, fieldValue.ToString());
			if(!string.IsNullOrEmpty(newValue)) {
				fieldValue = newValue[0];
			} else {
				fieldValue = new char();
			}
			if(EditorGUI.EndChangeCheck()) {
				option.value = fieldValue;
			}
		}
	}
}
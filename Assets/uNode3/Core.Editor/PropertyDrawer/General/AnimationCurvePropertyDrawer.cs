using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace MaxyGames.UNode.Editors.Drawer {
	class AnimationCurvePropertyDrawer : UPropertyDrawer<AnimationCurve> {
		public override void Draw(Rect position, DrawerOption option) {
			EditorGUI.BeginChangeCheck();
			var fieldValue = GetValue(option.property, option.nullable);
			if(fieldValue != null) {
				if(option.nullable)
					position.width -= 16;
				fieldValue = EditorGUI.CurveField(position, option.label, fieldValue);
				if(option.nullable) {
					position.x += position.width;
					position.width = 16;
					if(GUI.Button(position, GUIContent.none) && Event.current.button == 0) {
						fieldValue = null;
						GUI.changed = true;
					}
				}
			} else {
				uNodeGUIUtility.DrawNullValue(position, option.label, option.type, delegate (object o) {
					fieldValue = o as AnimationCurve;
					option.value = fieldValue;
				});
			}
			if(EditorGUI.EndChangeCheck()) {
				option.value = fieldValue;
			}
		}
	}
}
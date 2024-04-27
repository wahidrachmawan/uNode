using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace MaxyGames.UNode.Editors.Drawer {
	class IntPropertyDrawer : UPropertyDrawer<int> {
		public override void Draw(Rect position, DrawerOption option) {
			EditorGUI.BeginChangeCheck();
			var fieldValue = GetValue(option.property);
			var att = ReflectionUtils.GetAttribute<RangeAttribute>(option.property.GetCustomAttributes());
			if(att != null) {
				fieldValue = EditorGUI.IntSlider(position, option.label, fieldValue, (int)att.min, (int)att.max);
			} else {
				fieldValue = EditorGUI.IntField(position, option.label, fieldValue);
			}
			if(EditorGUI.EndChangeCheck()) {
				option.value = fieldValue;
			}
		}
	}
}
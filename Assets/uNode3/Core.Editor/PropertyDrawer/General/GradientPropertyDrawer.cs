using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace MaxyGames.UNode.Editors.Drawer {
	class GradientPropertyDrawer : UPropertyDrawer<Gradient> {
		public override void Draw(Rect position, DrawerOption option) {
			EditorGUI.BeginChangeCheck();
			var fieldValue = GetValue(option.property);
			var att = ReflectionUtils.GetAttribute<GradientUsageAttribute>(option.property.GetCustomAttributes());
			if(att != null) {
				fieldValue = EditorGUI.GradientField(position, option.label, fieldValue, att.hdr);
			} else {
				fieldValue = EditorGUI.GradientField(position, option.label, fieldValue);
			}
			if(EditorGUI.EndChangeCheck()) {
				option.value = fieldValue;
			}
		}
	}
}
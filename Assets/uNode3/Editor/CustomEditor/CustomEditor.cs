using System.Linq;
using System;
using UnityEngine;
using UnityEditor;
using System.Reflection;
using System.Collections.Generic;

namespace MaxyGames.UNode.Editors.Drawer {
	[CustomPropertyDrawer(typeof(SerializedType))]
	class SerializedTypeDrawer : PropertyDrawer {
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
			SerializedType variable = property.boxedValue as SerializedType;
			FilterAttribute filter = null;
			if(fieldInfo.GetCustomAttributes(typeof(FilterAttribute), false).Length > 0) {
				filter = (FilterAttribute)fieldInfo.GetCustomAttributes(typeof(FilterAttribute), false)[0];
			}
			else {
				filter = new FilterAttribute(typeof(object));
			}
			filter.OnlyGetType = true;
			if(fieldInfo.GetCustomAttributes(typeof(TooltipAttribute), false).Length > 0) {
				label.tooltip = ((TooltipAttribute)fieldInfo.GetCustomAttributes(typeof(TooltipAttribute), false)[0]).tooltip;
			}
			uNodeGUIUtility.DrawTypeDrawer(position, variable, label, (t) => {
				variable.type = t;
			}, filter);
		}
	}
}
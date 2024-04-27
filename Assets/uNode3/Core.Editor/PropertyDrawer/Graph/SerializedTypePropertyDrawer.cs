using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace MaxyGames.UNode.Editors.Drawer {
	class SerializedTypePropertyDrawer : UPropertyDrawer<SerializedType> {
		public override void Draw(Rect position, DrawerOption option) {
			var attributes = option.attributes;
			EditorGUI.BeginChangeCheck();
			var fieldValue = GetValue(option.property, false);
			if(fieldValue != null) {
				if(option.nullable)
					position.width -= 16;
				FilterAttribute filter = ReflectionUtils.GetAttribute<FilterAttribute>(attributes);
				if(filter == null) {
					var OTA = ReflectionUtils.GetAttribute<ObjectTypeAttribute>(attributes);
					if(OTA != null) {
						if(OTA.isElementType) {
							if(OTA.type != null) {
								filter = new FilterAttribute(OTA.type.ElementType());
							}
						} else {
							filter = new FilterAttribute(OTA.type);
						}
					}
				}
				uNodeGUIUtility.DrawTypeDrawer(position, fieldValue, option.label, (type) => {
					option.value = new SerializedType(type);
				}, filter, option.unityObject);
			}
			if(EditorGUI.EndChangeCheck()) {
				option.value = fieldValue;
			}
		}
	}
}
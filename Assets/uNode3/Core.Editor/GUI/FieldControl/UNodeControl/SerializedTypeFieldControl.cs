using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace MaxyGames.UNode.Editors.Drawer {
	class SerializedTypeFieldControl : FieldControl<SerializedType> {
		public override void Draw(Rect position, GUIContent label, object value, Type type, Action<object> onChanged, uNodeUtility.EditValueSettings settings) {
			var attributes = settings.attributes;
			EditorGUI.BeginChangeCheck();
			var fieldValue = GetValue(value, false);
			if(fieldValue != null) {
				if(settings.nullable)
					position.width -= 16;
				FilterAttribute filter = ReflectionUtils.GetAttribute<FilterAttribute>(attributes);
				if(filter == null) {
					var OTA = ReflectionUtils.GetAttribute<ObjectTypeAttribute>(attributes);
					if(OTA != null) {
						if(OTA.isElementType) {
							if(OTA.type != null) {
								filter = new FilterAttribute(OTA.type.ElementType());
							}
						}
						else {
							filter = new FilterAttribute(OTA.type);
						}
					}
				}
				uNodeGUIUtility.DrawTypeDrawer(position, fieldValue, label, (type) => {
					fieldValue = new SerializedType(type);
					onChanged?.Invoke(fieldValue);
				}, filter, settings.unityObject);
			}
			if(EditorGUI.EndChangeCheck()) {
				onChanged?.Invoke(fieldValue);
			}
		}
	}
}
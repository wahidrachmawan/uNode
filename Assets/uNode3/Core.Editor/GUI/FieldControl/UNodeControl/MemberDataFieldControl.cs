using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace MaxyGames.UNode.Editors.Drawer {
	class MemberDataFieldControl : FieldControl<MemberData> {
		public override void Draw(Rect position, GUIContent label, object value, Type type, Action<object> onChanged, uNodeUtility.EditValueSettings settings) {
			var attributes = settings.attributes;
			EditorGUI.BeginChangeCheck();
			var fieldValue = value as MemberData;
			if(fieldValue == null) {
				if(settings.nullable) {
					fieldValue = null;
				}
				else {
					fieldValue = MemberData.None;
					ObjectTypeAttribute OTA = ReflectionUtils.GetAttribute<ObjectTypeAttribute>(attributes);
					FilterAttribute filter = ReflectionUtils.GetAttribute<FilterAttribute>(attributes);
					if(OTA != null && (filter == null || !filter.SetMember)) {
						fieldValue = MemberData.Empty;
						if(OTA.type == typeof(string)) {
							fieldValue = new MemberData("");
						}
					}
					else {
						if(filter != null && !filter.SetMember) {
							fieldValue = MemberData.Empty;
							if(filter.IsValidType(typeof(string))) {
								fieldValue = new MemberData("");
							}
						}
					}
					GUI.changed = true;
				}
			}
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
				uNodeGUIUtility.RenderVariable(position, fieldValue, label, filter, settings.unityObject, (m) => {
					fieldValue = m;
					onChanged?.Invoke(value);
				});
				if(fieldValue.targetType == MemberData.TargetType.Values && fieldValue.type != null &&
					(fieldValue.type.IsArray || fieldValue.type.IsCastableTo(typeof(IList)))) {
					EditorGUI.indentLevel++;
					uNodeGUIUtility.DrawMemberValues(new GUIContent("Values"), fieldValue, fieldValue.type, filter, null, (m) => {
						fieldValue = m;
						onChanged?.Invoke(value);
					});
					EditorGUI.indentLevel--;
				}
				if(settings.nullable) {
					position.x += position.width;
					position.width = 16;
					if(GUI.Button(position, GUIContent.none) && Event.current.button == 0) {
						fieldValue = null;
						GUI.changed = true;
					}
				}
			}
			else {
				uNodeGUIUtility.DrawNullValue(position, label, type, delegate (object o) {
					fieldValue = o as MemberData;
					onChanged?.Invoke(value);
				});
			}
			if(EditorGUI.EndChangeCheck()) {
				onChanged?.Invoke(fieldValue);
			}
		}
	}
}
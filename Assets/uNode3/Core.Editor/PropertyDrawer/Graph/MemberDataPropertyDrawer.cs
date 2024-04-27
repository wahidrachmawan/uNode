using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace MaxyGames.UNode.Editors.Drawer {
	class MemberDataPropertyDrawer : UPropertyDrawer<MemberData> {
		public override void Draw(Rect position, DrawerOption option) {
			var attributes = option.attributes;
			EditorGUI.BeginChangeCheck();
			var fieldValue = GetValue(option.property, option.nullable);
			if(fieldValue == null) {
				if(option.nullable) {
					fieldValue = null;
				} else {
					fieldValue = MemberData.None;
					ObjectTypeAttribute OTA = ReflectionUtils.GetAttribute<ObjectTypeAttribute>(attributes);
					FilterAttribute filter = ReflectionUtils.GetAttribute<FilterAttribute>(attributes);
					if(OTA != null && (filter == null || !filter.SetMember)) {
						fieldValue = MemberData.Empty;
						if(OTA.type == typeof(string)) {
							fieldValue = new MemberData("");
						}
					} else {
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
				uNodeGUIUtility.RenderVariable(position, fieldValue, option.label, filter, option.unityObject, (m) => {
					option.value = m;
				});
				if(fieldValue.targetType == MemberData.TargetType.Values && fieldValue.type != null &&
					(fieldValue.type.IsArray || fieldValue.type.IsCastableTo(typeof(IList)))) {
					EditorGUI.indentLevel++;
					uNodeGUIUtility.DrawMemberValues(new GUIContent("Values"), fieldValue, fieldValue.type, filter, null, (m) => {
						option.value = m;
					});
					EditorGUI.indentLevel--;
				}
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
					fieldValue = o as MemberData;
					option.value = fieldValue;
				});
			}
			if(EditorGUI.EndChangeCheck()) {
				option.value = fieldValue;
			}
		}
	}
}
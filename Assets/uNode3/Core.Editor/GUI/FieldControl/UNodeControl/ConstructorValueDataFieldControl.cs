using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace MaxyGames.UNode.Editors.Control {
	class ConstructorValueDataFieldControl : FieldControl<ConstructorValueData> {
		public override bool IsValidControl(Type type, bool layouted) {
			return layouted && base.IsValidControl(type, layouted);
		}

		public override void DrawLayouted(object value, GUIContent label, Type type, Action<object> onChanged, uNodeUtility.EditValueSettings settings) {
			EditorGUI.BeginChangeCheck();
			ValidateValue(ref value, false);
			var fieldValue = value as ConstructorValueData;
			Type t = fieldValue.type;
			if(settings?.parentValue != null) {
				var parent = settings.parentValue;
				if(parent is AttributeData att && att.type != null) {
					if(t != att.type) {
						t = att.type;
						ConstructorInfo[] ctors = t.GetConstructors();
						foreach(ConstructorInfo ctor in ctors) {
							ParameterInfo[] pInfo = ctor.GetParameters();
							if(pInfo.Length > 0) {
								bool valid = true;
								foreach(var p in pInfo) {
									//Ignore out and ref parameter.
									if(p.IsOut || p.ParameterType.IsByRef) {
										valid = false;
										break;
									}
								}
								if(!valid) {
									continue;
								}
							}
							fieldValue = new ConstructorValueData(ctor);
							if(onChanged != null) {
								onChanged(fieldValue);
							}
						}
					}
				}
			}
			if(t != null) {
				Rect rect = uNodeGUIUtility.GetRect();
				rect = EditorGUI.PrefixLabel(rect, label);
				if(EditorGUI.DropdownButton(rect, new GUIContent(fieldValue.ToString()), FocusType.Keyboard) && Event.current.button == 0) {
					GenericMenu menu = new GenericMenu();
					/*
					if(Value.type.IsPrimitive || IsSupportedType(Value.type)) {
						menu.AddItem(new GUIContent("Dirrect Edit Value"), false, delegate() {
							uNodeEditorUtility.RegisterUndo(unityObject, "");
							Value.value = new ObjectValueData() { value = ReflectionUtils.CreateInstance(Value.type) };
						});
					}*/
					ConstructorInfo[] ctors = t.GetConstructors();
					foreach(ConstructorInfo ctor in ctors) {
						ParameterInfo[] pInfo = ctor.GetParameters();
						if(pInfo.Length > 0) {
							bool valid = true;
							foreach(var p in pInfo) {
								//Ignore out and ref parameter.
								if(p.IsOut || p.ParameterType.IsByRef) {
									valid = false;
									break;
								}
							}
							if(!valid) {
								continue;
							}
						}
						menu.AddItem(new GUIContent(EditorReflectionUtility.GetPrettyMethodName(ctor)), false, delegate (object obj) {
							uNodeEditorUtility.RegisterUndo(settings?.unityObject);
							fieldValue = new ConstructorValueData(obj as ConstructorInfo);
							if(onChanged != null) {
								onChanged(fieldValue);
							}
						}, ctor);
					}
					menu.ShowAsContext();
				}
				if(fieldValue.parameters != null && fieldValue.parameters.Length > 0) {
					uNodeGUI.DrawHeader("Parameters");
					for(int i = 0; i < fieldValue.parameters.Length; i++) {
						var o = fieldValue.parameters[i];
						var index = i;
						uNodeGUIUtility.EditValueLayouted(new GUIContent(fieldValue.parameters[i].name, fieldValue.parameters[i].name),
							o,
							typeof(ParameterValueData),
							delegate (object ob) {
								uNodeEditorUtility.RegisterUndo(settings?.unityObject);
								fieldValue.parameters[index] = ob as ParameterValueData;
								if(onChanged != null) {
									onChanged(fieldValue);
								}
								uNodeGUIUtility.GUIChanged(settings?.unityObject);
							}, new uNodeUtility.EditValueSettings() { nullable = true, unityObject = settings?.unityObject });
					}
				}
				uNodeGUIUtility.DrawConstructorInitializer(fieldValue, val => onChanged?.Invoke(val), settings?.unityObject);
			}
			if(EditorGUI.EndChangeCheck()) {
				onChanged(value);
			}
		}
	}
}
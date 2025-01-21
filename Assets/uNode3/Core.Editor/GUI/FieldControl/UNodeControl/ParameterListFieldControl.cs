using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace MaxyGames.UNode.Editors.Control {
	class ParameterListFieldControl : FieldControl<List<ParameterData>> {
		public override void DrawLayouted(object value, GUIContent label, Type type, Action<object> onChanged, uNodeUtility.EditValueSettings settings) {
			EditorGUI.BeginChangeCheck();
			ValidateValue(ref value);
			var fieldValue = value as List<ParameterData>;
			if(fieldValue != null) {
				uNodeGUI.DrawCustomList(fieldValue, label.text,
					drawElement: (pos, index, parameter) => {
						var name = EditorGUI.DelayedTextField(new Rect(pos.x, pos.y, pos.width, EditorGUIUtility.singleLineHeight), "Name", parameter.name);
						if(name != parameter.name) {
							parameter.name = uNodeUtility.AutoCorrectName(name);
							onChanged(fieldValue);
							uNodeGUIUtility.GUIChangedMajor(settings?.unityObject);
						}
						pos.y += EditorGUIUtility.singleLineHeight;
						uNodeGUIUtility.DrawTypeDrawer(
							new Rect(pos.x, pos.y, pos.width, EditorGUIUtility.singleLineHeight),
							parameter.type,
							new GUIContent("Type"),
							type => {
								uNodeEditorUtility.RegisterUndo(settings?.unityObject);
								parameter.type = type;
								onChanged(fieldValue);
								uNodeGUIUtility.GUIChangedMajor(settings?.unityObject);
							}, null, settings?.unityObject);
						pos.y += EditorGUIUtility.singleLineHeight;
						uNodeGUIUtility.EditValue(
							new Rect(pos.x, pos.y, pos.width, EditorGUIUtility.singleLineHeight),
							new GUIContent("Ref Kind"),
							parameter.refKind,
							onChange: (val) => {
								uNodeEditorUtility.RegisterUndo(settings?.unityObject);
								parameter.refKind = val;
								onChanged(fieldValue);
							});
						pos.y += EditorGUIUtility.singleLineHeight;
						uNodeGUIUtility.EditValue(
							new Rect(pos.x, pos.y, pos.width, EditorGUIUtility.singleLineHeight),
							new GUIContent("Has Default Value"),
							parameter.hasDefaultValue,
							onChange: (val) => {
								uNodeEditorUtility.RegisterUndo(settings?.unityObject);
								parameter.hasDefaultValue = val;
								onChanged(fieldValue);
							});
						if(parameter.hasDefaultValue) {
							var type = parameter.Type;
							if(ReflectionUtils.IsConstantType(type)) {
								if(type.IsValueType) {
									if(parameter.defaultValue == null) {
										parameter.defaultValue = ReflectionUtils.CreateInstance(type);
										onChanged(fieldValue);
									}
								}
								if(parameter.defaultValue != null && parameter.defaultValue.GetType() != type) {
									parameter.defaultValue = ReflectionUtils.CreateInstance(type);
									onChanged(fieldValue);
								}
								pos.y += EditorGUIUtility.singleLineHeight;
								uNodeGUIUtility.EditValue(
									new Rect(pos.x, pos.y, pos.width, EditorGUIUtility.singleLineHeight),
									new GUIContent("Default Value"),
									parameter.defaultValue, 
									type,
									onChange: (val) => {
										uNodeEditorUtility.RegisterUndo(settings?.unityObject);
										parameter.defaultValue = val;
										onChanged(fieldValue);
									});
							}
							else {
								if(parameter.defaultValue != null) {
									parameter.defaultValue = null;
									onChanged(fieldValue);
								}
								pos.y += EditorGUIUtility.singleLineHeight;
								var position = new Rect(pos.x, pos.y, pos.width, EditorGUIUtility.singleLineHeight);
								position = EditorGUI.PrefixLabel(position, new GUIContent("Default Value"));
								EditorGUI.HelpBox(position, type.IsValueType ? CG.New(type.PrettyName()) : "null", MessageType.None);
							}
						}
						if(settings.parentValue is Constructor constructor && constructor.InitializerType != ConstructorInitializer.None) {
							pos.y += EditorGUIUtility.singleLineHeight;
							uNodeGUIUtility.EditValue(
								new Rect(pos.x, pos.y, pos.width, EditorGUIUtility.singleLineHeight),
								new GUIContent("Use In Initializer"),
								parameter.useInInitializer,
								onChange: (val) => {
									uNodeEditorUtility.RegisterUndo(settings?.unityObject);
									parameter.useInInitializer = val;
									onChanged(fieldValue);
								});
						}
					},
					add: pos => {
						fieldValue.Add(new ParameterData("newParameter", typeof(object)));
						onChanged(fieldValue);
						uNodeGUIUtility.GUIChangedMajor(settings?.unityObject);
					},
					remove: index => {
						fieldValue.RemoveAt(index);
						onChanged(fieldValue);
						uNodeGUIUtility.GUIChangedMajor(settings?.unityObject);
					},
					elementHeight: index => {
						float heightMultiply = 4;
						if(fieldValue[index].hasDefaultValue) {
							heightMultiply++;
						}
						if(settings.parentValue is Constructor constructor && constructor.InitializerType != ConstructorInitializer.None) {
							heightMultiply++;
						}
						return EditorGUIUtility.singleLineHeight * heightMultiply;
					}
				);
			}

			if(EditorGUI.EndChangeCheck()) {
				onChanged(fieldValue);
			}
		}
	}
}
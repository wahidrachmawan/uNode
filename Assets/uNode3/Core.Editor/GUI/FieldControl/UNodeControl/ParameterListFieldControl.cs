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
						uNodeGUIUtility.DrawTypeDrawer(
							new Rect(pos.x, pos.y + EditorGUIUtility.singleLineHeight, pos.width, EditorGUIUtility.singleLineHeight),
							parameter.type,
							new GUIContent("Type"),
							type => {
								uNodeEditorUtility.RegisterUndo(settings?.unityObject);
								parameter.type = type;
								onChanged(fieldValue);
								uNodeGUIUtility.GUIChangedMajor(settings?.unityObject);
							}, null, settings?.unityObject);
						uNodeGUIUtility.EditValue(
							new Rect(pos.x, pos.y + EditorGUIUtility.singleLineHeight * 2, pos.width, EditorGUIUtility.singleLineHeight),
							new GUIContent("Ref Kind"),
							parameter.refKind,
							onChange: (val) => {
								uNodeEditorUtility.RegisterUndo(settings?.unityObject);
								parameter.refKind = val;
								onChanged(fieldValue);
							});
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
						return EditorGUIUtility.singleLineHeight * 3;
					}
				);
			}

			if(EditorGUI.EndChangeCheck()) {
				onChanged(fieldValue);
			}
		}
	}
}
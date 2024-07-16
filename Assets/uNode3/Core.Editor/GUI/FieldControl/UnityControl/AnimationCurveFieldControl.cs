using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace MaxyGames.UNode.Editors.Control {
	class AnimationCurveFieldControl : FieldControl<AnimationCurve> {
		static AnimationCurve buffer { get => uNodeEditorUtility.CopiedValue<AnimationCurve>.value; set => uNodeEditorUtility.CopiedValue<AnimationCurve>.value = value; }

		public override void Draw(Rect position, GUIContent label, object value, Type type, Action<object> onChanged, uNodeUtility.EditValueSettings settings) {
			EditorGUI.BeginChangeCheck();
			ValidateValue(ref value, settings != null ? settings.nullable : false);
			var fieldValue = value as AnimationCurve;
			if(value != null) {
				if(settings.nullable)
					position.width -= 16;
				fieldValue = EditorGUI.CurveField(position, label, fieldValue);
				if(Event.current.type == EventType.MouseDown && Event.current.button == 1 && position.Contains(Event.current.mousePosition)) {
					GenericMenu context = new GenericMenu();

					context.AddItem(new GUIContent("Copy"), false, () => { buffer = SerializerUtility.Duplicate(fieldValue); });
					context.AddItem(new GUIContent("Paste"), false, () => {
						if(buffer == null) return;

						fieldValue = new AnimationCurve(buffer.keys);
						fieldValue.preWrapMode = buffer.preWrapMode;
						fieldValue.postWrapMode = buffer.postWrapMode;
						onChanged(fieldValue);
					});

					context.ShowAsContext();
					Event.current.Use();
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
					fieldValue = o as AnimationCurve;
					onChanged(o);
				});
			}
			if(EditorGUI.EndChangeCheck()) {
				onChanged(fieldValue);
			}
		}
	}
}
using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace MaxyGames.UNode.Editors.Control {
	class UnityObjectFieldControl : FieldControl<UnityEngine.Object> {
		public override bool IsValidControl(Type type, bool layouted) {
			return type == typeof(UnityEngine.Object) || type.IsSubclassOf(typeof(UnityEngine.Object)) && type is not RuntimeType;
		}

		public override void Draw(Rect position, GUIContent label, object value, Type type, Action<object> onChanged, uNodeUtility.EditValueSettings settings) {
			bool flag = true;
			if(settings.acceptUnityObject == false) {
				flag = false;
				if(settings.HasAttribute<AllowAssetReferenceAttribute>()) {
					flag = true;
				}
			}
			else if(settings.unityObject is IGraph graph && !graph.GetGraphType().IsCastableTo(typeof(UnityEngine.Object))) {
				flag = false;
				if(settings.HasAttribute<AllowAssetReferenceAttribute>()) {
					flag = true;
				}
			}
			if(flag) {
				EditorGUI.BeginChangeCheck();
				ValidateValue(ref value);
				var oldValue = value as UnityEngine.Object;
				position = EditorGUI.PrefixLabel(position, label);
				var newValue = EditorGUI.ObjectField(position, oldValue, type, uNodeEditorUtility.IsSceneObject(settings.unityObject));
				if(EditorGUI.EndChangeCheck()) {
					onChanged(newValue);
				}
			}
			else {
				position = EditorGUI.PrefixLabel(position, label);
				uNodeGUI.Label(position, "null", EditorStyles.helpBox);
				if(value != null) {
					onChanged(null);
				}
			}
		}

		public override void DrawLayouted(object value, GUIContent label, Type type, Action<object> onChanged, uNodeUtility.EditValueSettings settings) {
			bool flag = true;
			if(settings.acceptUnityObject == false) {
				flag = false;
				if(settings.HasAttribute<AllowAssetReferenceAttribute>()) {
					flag = true;
				}
			}
			else if(settings.unityObject is IGraph graph && !graph.GetGraphType().IsCastableTo(typeof(UnityEngine.Object))) {
				flag = false;
				if(settings.HasAttribute<AllowAssetReferenceAttribute>()) {
					flag = true;
				}
			}
			if(flag) {
				DrawDecorators(settings);
				EditorGUI.BeginChangeCheck();
				ValidateValue(ref value);
				var oldValue = value as UnityEngine.Object;
				var newValue = EditorGUI.ObjectField(uNodeGUIUtility.GetRect(), label, oldValue, type, uNodeEditorUtility.IsSceneObject(settings.unityObject));
				if(EditorGUI.EndChangeCheck()) {
					onChanged(newValue);
				}
			}
			else {
				var position = EditorGUI.PrefixLabel(uNodeGUIUtility.GetRect(), label);
				uNodeGUI.Label(position, "null", EditorStyles.helpBox);
				if(value != null) {
					onChanged(null);
				}
			}
		}

		class InterfaceFieldControl : FieldControl {
			public override int order => int.MaxValue;

			public override bool IsValidControl(Type type, bool layouted) {
				return type.IsInterface;
			}

			public override void Draw(Rect position, GUIContent label, object value, Type type, Action<object> onChanged, uNodeUtility.EditValueSettings settings) {
				bool flag = true;
				if(settings.acceptUnityObject == false) {
					flag = false;
					if(settings.HasAttribute<AllowAssetReferenceAttribute>()) {
						flag = true;
					}
				}
				else if(settings.unityObject is IGraph graph && !graph.GetGraphType().IsCastableTo(typeof(UnityEngine.Object))) {
					flag = false;
					if(settings.HasAttribute<AllowAssetReferenceAttribute>()) {
						flag = true;
					}
				}
				if(flag) {
					EditorGUI.BeginChangeCheck();
					var oldValue = value as UnityEngine.Object;
					var newValue = EditorGUI.ObjectField(position, label, oldValue, type, uNodeEditorUtility.IsSceneObject(settings.unityObject));
					if(EditorGUI.EndChangeCheck()) {
						onChanged(newValue);
					}
				}
				else {
					position = EditorGUI.PrefixLabel(position, label);
					uNodeGUI.Label(position, "null", EditorStyles.helpBox);
					if(value != null) {
						onChanged(null);
					}
				}
			}

			public override void DrawLayouted(object value, GUIContent label, Type type, Action<object> onChanged, uNodeUtility.EditValueSettings settings) {
				bool flag = true;
				if(settings.acceptUnityObject == false) {
					flag = false;
					if(settings.HasAttribute<AllowAssetReferenceAttribute>()) {
						flag = true;
					}
				}
				else if(settings.unityObject is IGraph graph && !graph.GetGraphType().IsCastableTo(typeof(UnityEngine.Object))) {
					flag = false;
					if(settings.HasAttribute<AllowAssetReferenceAttribute>()) {
						flag = true;
					}
				}
				if(flag) {
					DrawDecorators(settings);
					EditorGUI.BeginChangeCheck();
					var oldValue = value as UnityEngine.Object;
					var newValue = EditorGUI.ObjectField(uNodeGUIUtility.GetRect(), label, oldValue, type, uNodeEditorUtility.IsSceneObject(settings.unityObject));
					if(EditorGUI.EndChangeCheck()) {
						onChanged(newValue);
					}
				}
				else {
					var position = EditorGUI.PrefixLabel(uNodeGUIUtility.GetRect(), label);
					uNodeGUI.Label(position, "null", EditorStyles.helpBox);
					if(value != null) {
						onChanged(null);
					}
				}
			}
		}
	}
}
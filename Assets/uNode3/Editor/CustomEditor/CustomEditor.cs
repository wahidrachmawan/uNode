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
				property.boxedValue = variable;
				property.serializedObject.ApplyModifiedProperties();
			}, filter);
		}
	}

	[CustomPropertyDrawer(typeof(GraphGuidAttribute))]
	class GraphGuidAttributeDrawer : PropertyDrawer {
		private static Dictionary<Type, Type[]> _derivedTypes = new();

		private void EnsureTypes(Type type) {
			if(!_derivedTypes.TryGetValue(type, out var types)) {
				types = EditorReflectionUtility.GetRuntimeTypes()
					.Where(t => t.IsClass && !t.IsAbstract && type.IsAssignableFrom(t))
					.ToArray();
				_derivedTypes[type] = types;
			}
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
			if(property.propertyType == SerializedPropertyType.ObjectReference) {
				var att = (GraphGuidAttribute)attribute;
				var path = AssetDatabase.GUIDToAssetPath(att.guid);
				if(!string.IsNullOrEmpty(path)) {
					var asset = AssetDatabase.LoadMainAssetAtPath(path);
					if(asset is IGraph graph) {
						var graphType = graph.GetGraphType();
						if(graphType is RuntimeType) {
							uNodeGUIUtility.EditRuntimeTypeValue(position, label, property.objectReferenceValue, graphType as RuntimeType, val => {
								property.objectReferenceValue = val as UnityEngine.Object;
								property.serializedObject.ApplyModifiedProperties();
							}, EditorUtility.IsPersistent(property.serializedObject.targetObject) == false);
						}
						else {
							EditorGUI.PropertyField(position, property, label, true);
						}
					}
					return;
				}
			}
			else if(property.propertyType == SerializedPropertyType.ManagedReference) {
				var att = (GraphGuidAttribute)attribute;
				var path = AssetDatabase.GUIDToAssetPath(att.guid);
				if(!string.IsNullOrEmpty(path)) {
					var asset = AssetDatabase.LoadMainAssetAtPath(path);
					if(asset is IGraph graph) {
						var graphType = graph.GetGraphType();
						if(graphType is RuntimeType) {
							uNodeGUIUtility.EditRuntimeTypeValue(position, label, property.managedReferenceValue, graphType as RuntimeType, val => {
								property.managedReferenceValue = val;
								property.serializedObject.ApplyModifiedProperties();
							}, EditorUtility.IsPersistent(property.serializedObject.targetObject) == false);
							//TODO: add support for edit child fields
							//if(property.managedReferenceValue != null) {
							//	var value = property.managedReferenceValue;
							//	if(Application.isPlaying == false && value is IRuntimeClassContainer) {
							//		var container = value as IRuntimeClassContainer;
							//		if(container.IsInitialized) {
							//			//This will make sure the instanced value is up to date.
							//			container.ResetInitialization();
							//		}
							//	}
							//	if(value is IRuntimeGraphWrapper graphWrapper) {
							//		uNodeGUI.DrawRuntimeGraphVariables(graphWrapper);
							//	}
							//}
						}
						else {
							EditorGUI.PropertyField(position, property, label, false);
						}
					}
					return;
				}
			}
			EditorGUI.PropertyField(position, property, label, true);
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
			if(property.propertyType != SerializedPropertyType.ObjectReference) {
				return EditorGUI.GetPropertyHeight(property, label, false);
			}

			return EditorGUI.GetPropertyHeight(property, label, false);
		}
	}
}
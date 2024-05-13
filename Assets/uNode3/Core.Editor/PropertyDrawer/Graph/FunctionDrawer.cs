using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace MaxyGames.UNode.Editors.Drawer {
	class FunctionDrawer : UGraphElementDrawer<Function> {
		protected override void DrawHeader(DrawerOption option) {
			var value = option.value as Function;
			DrawNicelyHeader(option, value.ReturnType());
		}

		protected override void DoDraw(DrawerOption option) {
			var value = option.value as Function;
			if(value.parent is not Property) {
				UInspector.Draw(option.property[nameof(Function.modifier)]);
			}
			uNodeGUIUtility.DrawTypeDrawer(value.ReturnType(), new GUIContent("Return Type"), type => {
				value.returnType = type;
				uNodeGUIUtility.GUIChangedMajor(value);
			}, targetObject: option.unityObject, filter: new FilterAttribute() { OnlyGetType = true, VoidType = true });
			
			if(value.modifier.Static) {
				bool isExtension = value.attributes.Any(a => a.attributeType == typeof(System.Runtime.CompilerServices.ExtensionAttribute));

				if(isExtension != EditorGUILayout.Toggle("Extension", isExtension)) {
					if(isExtension == false) {
						value.attributes.Add(new AttributeData(typeof(System.Runtime.CompilerServices.ExtensionAttribute)));
					} else {
						var att = value.attributes.First(a => a.attributeType == typeof(System.Runtime.CompilerServices.ExtensionAttribute));
						value.attributes.Remove(att);
					}
				}
			}

			UInspector.Draw(option.property[nameof(Function.parameters)]);
			uNodeGUI.DrawAttribute(value.attributes, option.unityObject, (a) => {
				value.attributes = a;
			}, AttributeTargets.Method);
		}
	}
}
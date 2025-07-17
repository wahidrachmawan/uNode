using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace MaxyGames.UNode.Editors.Drawer {
	class PropertyObjectDrawer : UGraphElementDrawer<Property> {
		protected override void DrawHeader(DrawerOption option) {
			var value = option.value as Property;
			DrawNicelyHeader(option, value.ReturnType());
		}

		protected override void DoDraw(DrawerOption option) {
			var value = option.value as Property;
			bool isInterface = option.unityObject is IScriptInterface;

			uNodeGUIUtility.DrawTypeDrawer(value.ReturnType(), new GUIContent("Type"), type => {
				value.type = type;
				uNodeGUIUtility.GUIChangedMajor(value);
			}, targetObject: option.unityObject);


			if(isInterface && value.AutoProperty) {
				value.modifier.SetPublic();
				UInspector.Draw(option.property[nameof(value.accessor)]);
			}
			else {
				value.accessor = PropertyAccessorKind.ReadWrite;
				UInspector.Draw(option.property[nameof(value.modifier)]);
				UInspector.Draw(option.property[nameof(value.getterModifier)]);
				UInspector.Draw(option.property[nameof(value.setterModifier)]);
			}

			uNodeGUI.DrawAttribute(value.attributes, option.unityObject, (a) => {
				value.attributes = a;
			}, AttributeTargets.Property);

			if(value.AutoProperty && !isInterface) {
				uNodeGUI.DrawAttribute(value.fieldAttributes, option.unityObject, (a) => {
					value.fieldAttributes = a;
				}, AttributeTargets.Field, "Field Attributes");
				uNodeGUI.DrawAttribute(value.getterAttributes, option.unityObject, (a) => {
					value.attributes = a;
				}, AttributeTargets.Property, "Getter Attributes");
				uNodeGUI.DrawAttribute(value.setterAttributes, option.unityObject, (a) => {
					value.setterAttributes = a;
				}, AttributeTargets.Property, "Setter Attributes");
			}
		}
	}
}
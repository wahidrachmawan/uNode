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
			base.DoDraw(option);
			uNodeGUIUtility.DrawTypeDrawer(value.ReturnType(), new GUIContent("Return Type"), type => {
				value.returnType = type;
				uNodeGUIUtility.GUIChangedMajor(value);
			}, targetObject: option.unityObject, filter: new FilterAttribute() { OnlyGetType = true, VoidType = true });
			UInspector.Draw(option.property[nameof(Function.parameters)]);
			uNodeGUI.DrawAttribute(value.attributes, option.unityObject, (a) => {
				value.attributes = a;
			}, AttributeTargets.Method);
		}
	}
}
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace MaxyGames.UNode.Editors.Drawer {
	class ConstructorDrawer : UGraphElementDrawer<Constructor> {
		protected override void DrawHeader(DrawerOption option) {
			var value = option.value as Constructor;
			DrawNicelyHeader(option, value.ReturnType());
		}

		protected override void DoDraw(DrawerOption option) {
			//var value = option.value as Constructor;
			base.DoDraw(option);
			UInspector.Draw(option.property[nameof(Constructor.parameters)]);
			//VariableEditorUtility.DrawAttribute(value.attributes, option.unityObject, (a) => {
			//	value.attributes = a.ToArray();
			//}, AttributeTargets.Constructor);
		}
	}
}
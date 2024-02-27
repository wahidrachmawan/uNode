using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;

namespace MaxyGames.UNode.Editors {
    [CustomEditor(typeof(ClassScript), true)]
    public class ClassScriptEditor : GraphAssetEditor {
		FilterAttribute inheritFilter;

		private void OnEnable() {
			inheritFilter = new FilterAttribute(FilterAttribute.DefaultInheritFilter);
			inheritFilter.DisplayRuntimeType = true;
			inheritFilter.ValidateType = type => {
				if(type is RuntimeType) {
					var selfType = (target as IReflectionType).ReflectionType;
					if(selfType != null) {
						if(type.IsSubclassOf(selfType) || type == selfType) {
							return false;
						}
						return type is INativeMember;
					}
					return false;
				}
				return true;
			};
		}

		public override void DrawGUI(bool isInspector) {
			var asset = target as ClassScript;
			uNodeGUIUtility.ShowField(nameof(asset.icon), asset, asset);
			uNodeGUIUtility.ShowField(nameof(asset.modifier), asset, asset);

			int popupIndex = asset.inheritType == typeof(ValueType) ? 1 : 0;
			var newPopupIndex = EditorGUILayout.Popup(new GUIContent("Type Kind"), popupIndex, new[] { "Class", "Struct" });
			if(popupIndex != newPopupIndex) {
				Undo.RegisterCompleteObjectUndo(asset, "Change Type Kind");
				if(newPopupIndex == 0) {
					asset.inheritType = typeof(object);
				}
				else {
					asset.inheritType = typeof(ValueType);
				}
			}
			if(popupIndex == 0) {
				uNodeGUIUtility.DrawTypeDrawer(uNodeGUIUtility.GetRect(), asset.inheritType, new GUIContent("Inherit From"), (type) => {
					asset.inheritType = type;
				}, inheritFilter, asset);
			}
			base.DrawGUI(isInspector);
		}
	}
}
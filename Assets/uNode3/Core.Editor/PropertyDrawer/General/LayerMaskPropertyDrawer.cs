using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace MaxyGames.UNode.Editors.Drawer {
	class LayerMaskPropertyDrawer : UPropertyDrawer<LayerMask> {
		public override void Draw(Rect position, DrawerOption option) {
			EditorGUI.BeginChangeCheck();
			var fieldValue = GetValue(option.property);
            fieldValue = EditorGUI.MaskField(
                position,
                option.label,
                UnityEditorInternal.InternalEditorUtility.LayerMaskToConcatenatedLayersMask(fieldValue),
                UnityEditorInternal.InternalEditorUtility.layers
            );
            if(EditorGUI.EndChangeCheck()) {
				if((int)fieldValue == -1) {
					option.value = fieldValue;
				}
				else {
					option.value = UnityEditorInternal.InternalEditorUtility.ConcatenatedLayersMaskToLayerMask(fieldValue);
				}
			}
		}
	}
}
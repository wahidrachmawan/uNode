using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace MaxyGames.UNode.Editors.Control {
	class LayerMaskFieldControl : FieldControl<LayerMask> {
		public override void Draw(Rect position, GUIContent label, object value, Type type, Action<object> onChanged, uNodeUtility.EditValueSettings settings) {
			EditorGUI.BeginChangeCheck();
			ValidateValue(ref value);
			var oldValue = (LayerMask)value;
			LayerMask newValue = EditorGUI.MaskField(
				position,
				label,
                UnityEditorInternal.InternalEditorUtility.LayerMaskToConcatenatedLayersMask(oldValue),
				UnityEditorInternal.InternalEditorUtility.layers
			);
			if(EditorGUI.EndChangeCheck()) {
				if((int)newValue == -1) {
					onChanged(newValue);
				}
				else {
					onChanged(UnityEditorInternal.InternalEditorUtility.ConcatenatedLayersMaskToLayerMask(newValue));
				}
			}
		}
	}
}
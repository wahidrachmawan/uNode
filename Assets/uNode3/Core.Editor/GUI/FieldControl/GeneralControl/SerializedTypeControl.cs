using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace MaxyGames.UNode.Editors.Control {
	class SerializedTypeControl : FieldControl<SerializedType> {
		public override void Draw(Rect position, GUIContent label, object value, Type type, Action<object> onChanged, uNodeUtility.EditValueSettings settings) {
			var attributes = settings.attributes;
			ValidateValue(ref value, settings != null ? settings.nullable : false);
			var fieldValue = value as SerializedType;
			uNodeGUIUtility.DrawTypeDrawer(position, fieldValue, label, (type) => {
				onChanged(new SerializedType(type));
			}, ReflectionUtils.GetAttribute<FilterAttribute>(attributes), settings.unityObject);
		}
	}
}
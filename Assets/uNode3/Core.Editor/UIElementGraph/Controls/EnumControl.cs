using System;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace MaxyGames.UNode.Editors.UIControl {
	[ControlField(typeof(Enum))]
	public class EnumControl : ValueControl {
		public EnumControl(ControlConfig config, bool autoLayout = false) : base(config, autoLayout) {
			Init();
		}

		void Init() {
			var actualType = config.type;
			if(actualType is INativeMember) {
				actualType = (actualType as INativeMember).GetNativeMember() as Type ?? config.type;
			}
			EnumField field = new EnumField(config.value as Enum ?? ReflectionUtils.CreateInstance(actualType) as Enum);
			field.RegisterValueChangedCallback((e) => {
				config.OnValueChanged(e.newValue);
				MarkDirtyRepaint();
			});
			Add(field);
		}
	}
}
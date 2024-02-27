using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace MaxyGames.UNode.Editors.UIControl {
	[ControlField(typeof(Quaternion))]
	public class QuaternionControl : ValueControl {
		public QuaternionControl(ControlConfig config, bool autoLayout = false) : base(config, autoLayout) {
			Init();
		}

		void Init() {
			Vector3Field field = new Vector3Field() {
				value = config.value != null ? ((Quaternion)config.value).eulerAngles: new Vector3(),
			};
			field.EnableInClassList("compositeField", false);
			field.style.flexDirection = FlexDirection.Row;
			field.style.flexWrap = Wrap.NoWrap;
			field.RegisterValueChangedCallback((e) => {
				config.OnValueChanged(Quaternion.Euler(e.newValue));
				MarkDirtyRepaint();
			});
			Add(field);
		}
	}
}
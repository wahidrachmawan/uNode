using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace MaxyGames.UNode.Editors.UIControl {
	[ControlField(typeof(Color))]
	public class ColorControl : ValueControl {
		public ColorControl(ControlConfig config, bool autoLayout = false) : base(config, autoLayout) {
			Init();
		}

		void Init() {
			ColorField field = new ColorField() {
				value = config.value != null ? (Color)config.value : new Color(),
			};
			field.RegisterValueChangedCallback((e) => {
				config.OnValueChanged(e.newValue);
				MarkDirtyRepaint();
			});
			Add(field);
		}
	}
}
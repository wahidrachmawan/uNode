using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace MaxyGames.UNode.Editors.UIControl {
	[ControlField(typeof(RectInt))]
	public class RectIntControl : ValueControl {
		public RectIntControl(ControlConfig config, bool autoLayout = false) : base(config, autoLayout) {
			Init();
		}

		void Init() {
			RectIntField field = new RectIntField() {
				value = config.value != null ? (RectInt)config.value : new RectInt(),
			};
			field.RegisterValueChangedCallback((e) => {
				config.OnValueChanged(e.newValue);
				MarkDirtyRepaint();
			});
			Add(field);
		}
	}
}
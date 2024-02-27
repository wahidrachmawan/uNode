using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace MaxyGames.UNode.Editors.UIControl {
	[ControlField(typeof(Rect))]
	public class RectControl : ValueControl {
		public RectControl(ControlConfig config, bool autoLayout = false) : base(config, autoLayout) {
			Init();
		}

		void Init() {
			RectField field = new RectField() {
				value = config.value != null ? (Rect)config.value : new Rect(),
			};
			field.RegisterValueChangedCallback((e) => {
				config.OnValueChanged(e.newValue);
				MarkDirtyRepaint();
			});
			Add(field);
		}
	}
}
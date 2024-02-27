using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace MaxyGames.UNode.Editors.UIControl {
	[ControlField(typeof(float))]
	public class FloatControl : ValueControl {
		public FloatControl(ControlConfig config, bool autoLayout = false) : base(config, autoLayout) {
			Init();
		}

		void Init() {
			FloatField field = new FloatField() {
				value = config.value != null ? config.value.ConvertTo<float>() : new float(),
			};
			field.isDelayed = true;
			field.RegisterValueChangedCallback((e) => {
				config.OnValueChanged(e.newValue);
				MarkDirtyRepaint();
			});
			Add(field);
		}
	}
}
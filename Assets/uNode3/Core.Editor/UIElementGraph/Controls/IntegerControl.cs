using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace MaxyGames.UNode.Editors.UIControl {
	[ControlField(typeof(int))]
	public class IntegerControl : ValueControl {
		public IntegerControl(ControlConfig config, bool autoLayout = false) : base(config, autoLayout) {
			Init();
		}

		void Init() {
			IntegerField field = new IntegerField() {
				value = config.value.ConvertTo<int>(),
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
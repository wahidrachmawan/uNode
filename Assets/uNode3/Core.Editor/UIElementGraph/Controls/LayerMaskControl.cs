using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace MaxyGames.UNode.Editors.UIControl {
	[ControlField(typeof(LayerMask))]
	public class LayerMaskControl : ValueControl {
		public LayerMaskControl(ControlConfig config, bool autoLayout = false) : base(config, autoLayout) {
			Init();
		}

		void Init() {
			LayerMaskField field = new LayerMaskField() {
				value = config.value is LayerMask ? (LayerMask)config.value : default(LayerMask),
			};
			field.EnableInClassList("compositeField", false);

			field.RegisterValueChangedCallback((e) => {
				config.OnValueChanged((LayerMask)e.newValue);
				MarkDirtyRepaint();
			});
			Add(field);
		}
	}
}
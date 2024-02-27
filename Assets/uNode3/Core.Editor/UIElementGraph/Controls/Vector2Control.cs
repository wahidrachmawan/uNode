using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace MaxyGames.UNode.Editors.UIControl {
	[ControlField(typeof(Vector2))]
	public class Vector2Control : ValueControl {
		public Vector2Control(ControlConfig config, bool autoLayout = false) : base(config, autoLayout) {
			Init();
		}

		void Init() {
			Vector2Field field = new Vector2Field() {
				value = config.value != null ? (Vector2)config.value : new Vector2(),
			};
			field.EnableInClassList("compositeField", false);
			field.style.flexDirection = FlexDirection.Row;
			field.style.flexWrap = Wrap.NoWrap;

			field.RegisterValueChangedCallback((e) => {
				config.OnValueChanged(e.newValue);
				MarkDirtyRepaint();
			});
			Add(field);
		}
	}
}
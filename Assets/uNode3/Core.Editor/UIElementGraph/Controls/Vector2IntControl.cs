using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace MaxyGames.UNode.Editors.UIControl {
	[ControlField(typeof(Vector2Int))]
	public class Vector2IntControl : ValueControl {
		public Vector2IntControl(ControlConfig config, bool autoLayout = false) : base(config, autoLayout) {
			Init();
		}

		void Init() {
			Vector2IntField field = new Vector2IntField() {
				value = config.value != null ? (Vector2Int)config.value : new Vector2Int(),
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
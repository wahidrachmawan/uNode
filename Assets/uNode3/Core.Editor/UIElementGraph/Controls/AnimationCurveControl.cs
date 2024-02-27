using UnityEditor.UIElements;
using UnityEngine.UIElements;
using UnityEngine;

namespace MaxyGames.UNode.Editors.UIControl {
	[ControlField(typeof(AnimationCurve))]
	public class AnimationCurveControl : ValueControl {
		public AnimationCurveControl(ControlConfig config, bool autoLayout = false) : base(config, autoLayout) {
			Init();
		}

		void Init() {
			CurveField field = new CurveField() {
				value = config.value != null ? (AnimationCurve)config.value : new AnimationCurve(),
			};
			field.RegisterCallback<ChangeEvent<AnimationCurve>>(evt => {
				config.OnValueChanged(evt.newValue);
				MarkDirtyRepaint();
			});
			Add(field);
		}
	}
}
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using UnityEngine;

namespace MaxyGames.UNode.Editors.UIControl {
	[ControlField(typeof(AnimationCurve))]
	public class AnimationCurveControl : ValueControl {
		static AnimationCurve buffer { get => uNodeEditorUtility.CopiedValue<AnimationCurve>.value; set => uNodeEditorUtility.CopiedValue<AnimationCurve>.value = value; }

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
			field.AddManipulator(new ContextualMenuManipulator(evt => {
				evt.menu.AppendAction("Copy", act => {
					buffer = SerializerUtility.Duplicate(field.value);
				}, DropdownMenuAction.AlwaysEnabled);
				evt.menu.AppendAction("Paste", act => {
					if(buffer == null) return;

					var fieldValue = new AnimationCurve(buffer.keys);
					fieldValue.preWrapMode = buffer.preWrapMode;
					fieldValue.postWrapMode = buffer.postWrapMode;
					field.value = fieldValue;
				}, DropdownMenuAction.AlwaysEnabled);
			}));
			Add(field);
		}
	}
}
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

	[ControlField(typeof(uint))]
	public class UIntegerControl : ValueControl {
		public UIntegerControl(ControlConfig config, bool autoLayout = false) : base(config, autoLayout) {
			Init();
		}

		void Init() {
			UIntField field = new UIntField() {
				value = config.value.ConvertTo<uint>(),
			};
			field.isDelayed = true;
			field.RegisterValueChangedCallback((e) => {
				config.OnValueChanged(e.newValue);
				MarkDirtyRepaint();
			});
			Add(field);
		}

		class UIntField : TextField {
			private uint _value;

			public new uint value {
				get => _value;
				set {
					_value = value;
					SetValueWithoutNotify(_value.ToString());
				}
			}

			public UIntField(string label = null) : base(label) {
				isDelayed = true;

				this.RegisterValueChangedCallback(evt => {
					if(uint.TryParse(evt.newValue, out uint parsed)) {
						_value = parsed;
					}
					else {
						// revert if invalid
						SetValueWithoutNotify(_value.ToString());
					}
				});
			}
		}
	}
}
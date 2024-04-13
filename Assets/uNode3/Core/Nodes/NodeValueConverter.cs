using System;
using UnityEngine;

namespace MaxyGames.UNode.Nodes {
	public class NodeValueConverter : ValueNode {
		public SerializedType type = new SerializedType(typeof(object));
		public ValueInput input { get; set; }

		public enum ConvertKind {
			As,
			Convert,
		}
		public ConvertKind kind;

		protected override void OnRegister() {
			base.OnRegister();
			input = ValueInput(nameof(input), typeof(object));
		}

		public override Type ReturnType() {
			return type.type;
		}

		public override object GetValue(Flow flow) {
			var value = input.GetValue(flow);
			Type t = type.type;
			if(value != null) {
				if(value.GetType() == t)
					return value;
				if(t == typeof(string)) {
					return value.ToString();
				} else if(t == typeof(GameObject)) {
					if(value is Component component) {
						return component.gameObject;
					}
				} else if(t.IsCastableTo(typeof(Component))) {
					if(value is GameObject gameObject) {
						if(t is RuntimeType) {
							return gameObject.GetGeneratedComponent(t as RuntimeType);
						}
						return gameObject.GetComponent(t);
					} else if(value is Component component) {
						if(t is RuntimeType) {
							return component.GetGeneratedComponent(t as RuntimeType);
						}
						return component.GetComponent(t);
					}
				}
			}
			if(kind == ConvertKind.Convert || t is RuntimeType || t.IsValueType) {
				value = Operator.Convert(value, t);
			} else {
				value = Operator.TypeAs(value, t);
			}
			return value;
		}

		protected override string GenerateValueCode() {
			if(input.isAssigned && type.type != null) {
				System.Type t = type.type;
				System.Type targetType = input.ValueType;
				if(t != null && targetType != null) {
					if(!targetType.IsCastableTo(t) && !t.IsCastableTo(targetType)) {
						if(t == typeof(string)) {
							return CG.Value(input).CGInvoke(nameof(object.ToString));
						}
						else if(t == typeof(GameObject)) {
							if(targetType.IsCastableTo(typeof(Component))) {
								return CG.Value(input).CGAccess(nameof(Component.gameObject));
							}
						}
						else if(t.IsCastableTo(typeof(Component))) {
							if(targetType.IsCastableTo(typeof(Component)) || targetType == typeof(GameObject)) {
								if(t == typeof(Transform)) {
									return CG.Value(input).CGAccess(nameof(Component.transform));
								}
								else {
									if(ReflectionUtils.IsNativeType(t) == false) {
										if(CG.generatePureScript) {
											return CG.Value(input).CGInvoke(nameof(uNodeHelper.GetGeneratedComponent), new[] { t });
										}
										else {
											return CG.Value(input).CGInvoke(nameof(uNodeHelper.GetGeneratedComponent), new[] { CG.GetUniqueNameForType(t as RuntimeType) });
										}
									}
									return CG.Value(input).CGInvoke(nameof(Component.GetComponent), new System.Type[] { t }, null);
								}
							}
						}
					}
				}
				else if(t == null) {
					return CG.Convert(input.CGValue(), CG.Type(type.type));
				}
				if(kind == ConvertKind.Convert || t.IsValueType) {
					return CG.Convert(input, t);
				}
				return CG.As(input, t);
			}
			throw new System.Exception();
		}

		public override bool CanGetValue() {
			return true;
		}

		public override bool CanSetValue() {
			return false;
		}

		public override Type GetNodeIcon() {
			return typeof(TypeIcons.RefreshIcon);
		}

		public override string GetTitle() {
			if(kind == ConvertKind.As && type.type != null && !type.type.IsValueType) {
				return "AS";
			}
			return "Convert";
		}

		public override string GetRichName() {
			return $"({type.typeName})" + input.GetRichName();
		}

		public override void CheckError(ErrorAnalyzer analizer) {
			analizer.CheckPort(input);
			analizer.CheckValue(type, nameof(type), this);
			if(input.hasValidConnections && type.isAssigned) {
				var inputType = input.ValueType;
				var targetType = type.type;
				if(inputType.IsCastableTo(targetType, true) == false && targetType.IsCastableTo(inputType, true) == false) {
					if(targetType == typeof(string)) {

					}
					else if((inputType == typeof(GameObject) || inputType.IsCastableTo(typeof(Component))) && (targetType == typeof(GameObject) || targetType.IsCastableTo(typeof(Component)))) {

					}
					else {
						analizer.RegisterError(this, $"Cannot convert: {inputType.PrettyName(true)} to {type.prettyName}");
					}
				}
			}
		}
	}
}
using System;
using UnityEngine;

namespace MaxyGames.UNode.Nodes {
    [NodeMenu("Data", "Convert", typeof(object), inputs = new[] { typeof(object) })]
	[Description("Convert value to other type")]
	public class NodeConvert : ValueNode {
		[Filter(AllowInterface = true, OnlyGetType = true, ArrayManipulator = true, DisplayRuntimeType = true)]
		public SerializedType type = SerializedType.None;
		public bool useASWhenPossible = true;
		public bool compactDisplay = true;

		public ValueInput target { get; set; }

		protected override void OnRegister() {
			base.OnRegister();
			target = ValueInput(nameof(target), typeof(object)).SetName("Value");
			output.SetAutoType(() => type.isFilled == false);
		}

		protected override System.Type ReturnType() {
			if(type.isFilled) {
				try {
					System.Type t = type.type;
					if(!object.ReferenceEquals(t, null)) {
						return t;
					}
				}
				catch { }
			}
			return typeof(object);
		}

		public override object GetValue(Flow flow) {
			var value = target.GetValue(flow);
			System.Type t = type.nativeType;
			if(type.isFilled == false) {
				t = output.type;
			}
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
							if(ReflectionUtils.IsNativeType(t)) {
								var nativeType = ReflectionUtils.GetNativeType(t);
								if(nativeType != null) {
									return gameObject.GetComponent(nativeType);
								}
								else {
									throw new Exception($"The graph: {t} need to be compiled first.");
								}
							}
							return gameObject.GetGeneratedComponent(t as RuntimeType);
						}
						return gameObject.GetComponent(t);
					} else if(value is Component component) {
						if(t is RuntimeType) {
							if(ReflectionUtils.IsNativeType(t)) {
								var nativeType = ReflectionUtils.GetNativeType(t);
								if(nativeType != null) {
									return component.GetComponent(nativeType);
								}
								else {
									throw new Exception($"The graph: {t} need to be compiled first.");
								}
							}
							return component.GetGeneratedComponent(t as RuntimeType);
						}
						return component.GetComponent(t);
					}
				}
			}
			if(!useASWhenPossible || t is RuntimeType || t.IsValueType) {
				value = Operator.Convert(value, t);
			} else {
				value = Operator.TypeAs(value, t);
			}
			return value;
		}

		protected override string GenerateValueCode() {
			if(target.isAssigned && type.isAssigned) {
				System.Type t = type.type;
				if(type.isFilled == false) {
					t = output.type;
				}
				System.Type targetType = target.ValueType;
				if(t != null && targetType != null) {
					if(!targetType.IsCastableTo(t) && !t.IsCastableTo(targetType)) {
						if(t == typeof(string)) {
							return CG.Value(target).CGInvoke(nameof(object.ToString));
						}
						else if(t == typeof(GameObject)) {
							if(targetType.IsCastableTo(typeof(Component))) {
								return CG.Value(target).CGAccess(nameof(Component.gameObject));
							}
						}
						else if(t.IsCastableTo(typeof(Component))) {
							if(targetType.IsCastableTo(typeof(Component)) || targetType == typeof(GameObject)) {
								if(t == typeof(Transform)) {
									return CG.Value(target).CGAccess(nameof(Component.transform));
								}
								else {
									if(ReflectionUtils.IsNativeType(t) == false) {
										CG.RegisterUsingNamespace("MaxyGames.UNode");//Register namespace to make sure Extensions work for GameObject or Component target type.
										if(CG.generatePureScript) {
											return CG.Value(target).CGInvoke(nameof(uNodeHelper.GetGeneratedComponent), new[] { t });
										}
										else {
											return CG.Value(target).CGInvoke(nameof(uNodeHelper.GetGeneratedComponent), new[] { CG.GetUniqueNameForType(t as RuntimeType) });
										}
									}
									return CG.Value(target).CGInvoke(nameof(Component.GetComponent), new System.Type[] { t }, null);
								}
							}
						}
					}
				}
				else if(t == null) {
					return CG.Convert(target.CGValue(), CG.Type(type));
				}
				if(!useASWhenPossible || t.IsValueType) {
					return CG.Convert(target, t);
				}
				return CG.As(target, t);
			}
			throw new System.Exception("Target or Type is unassigned.");
		}

		public override string GetTitle() {
			return "Convert";
		}

		public override string GetRichName() {
			if(target.isAssigned && type.type != null) {
				System.Type t = type.type;
				System.Type targetType = target.ValueType;
				if(t != null && targetType != null) {
					if(!targetType.IsCastableTo(t) && !t.IsCastableTo(targetType)) {
						if(t == typeof(string)) {
							return target.GetRichName().CGInvoke(nameof(ToString));
						}
						else if(t == typeof(GameObject)) {
							if(targetType.IsCastableTo(typeof(Component))) {
								return target.GetRichName().CGAccess(nameof(Component.gameObject));
							}
						}
						else if(t.IsCastableTo(typeof(Component))) {
							if(targetType.IsCastableTo(typeof(Component)) || targetType == typeof(GameObject)) {
								if(t == typeof(Transform)) {
									return target.GetRichName().CGAccess(nameof(Component.transform));
								}
								return target.GetRichName().CGAccess(nameof(Component.GetComponent)) + $"<{TypeName}>()";
							}
						}
					}
				}
				else if(t == null) {
					return CG.Convert(target.CGValue(), CG.Type(type.type));
				}
				if(useASWhenPossible == false || t.IsValueType) {
					return $"({TypeName})" + target.GetRichName();
				}
				return (target.GetRichName() + " as " + TypeName).Wrap();
			}
			return $"({type.GetRichName()})" + target.GetRichName();
		}

		private string TypeName => type.isFilled ? type.GetRichName() : output.type.PrettyName();

		public override string GetRichTitle() {
			return $"Convert to: {TypeName}";
		}

		public override void CheckError(ErrorAnalyzer analizer) {
			analizer.CheckPort(target);
			//analizer.CheckValue(type, nameof(type), this);
			if(target.isAssigned && type.isAssigned) {
				var inputType = target.ValueType;
				var targetType = type.type;
				if(inputType.IsCastableTo(targetType, true) == false && targetType.IsCastableTo(inputType, true) == false) {
					if(targetType == typeof(string)) {

					}
					else if((inputType == typeof(GameObject) || inputType.IsCastableTo(typeof(Component))) && targetType.IsCastableTo(typeof(Component))) {

					}
					else {
						analizer.RegisterError(this, $"Cannot convert: {inputType.PrettyName(true)} to {type.prettyName}");
					}
				}
			}
		}
	}
}
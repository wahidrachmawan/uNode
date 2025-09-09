using UnityEngine;
using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

namespace MaxyGames.UNode.Nodes {
	public class ComparisonNode : ValueNode {
		public ComparisonType operatorKind = ComparisonType.Equal;
		[Header("Editor")]
		public SerializedType inputType = typeof(object);

		public ValueInput inputA { get; set; }
		public ValueInput inputB { get; set; }

		protected override void OnRegister() {
			base.OnRegister();
			inputA = ValueInput(nameof(inputA), GetInputType);
			inputB = ValueInput(nameof(inputB), GetInputType);

			void OnPortChanged(UPort port) {
				var a = port as ValueInput;
				var b = inputA == port ? inputB : inputA;
				if(b.UseDefaultValue) {
					var aType = a.ValueType;
					var bType = b.ValueType;
					if(aType != bType) {
						if(bType == null || b.TargetType.IsTargetingValue()) {
							if(bType != null && bType.IsCastableTo(aType)) {
								b.AssignToDefault(MemberData.CreateFromValue(b.DefaultValue.Get(null, aType), aType));
							}
							else {
								b.AssignToDefault(MemberData.CreateFromValue(null, aType));
							}
						}
					}
				}
			}

			inputA.SetOnChangedCallback(OnPortChanged);
			inputB.SetOnChangedCallback(OnPortChanged);
		}

		private Type GetInputType() {
			return inputType;
		}

		protected override System.Type ReturnType() {
			return typeof(bool);
		}

		public override object GetValue(Flow flow) {
			return uNodeHelper.OperatorComparison(inputA.GetValue(flow), inputB.GetValue(flow), operatorKind);
		}

		public override string GetTitle() {
			return operatorKind.ToString();
		}

		public override string GetRichName() {
			string separator = null;
			switch(operatorKind) {
				case ComparisonType.Equal:
					separator = " == ";
					break;
				case ComparisonType.GreaterThan:
					separator = " > ";
					break;
				case ComparisonType.GreaterThanOrEqual:
					separator = " >= ";
					break;
				case ComparisonType.LessThan:
					separator = " < ";
					break;
				case ComparisonType.LessThanOrEqual:
					separator = " <= ";
					break;
				case ComparisonType.NotEqual:
					separator = " != ";
					break;
			}
			return inputA.GetRichName() + separator + inputB.GetRichName();
		}

		public override Type GetNodeIcon() {
			switch(operatorKind) {
				case ComparisonType.Equal:
					return typeof(TypeIcons.Equal);
				case ComparisonType.NotEqual:
					return typeof(TypeIcons.NotEqual);
				case ComparisonType.LessThan:
					return typeof(TypeIcons.LessThan);
				case ComparisonType.LessThanOrEqual:
					return typeof(TypeIcons.LessThanOrEqual);
				case ComparisonType.GreaterThan:
					return typeof(TypeIcons.GreaterThan);
				case ComparisonType.GreaterThanOrEqual:
					return typeof(TypeIcons.GreaterThanOrEqual);
			}
			return typeof(TypeIcons.CompareIcon);
		}

		public override void CheckError(ErrorAnalyzer analizer) {
			base.CheckError(analizer);
			if(!analizer.CheckPort(inputA, inputB)) {
				try {
					var type1 = inputA.ValueType;
					var type2 = inputB.ValueType;
					if(type1 != null && type2 != null) {
						switch(operatorKind) {
							case ComparisonType.Equal:
								if(!Operator.IsValidEquality(type1, type2)) {
									analizer.RegisterError(this, $"Operator '==' cannot be applied to operands of type '{type1}' and '{type2}'");
								}
								break;
							case ComparisonType.NotEqual:
								if(!Operator.IsValidInequality(type1, type2)) {
									analizer.RegisterError(this, $"Operator '!=' cannot be applied to operands of type '{type1}' and '{type2}'");
								}
								break;
							default:
								uNodeHelper.OperatorComparison(
									ReflectionUtils.CreateInstance(type1),
									ReflectionUtils.CreateInstance(type2), operatorKind);
								break;
						}
					}
				}
				catch(System.Exception ex) {
					if(ex is NullReferenceException)
						return;
					analizer.RegisterError(this, ex.Message);
				}
			}
		}

		protected override string GenerateValueCode() {
			if(inputA.isAssigned && inputB.isAssigned) {
				return CG.Compare(
					CG.Value(inputA),
					CG.Value(inputB),
					operatorKind).AddFirst("(").Add(")");
			}
			throw new Exception("The target is unassigned");
		}
	}
}
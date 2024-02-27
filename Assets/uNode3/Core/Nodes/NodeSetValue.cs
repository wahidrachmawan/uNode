using UnityEngine;
using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

namespace MaxyGames.UNode.Nodes {
	[NodeMenu("Flow", "SetValue")]
	public class NodeSetValue : FlowNode {
		public SetType setType;
		public ValueInput target { get; set; }
		public ValueInput value { get; set; }

		protected override void OnRegister() {
			base.OnRegister();
			target = ValueInput(nameof(target), typeof(object));
			target.canSetValue = () => true;
			target.filter = new FilterAttribute() {
				SetMember = true,
			};
			value = ValueInput(nameof(value), () => target.ValueType);
		}

		#region Runtime
		protected override void OnExecuted(Flow flow) {
			if(target.isAssigned && value.isAssigned) {
				object obj = value.GetValue(flow);
				switch(setType) {
					case SetType.Change:
						target.SetValue(flow, obj);
						break;
					case SetType.Add:
						if(obj is Delegate) {
							object targetVal = target.GetValue(flow);
							if(targetVal is MemberData.Event) {
								MemberData.Event e = targetVal as MemberData.Event;
								if(e.eventInfo != null) {
									e.eventInfo.AddEventHandler(e.instance, ReflectionUtils.ConvertDelegate(obj as Delegate, e.eventInfo.EventHandlerType));
								}
							} else if(targetVal == null) {
								target.SetValue(flow, obj);
							} else {
								target.SetValue(flow, Operator.Add(targetVal, obj));
							}
						} else {
							target.SetValue(flow, Operator.Add(target.GetValue(flow), obj));
						}
						break;
					case SetType.Divide:
						target.SetValue(flow, Operator.Divide(target.GetValue(flow), obj));
						break;
					case SetType.Subtract:
						if(obj is Delegate) {
							object targetVal = target.GetValue(flow);
							if(targetVal is MemberData.Event) {
								MemberData.Event e = targetVal as MemberData.Event;
								if(e.eventInfo != null) {
									e.eventInfo.RemoveEventHandler(e.instance, ReflectionUtils.ConvertDelegate(obj as Delegate, e.eventInfo.EventHandlerType));
								}
							} else if(targetVal != null) {
								target.SetValue(flow, Operator.Add(targetVal, obj));
							}
						} else {
							target.SetValue(flow, Operator.Subtract(target.GetValue(flow), obj));
						}
						break;
					case SetType.Multiply:
						target.SetValue(flow, Operator.Multiply(target.GetValue(flow), obj));
						break;
					case SetType.Modulo:
						target.SetValue(flow, Operator.Modulo(target.GetValue(flow), obj));
						break;
				}
			} else {
				throw new Exception("Target is unassigned.");
			}
		}
		#endregion

		protected override string GenerateFlowCode() {
			if(target.isAssigned && value.isAssigned) {
				return CG.Flow(
					CG.Set(target, value, setType, target.type, value.type),
					CG.FlowFinish(enter, exit)
				);
			}
			return CG.FlowFinish(enter, exit);
		}

		public override string GetTitle() {
			switch(setType) {
				case SetType.Change:
					return "Set Value";
				case SetType.Add:
					return "Add Value";
				case SetType.Divide:
					return "Divide Value";
				case SetType.Subtract:
					return "Subtract Value";
				case SetType.Multiply:
					return "Multiply Value";
				case SetType.Modulo:
					return "Mod Value";
			}
			return base.GetTitle();
		}

		public override string GetRichName() {
			var setCode = "=";
			switch(setType) {
				case SetType.Add:
					setCode = "+=";
					break;
				case SetType.Divide:
					setCode = "/=";
					break;
				case SetType.Subtract:
					setCode = "-=";
					break;
				case SetType.Multiply:
					setCode = "*=";
					break;
				case SetType.Modulo:
					setCode = "%=";
					break;
			}
			return $"{target.GetRichName()} {setCode} {value.GetRichName()}";
		}

		public override Type GetNodeIcon() {
			switch(setType) {
				case SetType.Change:
					return typeof(TypeIcons.SetValueIcon);
				case SetType.Add:
					return typeof(TypeIcons.SetAddIcon);
				case SetType.Divide:
					return typeof(TypeIcons.SetDivideIcon);
				case SetType.Subtract:
					return typeof(TypeIcons.SetSubtractIcon);
				case SetType.Multiply:
					return typeof(TypeIcons.SetMultiplyIcon);
				case SetType.Modulo:
					return typeof(TypeIcons.SetModuloIcon);
			}
			return base.GetNodeIcon();
		}
	}
}
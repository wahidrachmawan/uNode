using System;
using UnityEngine;

namespace MaxyGames.UNode.Nodes {
    [NodeMenu("Data", "IS", typeof(bool), inputs = new[] { typeof(object) })]
	public class ISNode : ValueNode {
		[Hide, FieldDrawer("Type"), Filter(OnlyGetType = true, DisplayRuntimeType = true, ArrayManipulator = true)]
		public SerializedType type = typeof(object);
		[System.NonSerialized]
		public ValueInput target;
		[System.NonSerialized]
		public ValueOutput value;

		protected override void OnRegister() {
			base.OnRegister();
			target = ValueInput(nameof(target), typeof(object));
			value = ValueOutput(nameof(value), () => type.type);
			value.AssignGetCallback(flow => target.GetValue(flow, type.type));
		}

		public override System.Type ReturnType() {
			return typeof(bool);
		}

		public override object GetValue(Flow flow) {
			return Operator.TypeIs(target.GetValue(flow), type.type);
		}

		public override void OnGeneratorInitialize() {
			base.OnGeneratorInitialize();
			CG.RegisterPort(value, () => CG.GeneratePort(target).CGConvert(value.type));
		}

		protected override string GenerateValueCode() {
			if(target.isAssigned && type.isAssigned) {
				return CG.Is(target, type.type);
			}
			throw new System.Exception("Target or Type is unassigned.");
		}

		public override string GetTitle() {
			return "IS";
		}

		public override string GetRichName() {
			return target.GetRichName() + uNodeUtility.WrapTextWithKeywordColor(" is ") + target.GetRichName();
		}

		public override System.Type GetNodeIcon() {
			if(type.isFilled) {
				return type.type;
			}
			return typeof(bool);
		}

		public override void CheckError(ErrorAnalyzer analizer) {
			analizer.CheckPort(target);
			analizer.CheckValue(type, nameof(type), this);
		}
	}
}
using System;
using MaxyGames.UNode.Nodes;

namespace MaxyGames.UNode.Editors.Commands {
	public class AddArithmeticItem : CreateNodeCommand<MultiArithmeticNode> {
		public override string name {
			get {
				return "Add (+)";
			}
		}

		public override string category {
			get {
				return "Math";
			}
		}

		public override System.Type icon => typeof(TypeIcons.AddIcon);

		protected override void OnNodeCreated(MultiArithmeticNode node) {
			node.EnsureRegistered();
			node.operatorKind = ArithmeticType.Add;
			if(filter != null) {
				var type = filter.GetActualType();
				if(type != typeof(object)) {
					node.inputs[0].port.AssignToDefault(MemberData.Default(type));
					node.inputs[0].type = type;
					node.inputs[1].port.AssignToDefault(MemberData.Default(type));
					node.inputs[1].type = type;
					return;
				}
			}
			node.inputs[0].port.AssignToDefault(MemberData.CreateFromValue(0f));
			node.inputs[1].port.AssignToDefault(MemberData.CreateFromValue(0f));
		}

		public override bool IsValid() {
			if(filter == null) {
				return true;
			}
			var type = filter.GetActualType();
			if(type == null)
				return true;
			return type == typeof(object) || type.IsPrimitive && type != typeof(bool) && type != typeof(char);
		}
	}

	public class DivideArithmeticItem : CreateNodeCommand<MultiArithmeticNode> {
		public override string name {
			get {
				return "Divide (/)";
			}
		}

		public override string category {
			get {
				return "Math";
			}
		}

		public override System.Type icon => typeof(TypeIcons.DivideIcon);

		protected override void OnNodeCreated(MultiArithmeticNode node) {
			node.EnsureRegistered();
			node.operatorKind = ArithmeticType.Divide;
			if(filter != null) {
				var type = filter.GetActualType();
				if(type != typeof(object)) {
					node.inputs[0].port.AssignToDefault(MemberData.Default(type));
					node.inputs[0].type = type;
					node.inputs[1].port.AssignToDefault(MemberData.Default(type));
					node.inputs[1].type = type;
					return;
				}
			}
			node.inputs[0].port.AssignToDefault(MemberData.CreateFromValue(1f));
			node.inputs[1].port.AssignToDefault(MemberData.CreateFromValue(1f));
		}


		public override bool IsValid() {
			if(filter == null) {
				return true;
			}
			var type = filter.GetActualType();
			if(type == null)
				return true;
			return type == typeof(object) || type.IsPrimitive && type != typeof(bool) && type != typeof(char);
		}
	}

	public class ModuloArithmeticItem : CreateNodeCommand<MultiArithmeticNode> {
		public override string name {
			get {
				return "Modulo (%)";
			}
		}

		public override string category {
			get {
				return "Math";
			}
		}

		public override System.Type icon => typeof(TypeIcons.ModuloIcon);

		protected override void OnNodeCreated(MultiArithmeticNode node) {
			node.EnsureRegistered();
			node.operatorKind = ArithmeticType.Modulo;
			if(filter != null) {
				var type = filter.GetActualType();
				if(type != typeof(object)) {
					node.inputs[0].port.AssignToDefault(MemberData.Default(type));
					node.inputs[0].type = type;
					node.inputs[1].port.AssignToDefault(MemberData.Default(type));
					node.inputs[1].type = type;
					return;
				}
			}
			node.inputs[0].port.AssignToDefault(MemberData.CreateFromValue(1f));
			node.inputs[1].port.AssignToDefault(MemberData.CreateFromValue(1f));
		}

		public override bool IsValid() {
			if(filter == null) {
				return true;
			}
			var type = filter.GetActualType();
			if(type == null)
				return true;
			return type == typeof(object) || type.IsPrimitive && type != typeof(bool) && type != typeof(char);
		}
	}

	public class MultiplyArithmeticItem : CreateNodeCommand<MultiArithmeticNode> {
		public override string name {
			get {
				return "Multiply (*)";
			}
		}

		public override string category {
			get {
				return "Math";
			}
		}

		public override System.Type icon => typeof(TypeIcons.MultiplyIcon);

		protected override void OnNodeCreated(MultiArithmeticNode node) {
			node.EnsureRegistered();
			node.operatorKind = ArithmeticType.Multiply;
			if(filter != null) {
				var type = filter.GetActualType();
				if(type != typeof(object)) {
					node.inputs[0].port.AssignToDefault(MemberData.Default(type));
					node.inputs[0].type = type;
					node.inputs[1].port.AssignToDefault(MemberData.Default(type));
					node.inputs[1].type = type;
					return;
				}
			}
			node.inputs[0].port.AssignToDefault(MemberData.CreateFromValue(1f));
			node.inputs[1].port.AssignToDefault(MemberData.CreateFromValue(1f));
		}

		public override bool IsValid() {
			if(filter == null) {
				return true;
			}
			var type = filter.GetActualType();
			if(type == null)
				return true;
			return type == typeof(object) || type.IsPrimitive && type != typeof(bool) && type != typeof(char);
		}
	}

	public class SubtractArithmeticItem : CreateNodeCommand<MultiArithmeticNode> {
		public override string name {
			get {
				return "Subtract (-)";
			}
		}

		public override string category {
			get {
				return "Math";
			}
		}

		public override System.Type icon => typeof(TypeIcons.SubtractIcon);

		protected override void OnNodeCreated(MultiArithmeticNode node) {
			node.EnsureRegistered();
			node.operatorKind = ArithmeticType.Subtract;
			if(filter != null) {
				var type = filter.GetActualType();
				if(type != typeof(object)) {
					node.inputs[0].port.AssignToDefault(MemberData.Default(type));
					node.inputs[0].type = type;
					node.inputs[1].port.AssignToDefault(MemberData.Default(type));
					node.inputs[1].type = type;
					return;
				}
			}
			node.inputs[0].port.AssignToDefault(MemberData.CreateFromValue(1f));
			node.inputs[1].port.AssignToDefault(MemberData.CreateFromValue(1f));
		}

		public override bool IsValid() {
			if(filter == null) {
				return true;
			}
			var type = filter.GetActualType();
			if(type == null)
				return true;
			return type == typeof(object) || type.IsPrimitive && type != typeof(bool) && type != typeof(char);
		}
	}
}
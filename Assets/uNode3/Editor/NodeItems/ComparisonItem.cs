using MaxyGames.UNode.Nodes;

namespace MaxyGames.UNode.Editors.Commands {
	public class EqualComparisonItem : CreateNodeCommand<ComparisonNode> {
		public override string name {
			get {
				return "Equal (==)";
			}
		}

		public override string category {
			get {
				return "Compare";
			}
		}

		public override System.Type icon => typeof(TypeIcons.Equal);

		protected override void OnNodeCreated(ComparisonNode node) {
			node.operatorKind = ComparisonType.Equal;
		}

		public override bool IsValid() {
			if(filter == null) {
				return true;
			}
			var type = filter.GetActualType();
			if(type == null)
				return true;
			return type == typeof(object) || type == typeof(bool) || type == typeof(int) || type == typeof(float) || type == typeof(long) || type == typeof(double);
		}
	}

	public class NotEqualComparisonItem : CreateNodeCommand<ComparisonNode> {
		public override string name {
			get {
				return "NotEqual (!=)";
			}
		}

		public override string category {
			get {
				return "Compare";
			}
		}

		public override System.Type icon => typeof(TypeIcons.NotEqual);

		protected override void OnNodeCreated(ComparisonNode node) {
			node.operatorKind = ComparisonType.NotEqual;
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

	public class GreaterThanComparisonItem : CreateNodeCommand<ComparisonNode> {
		public override string name {
			get {
				return "GreaterThan (>)";
			}
		}

		public override string category {
			get {
				return "Compare";
			}
		}

		public override System.Type icon => typeof(TypeIcons.GreaterThan);

		protected override void OnNodeCreated(ComparisonNode node) {
			node.operatorKind = ComparisonType.GreaterThan;
			node.inputType = typeof(float);
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

	public class GreaterThanOrEqualComparisonItem : CreateNodeCommand<ComparisonNode> {
		public override string name {
			get {
				return "GreaterThanOrEqual (>=)";
			}
		}

		public override string category {
			get {
				return "Compare";
			}
		}

		public override System.Type icon => typeof(TypeIcons.GreaterThanOrEqual);

		protected override void OnNodeCreated(ComparisonNode node) {
			node.operatorKind = ComparisonType.GreaterThanOrEqual;
			node.inputType = typeof(float);
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

	public class LessThanComparisonItem : CreateNodeCommand<ComparisonNode> {
		public override string name {
			get {
				return "LessThan (<)";
			}
		}

		public override string category {
			get {
				return "Compare";
			}
		}

		public override System.Type icon => typeof(TypeIcons.LessThan);

		protected override void OnNodeCreated(ComparisonNode node) {
			node.operatorKind = ComparisonType.LessThan;
			node.inputType = typeof(float);
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

	public class LessThanOrEqualComparisonItem : CreateNodeCommand<ComparisonNode> {
		public override string name {
			get {
				return "LessThanOrEqual (<=)";
			}
		}

		public override string category {
			get {
				return "Compare";
			}
		}

		public override System.Type icon => typeof(TypeIcons.LessThanOrEqual);

		protected override void OnNodeCreated(ComparisonNode node) {
			node.operatorKind = ComparisonType.LessThanOrEqual;
			node.inputType = typeof(float);
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
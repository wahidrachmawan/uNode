using System;

namespace MaxyGames.UNode.Editors {
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
	public class ControlFieldAttribute : Attribute {
		public Type type;
		public bool inherit = true;
		public int order;

		public Type classType;

		public ControlFieldAttribute() {

		}

		public ControlFieldAttribute(Type type, bool inherit = true, int order = 0) {
			this.type = type;
			this.inherit = inherit;
			this.order = order;
		}
	}
}
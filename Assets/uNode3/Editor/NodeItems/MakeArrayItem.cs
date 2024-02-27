using System;
using MaxyGames.UNode.Nodes;

namespace MaxyGames.UNode.Editors.Commands {
	public class MakeArrayItem : CreateNodeCommand<MakeArrayNode> {
		public override string name {
			get {
				return "Make Array";
			}
		}

		public override System.Type icon => typeof(object[]);

		protected override void OnNodeCreated(MakeArrayNode node) {
			node.EnsureRegistered();
			if(filter != null) {
				var type = filter.GetActualType();
				if(type != typeof(object)) {
					node.elementType = type.ElementType();
				}
			}
			node.Register();
		}

		public override bool IsValid() {
			if(filter == null) {
				return true;
			}
			var type = filter.GetActualType();
			if(type == null)
				return true;
			return type.IsArray || filter.IsValidType(typeof(object[]));
		}
	}
}
using System;
using System.Collections.Generic;
using MaxyGames.UNode.Nodes;

namespace MaxyGames.UNode.Editors.Commands {
	public class MakeDictionaryItem : CreateNodeCommand<MakeDictionaryNode> {
		public override string name {
			get {
				return "Make Dictionary";
			}
		}

		public override Type icon => typeof(Dictionary<,>);

		protected override void OnNodeCreated(MakeDictionaryNode node) {
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
			return filter.IsValidType(typeof(Dictionary<,>));
		}
	}
}
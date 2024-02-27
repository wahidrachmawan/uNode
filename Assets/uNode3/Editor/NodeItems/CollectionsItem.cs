using System;
using System.Collections;

namespace MaxyGames.UNode.Editors.Commands {
	public class RemoveListItemAtIndex : CreateNodeCommand<MultipurposeNode> {
		public override string name {
			get {
				return "Remove Item At Index";
			}
		}

		public override string category {
			get {
				return "Collections.List";
			}
		}

		public override Type icon => typeof(IList);

		protected override void OnNodeCreated(MultipurposeNode node) {
			node.target = MemberData.CreateFromMember(typeof(IList).GetMethod(nameof(IList.RemoveAt)));
		}

		public override bool IsValid() {
			if(filter == null) {
				return true;
			}
			var type = filter.GetActualType();
			if(type == null)
				return true;
			return type == typeof(object) || type.IsCastableTo(typeof(IList));
		}
	}
	public class ClearListItem : CreateNodeCommand<MultipurposeNode> {
		public override string name {
			get {
				return "Clear Item";
			}
		}

		public override string category {
			get {
				return "Collections.List";
			}
		}

		public override Type icon => typeof(IList);

		protected override void OnNodeCreated(MultipurposeNode node) {
			node.target = MemberData.CreateFromMember(typeof(IList).GetMethod(nameof(IList.Clear)));
		}

		public override bool IsValid() {
			if(filter == null) {
				return true;
			}
			var type = filter.GetActualType();
			if(type == null)
				return true;
			return type == typeof(object) || type.IsCastableTo(typeof(IList));
		}
	}
}
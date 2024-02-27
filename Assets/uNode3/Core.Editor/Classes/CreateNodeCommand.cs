using System;
using UnityEngine;

namespace MaxyGames.UNode.Editors {
	public abstract class CreateNodeCommand<T> : INodeItemCommand where T : Node {
		public NodeGraph graph { get; set; }
		public FilterAttribute filter { get; set; }

		public abstract string name { get; }

		public virtual string category {
			get {
				return "Data";
			}
		}

		public virtual int order {
			get {
				return 0;
			}
		}

		public virtual Type icon => null;

		protected abstract void OnNodeCreated(T node);

		public virtual Node Setup(Vector2 mousePosition) {
			Node node = null;
			NodeEditorUtility.AddNewNode<T>(
				graph.graphData,
				mousePosition,
				(n) => {
					OnNodeCreated(n);
					node = n;
					graph.Refresh();
				});
			return node;
		}

		public virtual bool IsValid() {
			return true;
		}
	}
}
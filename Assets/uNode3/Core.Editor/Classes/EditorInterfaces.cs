namespace MaxyGames.UNode.Editors {
	public interface INodeItemCommand {
		GraphEditor graph { get; set; }
		public NodeFilter nodeFilter { get; set; }
		string name { get; }
		string category { get; }
		FilterAttribute filter { get; set; }
		Node Setup(UnityEngine.Vector2 mousePosition);
		System.Type icon { get; }
		bool IsValid();
		int order { get; }
	}

	public interface INodeDrawer {
		GraphEditor graph { get; set; }
		int order { get; }
		void Setup(Node node);
		float OnGUI();
		bool IsValid(System.Type nodeType);
	}
}
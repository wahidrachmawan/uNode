namespace MaxyGames.UNode.Editors {
	public interface INodeItemCommand {
		NodeGraph graph { get; set; }
		string name { get; }
		string category { get; }
		FilterAttribute filter { get; set; }
		Node Setup(UnityEngine.Vector2 mousePosition);
		System.Type icon { get; }
		bool IsValid();
		int order { get; }
	}

	public interface INodeDrawer {
		NodeGraph graph { get; set; }
		int order { get; }
		void Setup(Node node);
		float OnGUI();
		bool IsValid(System.Type nodeType);
	}
}
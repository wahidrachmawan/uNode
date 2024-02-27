using System.Collections;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace MaxyGames.UNode.Editors {
	public interface IElementResizable {
		void OnStartResize();
		void OnResized();
	}

	public interface IDragableElement {
		void StartDrag();
	}

	public interface IDragManager {
		List<VisualElement> draggableElements { get; }
	}

	public interface IDragableGraphHandler {
		bool CanAcceptDrag(GraphDraggedData data);

		void AcceptDrag(GraphDraggedData data);
	}
}
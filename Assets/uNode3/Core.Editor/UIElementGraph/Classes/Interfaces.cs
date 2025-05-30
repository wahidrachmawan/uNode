using System.Collections;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace MaxyGames.UNode.Editors {
	public interface IElementResizable {
		/// <summary>
		/// Start of the resize
		/// </summary>
		void OnStartResize() { }
		/// <summary>
		/// Apply resize element
		/// </summary>
		void OnResized() { }
		/// <summary>
		/// Update each time element is resized
		/// </summary>
		void OnResizeUpdate() { }
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
using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace MaxyGames.UNode.Editors.UI {
/// <summary>
/// Drag-and-drop controller for the favorites tree.
///
/// Extends the standard TreeViewCustomDragAndDropController so that all
/// hit-detection, drag-start, and indicator rendering come from Unity's
/// built-in implementation (which works now that favorites rows use the
/// same PanelElement pattern as GraphPanel).
///
/// The only override is OnDrop: instead of calling viewController.Move
/// (which would mutate Unity's internal tree into a state that doesn't
/// match our data source), it reads the dragged entry id from the drag
/// payload and forwards the drop position to the window:
/// - OverItem   nest the moved entry INSIDE the targeted row (folder),
///   matching GraphPanel's TreeViewUGraphElementDragAndDropController.
/// - otherwise  resolve the slot via the callback and persist the move
///   through NodeBrowserManager, then let the window rebuild the tree.
/// </summary>
internal class NodeBrowserReorderController : TreeViewCustomDragAndDropController {
	readonly Action<NodeBrowserDataAsset.BrowserEntry, int, bool> onDrop;

	public NodeBrowserReorderController(BaseTreeView view, Action<NodeBrowserDataAsset.BrowserEntry, int, bool> onDrop) : base(view, null) {
		this.onDrop = onDrop;
	}

	public override void OnDrop(IListDragAndDropArgs args) {
		// Read the dragged entry from the drag payload.
		NodeBrowserDataAsset.BrowserEntry movedEntry = null;
		try {
			var data = DragAndDropUtility.dragAndDrop.data;
			movedEntry = data?.GetGenericData("favoriteEntry") as NodeBrowserDataAsset.BrowserEntry;
		} catch { }
		if(movedEntry == null)
			return;

		bool overItem = args.dragAndDropPosition == DragAndDropPosition.OverItem;
		onDrop?.Invoke(movedEntry, args.insertAtIndex, overItem);
	}
}
}


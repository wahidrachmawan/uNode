using System;
using System.Collections.Generic;

namespace MaxyGames.UNode.Editors {
	public class GraphCanvasData {
		public readonly HashSet<string> features = new();

		public class AddNodeFilterData {
			public Action<FilterAttribute> ManipulateFilter;
			public Action<ItemSelector> ManipulateWindow;

			public void ApplyManipulator(FilterAttribute filter) {
				ManipulateFilter?.Invoke(filter);
			}

			public void ApplyManipulator(ItemSelector window) {
				ManipulateWindow?.Invoke(window);
			}

			public void Reset() {
				ManipulateFilter = null;
				ManipulateWindow = null;
			}
		}

		public AddNodeFilterData addNodeFilter = new();

		public bool SupportSurroundWith => features.Contains(nameof(GraphManipulator.Feature.SurroundWith));
		public bool SupportMacro => features.Contains(nameof(GraphManipulator.Feature.Macro));
		public bool SupportPlaceFit => features.Contains(nameof(GraphManipulator.Feature.PlaceFit));
		public bool ShowAddNodeContextMenu => features.Contains(nameof(GraphManipulator.Feature.ShowAddNodeContextMenu));

		public bool IsFeatureSupported(string feature) => feature.Contains(feature);

		public void Reset() {
			features.Clear();
			addNodeFilter.Reset();
		}
	}
}
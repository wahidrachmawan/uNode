using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.ShortcutManagement;

namespace MaxyGames.UNode.Editors {
	public enum GraphShortcutType {
		Save,
		Refresh,
		Rename,
		AddNode,
		CreateRegion,
		FrameGraph,
		OpenCommand,
		CopySelectedNodes,
		DuplicateNodes,
		PasteNodesClean,
		PasteNodesWithLink,
		PreviewScript,
		CompileScript,
		DeleteSelectedNodes,
		CutSelectedNodes,
		SelectAllNodes,
		PlaceFitNodes,
	}

	internal static class EditorShortcut {
		//[Shortcut("uNode/Inspect", typeof(uNodeEditor), KeyCode.Mouse0, ShortcutModifiers.Shift)]
		//static void Shortcut_Inspect(ShortcutArguments args) {
		//	var window = args.context as uNodeEditor;
		//	if(window != null) {

		//	}
		//}

		[Shortcut("uNode/Save", typeof(uNodeEditor), KeyCode.S, ShortcutModifiers.Action)]
		static void Shortcut_Save(ShortcutArguments args) {
			var window = args.context as uNodeEditor;
			if(window != null) {
				window.graphEditor.HandleShortcut(GraphShortcutType.Save);
			}
		}

		[Shortcut("uNode/Preview C# Script", typeof(uNodeEditor), KeyCode.F9)]
		static void Shortcut_PreviewScript(ShortcutArguments args) {
			var window = args.context as uNodeEditor;
			if(window != null) {
				window.graphEditor.HandleShortcut(GraphShortcutType.PreviewScript);
			}
		}

		[Shortcut("uNode/Compile Script", typeof(uNodeEditor), KeyCode.F10)]
		static void Shortcut_CompileScript(ShortcutArguments args) {
			var window = args.context as uNodeEditor;
			if(window != null) {
				window.graphEditor.HandleShortcut(GraphShortcutType.CompileScript);
			}
		}

		[Shortcut("uNode/Refresh", typeof(uNodeEditor), KeyCode.F5)]
		static void Shortcut_Refresh(ShortcutArguments args) {
			var window = args.context as uNodeEditor;
			if(window != null) {
				window.graphEditor.HandleShortcut(GraphShortcutType.Refresh);
			}
		}

		[Shortcut("uNode/Rename", typeof(uNodeEditor), KeyCode.F2)]
		static void Shortcut_Rename(ShortcutArguments args) {
			var window = args.context as uNodeEditor;
			if(window != null) {
				window.graphEditor.HandleShortcut(GraphShortcutType.Rename);
			}
		}

		[Shortcut("uNode/Add Node", typeof(uNodeEditor), KeyCode.Space)]
		static void Shortcut_AddNode(ShortcutArguments args) {
			var window = args.context as uNodeEditor;
			if(window != null) {
				window.graphEditor.HandleShortcut(GraphShortcutType.AddNode);
			}
		}

		[Shortcut("uNode/Quick Create Region", typeof(uNodeEditor), KeyCode.F, ShortcutModifiers.Action)]
		static void Shortcut_QuickCreateRegion(ShortcutArguments args) {
			var window = args.context as uNodeEditor;
			if(window != null) {
				window.graphEditor.HandleShortcut(GraphShortcutType.CreateRegion);
			}
		}

		[Shortcut("uNode/Place Fit selected node", typeof(uNodeEditor), KeyCode.Q)]
		static void Shortcut_PlaceFitSelections(ShortcutArguments args) {
			var window = args.context as uNodeEditor;
			if(window != null) {
				window.graphEditor.HandleShortcut(GraphShortcutType.PlaceFitNodes);
			}
		}

		[Shortcut("uNode/Frame Graph", typeof(uNodeEditor), KeyCode.F)]
		static void Shortcut_FrameGraph(ShortcutArguments args) {
			var window = args.context as uNodeEditor;
			if(window != null) {
				window.graphEditor.HandleShortcut(GraphShortcutType.FrameGraph);
			}
		}

		[Shortcut("uNode/Open Command", typeof(uNodeEditor), KeyCode.Space, ShortcutModifiers.Action)]
		static void Shortcut_OpenCommand(ShortcutArguments args) {
			var window = args.context as uNodeEditor;
			if(window != null) {
				window.graphEditor.HandleShortcut(GraphShortcutType.OpenCommand);
			}
		}

		//[Shortcut("uNode/Graph Canvas/Add - Remove Node from selection", typeof(uNodeEditor), KeyCode.Mouse0, ShortcutModifiers.Action)]
		//static void Shortcut_AddOrRemoveNode(ShortcutArguments args) {
		//	var window = args.context as uNodeEditor;
		//	if(window != null) {

		//	}
		//}

		[Shortcut("uNode/Select all nodes", typeof(uNodeEditor), KeyCode.A, ShortcutModifiers.Action)]
		static void Shortcut_SelectAll(ShortcutArguments args) {
			var window = args.context as uNodeEditor;
			if(window != null) {
				window.graphEditor.HandleShortcut(GraphShortcutType.SelectAllNodes);
			}
		}

		[Shortcut("uNode/Copy selected node", typeof(uNodeEditor), KeyCode.C, ShortcutModifiers.Action)]
		static void Shortcut_Copy(ShortcutArguments args) {
			var window = args.context as uNodeEditor;
			if(window != null) {
				window.graphEditor.HandleShortcut(GraphShortcutType.CopySelectedNodes);
			}
		}

		[Shortcut("uNode/Cut selected node", typeof(uNodeEditor), KeyCode.X, ShortcutModifiers.Action)]
		static void Shortcut_Cut(ShortcutArguments args) {
			var window = args.context as uNodeEditor;
			if(window != null) {
				window.graphEditor.HandleShortcut(GraphShortcutType.CutSelectedNodes);
			}
		}

		[Shortcut("uNode/Delete selected node", typeof(uNodeEditor), KeyCode.Delete)]
		static void Shortcut_Delete(ShortcutArguments args) {
			var window = args.context as uNodeEditor;
			if(window != null) {
				window.graphEditor.HandleShortcut(GraphShortcutType.DeleteSelectedNodes);
			}
		}

		[Shortcut("uNode/Duplicate selected node", typeof(uNodeEditor), KeyCode.D, ShortcutModifiers.Action)]
		static void Shortcut_Duplicate(ShortcutArguments args) {
			var window = args.context as uNodeEditor;
			if(window != null) {
				window.graphEditor.HandleShortcut(GraphShortcutType.DuplicateNodes);
			}
		}

		[Shortcut("uNode/Paste node ( With Link )", typeof(uNodeEditor), KeyCode.V, ShortcutModifiers.Action)]
		static void Shortcut_PasteWithLink(ShortcutArguments args) {
			var window = args.context as uNodeEditor;
			if(window != null) {
				window.graphEditor.HandleShortcut(GraphShortcutType.PasteNodesWithLink);
			}
		}

		[Shortcut("uNode/Paste node( Clean )", typeof(uNodeEditor), KeyCode.V, ShortcutModifiers.Action | ShortcutModifiers.Shift)]
		static void Shortcut_PasteClean(ShortcutArguments args) {
			var window = args.context as uNodeEditor;
			if(window != null) {
				window.graphEditor.HandleShortcut(GraphShortcutType.PasteNodesClean);
			}
		}
	}
}
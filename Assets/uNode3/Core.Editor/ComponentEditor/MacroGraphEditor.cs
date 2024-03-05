using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditorInternal;

namespace MaxyGames.UNode.Editors {
	[CustomEditor(typeof(MacroGraph), true)]
	class MacroGraphEditor : GraphAssetEditor {
		List<Nodes.MacroPortNode> inputFlows = new List<Nodes.MacroPortNode>();
		List<Nodes.MacroPortNode> inputValues = new List<Nodes.MacroPortNode>();
		List<Nodes.MacroPortNode> outputFlows = new List<Nodes.MacroPortNode>();
		List<Nodes.MacroPortNode> outputValues = new List<Nodes.MacroPortNode>();

		public override void DrawGUI(bool isInspector) {
			var asset = target as MacroGraph;
			uNodeGUIUtility.ShowField(nameof(asset.icon), asset, asset);
			uNodeGUIUtility.ShowField(nameof(asset.category), asset, asset);
			uNodeGUI.DrawNamespace("Using Namespaces", asset.usingNamespaces, asset, (arr) => {
				asset.usingNamespaces = arr as List<string> ?? arr.ToList();
				uNodeEditorUtility.MarkDirty(asset);
			});

			inputFlows.Clear();
			inputFlows.AddRange(asset.inputFlows);
			inputValues.Clear();
			inputValues.AddRange(asset.inputValues);
			outputFlows.Clear();
			outputFlows.AddRange(asset.outputFlows);
			outputValues.Clear();
			outputValues.AddRange(asset.outputValues);

			uNodeGUI.DrawCustomList(
				inputFlows,
				"Input Flows",
				(position, index, element) => {//Draw Element
					EditorGUI.LabelField(position, new GUIContent(element.GetTitle(), uNodeEditorUtility.GetTypeIcon(typeof(TypeIcons.FlowIcon))));
				}, null, null,
				reorder: (ReorderableList list, int oldIndex, int newIndex) => {
					var Old = inputFlows[oldIndex];
					var New = inputFlows[newIndex];
					Old.nodeObject.SetSiblingIndex(New.nodeObject.GetSiblingIndex());
				});

			uNodeGUI.DrawCustomList(
				outputFlows,
				"Output Flows",
				(position, index, element) => {//Draw Element
					EditorGUI.LabelField(position, new GUIContent(element.GetTitle(), uNodeEditorUtility.GetTypeIcon(typeof(TypeIcons.FlowIcon))));
				}, null, null,
				reorder: (ReorderableList list, int oldIndex, int newIndex) => {
					var Old = outputFlows[oldIndex];
					var New = outputFlows[newIndex];
					Old.nodeObject.SetSiblingIndex(New.nodeObject.GetSiblingIndex());
				});

			uNodeGUI.DrawCustomList(
				inputValues,
				"Input Values",
				(position, index, element) => {//Draw Element
					position.width -= EditorGUIUtility.labelWidth;
					EditorGUI.LabelField(position, new GUIContent(element.GetTitle(), uNodeEditorUtility.GetTypeIcon(element.ReturnType())));
					position.x += EditorGUIUtility.labelWidth;
					uNodeGUIUtility.DrawTypeDrawer(position, element.type, GUIContent.none, type => {
						element.type = type;
						uNodeGUIUtility.GUIChangedMajor(element);
					}, null, asset);
				}, null, null,
				reorder: (ReorderableList list, int oldIndex, int newIndex) => {
					var Old = inputValues[oldIndex];
					var New = inputValues[newIndex];
					Old.nodeObject.SetSiblingIndex(New.nodeObject.GetSiblingIndex());
				});

			uNodeGUI.DrawCustomList(
				outputValues,
				"Output Values",
				(position, index, element) => {//Draw Element
					position.width -= EditorGUIUtility.labelWidth;
					EditorGUI.LabelField(position, new GUIContent(element.GetTitle(), uNodeEditorUtility.GetTypeIcon(element.ReturnType())));
					position.x += EditorGUIUtility.labelWidth;
					uNodeGUIUtility.DrawTypeDrawer(position, element.type, GUIContent.none, type => {
						element.type = type;
						uNodeGUIUtility.GUIChangedMajor(element);
					}, null, asset);
				}, null, null,
				reorder: (ReorderableList list, int oldIndex, int newIndex) => {
					var Old = outputValues[oldIndex];
					var New = outputValues[newIndex];
					Old.nodeObject.SetSiblingIndex(New.nodeObject.GetSiblingIndex());
				});

			base.DrawGUI(isInspector);

			if(isInspector) {
				DrawOpenGraph();
			}
		}
	}
}
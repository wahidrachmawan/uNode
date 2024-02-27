using UnityEngine;
using UnityEditor;
using System;
using System.Reflection;

namespace MaxyGames.UNode.Editors {
	public abstract class NodeDrawer : UPropertyDrawer {
		public static void DrawNicelyHeader(NodeObject node, Type icon, UnityEngine.Object unityObject = null) {
			EditorGUILayout.BeginHorizontal();
			GUI.DrawTexture(uNodeGUIUtility.GetRect(GUILayout.Width(32), GUILayout.Height(32)), uNodeEditorUtility.GetTypeIcon(icon));
			EditorGUILayout.BeginVertical();
			EditorGUILayout.BeginHorizontal();
			var name = uNodeGUI.TextInput(node.name, "(Title)", false);
			if(!string.IsNullOrEmpty(name) && name != node.name) {
				if(unityObject)
					uNodeEditorUtility.RegisterUndo(unityObject);
				node.name = name;
				uNodeGUIUtility.GUIChanged(node, UIChangeType.Average);
			}
			EditorGUILayout.EndHorizontal();
			GUILayout.Space(2);
			var comment = uNodeGUI.TextInput(node.comment, "(Summary)", true);
			if(comment != node.comment) {
				if(unityObject)
					uNodeEditorUtility.RegisterUndo(unityObject);
				node.comment = comment;
				uNodeGUIUtility.GUIChanged(node, UIChangeType.Average);
			}
			EditorGUILayout.EndVertical();
			EditorGUILayout.EndHorizontal();
			if(node.node != null) {
				var monoScript = uNodeEditorUtility.GetMonoScript(node.node.GetType());
				if(monoScript != null) {
					GUILayout.Space(4);
					EditorGUI.BeginDisabledGroup(true);
					EditorGUILayout.ObjectField(new GUIContent("Script"), monoScript, typeof(MonoScript), false);
					EditorGUI.EndDisabledGroup();
				}
			}
			EditorGUI.BeginDisabledGroup(true);
			EditorGUILayout.IntField(new GUIContent("ID"), node.id);
			EditorGUI.EndDisabledGroup();
			EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
		}

		protected virtual void DrawInputs(DrawerOption option) {
			var node = GetValue<Node>(option.property);
			EditorGUILayout.BeginVertical("Box");
			EditorGUILayout.LabelField("Inputs", EditorStyles.centeredGreyMiniLabel);
			foreach(var port in node.nodeObject.FlowInputs) {
				EditorGUILayout.LabelField(new GUIContent(port.GetPrettyName() + " : Flow", uNodeEditorUtility.GetTypeIcon(typeof(TypeIcons.FlowIcon))), EditorStyles.boldLabel);
				var tooltip = port.tooltip;
				if(!string.IsNullOrEmpty(tooltip)) {
					EditorGUI.indentLevel++;
					EditorGUILayout.LabelField(tooltip, EditorStyles.wordWrappedLabel);
					EditorGUI.indentLevel--;
				} else if(port == node.nodeObject.primaryFlowInput) {
					EditorGUI.indentLevel++;
					EditorGUILayout.LabelField("The flow to begin the execution of node", EditorStyles.wordWrappedLabel);
					EditorGUI.indentLevel--;
				}
			}
			foreach(var port in node.nodeObject.ValueInputs) {
				EditorGUILayout.LabelField(new GUIContent(port.GetPrettyName() + " : " + port.type.PrettyName(), uNodeEditorUtility.GetTypeIcon(port.type)), EditorStyles.boldLabel);
				var tooltip = port.tooltip;
				if(!string.IsNullOrEmpty(tooltip)) {
					EditorGUI.indentLevel++;
					EditorGUILayout.LabelField(tooltip, EditorStyles.wordWrappedLabel);
					EditorGUI.indentLevel--;
				}
			}
			EditorGUILayout.EndVertical();
		}

		protected virtual void DrawOutputs(DrawerOption option) {
			var node = GetValue<Node>(option.property);
			EditorGUILayout.BeginVertical("Box");
			EditorGUILayout.LabelField("Outputs", EditorStyles.centeredGreyMiniLabel);
			foreach(var port in node.nodeObject.FlowOutputs) {
				EditorGUILayout.LabelField(new GUIContent(port.GetPrettyName() + " : Flow", uNodeEditorUtility.GetTypeIcon(typeof(TypeIcons.FlowIcon))), EditorStyles.boldLabel);
				var tooltip = port.tooltip;
				if(!string.IsNullOrEmpty(tooltip)) {
					EditorGUI.indentLevel++;
					EditorGUILayout.LabelField(tooltip, EditorStyles.wordWrappedLabel);
					EditorGUI.indentLevel--;
				} else if(port == node.nodeObject.primaryFlowOutput) {
					EditorGUI.indentLevel++;
					EditorGUILayout.LabelField("The output flow to executes", EditorStyles.wordWrappedLabel);
					EditorGUI.indentLevel--;
				}
			}
			foreach(var port in node.nodeObject.ValueOutputs) {
				EditorGUILayout.LabelField(new GUIContent(port.GetPrettyName() + " : " + port.type.PrettyName(), uNodeEditorUtility.GetTypeIcon(port.type)), EditorStyles.boldLabel);
				var tooltip = port.tooltip;
				if(!string.IsNullOrEmpty(tooltip)) {
					EditorGUI.indentLevel++;
					EditorGUILayout.LabelField(tooltip, EditorStyles.wordWrappedLabel);
					EditorGUI.indentLevel--;
				} else if(port == node.nodeObject.primaryValueOutput) {
					EditorGUI.indentLevel++;
					EditorGUILayout.LabelField("The output value", EditorStyles.wordWrappedLabel);
					EditorGUI.indentLevel--;
				}
			}
			EditorGUILayout.EndVertical();
		}

		protected virtual void DrawErrors(DrawerOption option) {
			var node = GetValue<Node>(option.property);
			var desc = node.GetType().GetCustomAttribute<DescriptionAttribute>();
			if(desc != null) {
				EditorGUILayout.HelpBox(desc.description, MessageType.Info);
			}

			GraphUtility.ErrorChecker.DrawErrorMessages(node.nodeObject);
		}

		public override void DrawLayouted(DrawerOption option) {
			DrawChilds(option);
			DrawInputs(option);
			DrawOutputs(option);
			DrawErrors(option);
		}
	}

	public abstract class NodeDrawer<T> : NodeDrawer where T : Node {
		public T GetNode(DrawerOption option) {
			return GetValue<T>(option.property);
		}

		public override bool IsValid(Type type, bool layouted) {
			return type == typeof(T);
		}
	}
}

namespace MaxyGames.UNode.Editors.Drawer {
	class DefaultNodeDrawer : NodeDrawer<Node> {
		public override int order => 10000;

		public override bool IsValid(Type type, bool layouted) {
			return type == typeof(Node) || type.IsSubclassOf(typeof(Node));
		}
	}
}
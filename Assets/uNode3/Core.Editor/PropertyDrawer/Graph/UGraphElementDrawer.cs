using UnityEngine;
using UnityEditor;
using System;

namespace MaxyGames.UNode.Editors {
	public abstract class UGraphElementDrawer : UPropertyDrawer {
		public override bool IsValid(Type type, bool layouted) {
			return type == typeof(UGraphElement) || type.IsSubclassOf(typeof(UGraphElement));
		}

		protected virtual void DrawHeader(DrawerOption option) {
			//UInspector.Draw(new DrawerOption() {
			//	property = option.property[nameof(UGraphElement.name)],
			//	nullable = false,
			//});
			//UInspector.Draw(new DrawerOption() {
			//	property = option.property[nameof(UGraphElement.comment)],
			//	nullable = false,
			//});
			DrawNicelyHeader(option, option.type);
		}

		protected void DrawNicelyHeader(DrawerOption option, Type type) {
			var value = option.value as UGraphElement;
			EditorGUILayout.BeginHorizontal();
			GUI.DrawTexture(uNodeGUIUtility.GetRect(GUILayout.Width(32), GUILayout.Height(32)), uNodeEditorUtility.GetTypeIcon(type));
			EditorGUILayout.BeginVertical();
			EditorGUILayout.BeginHorizontal();
			var name = uNodeGUI.TextInput(value.name, "(Title)", false);
			if(!string.IsNullOrEmpty(name) && name != value.name) {
				option.RegisterUndo();
				if(value is Variable || value is Property || value is Function) {
					value.name = uNodeUtility.AutoCorrectName(name);
					uNodeGUIUtility.GUIChangedMajor(value);
				}
				else {
					value.name = name;
					uNodeGUIUtility.GUIChanged(value, UIChangeType.Average);
				}
			}
			EditorGUILayout.EndHorizontal();
			GUILayout.Space(2);
			var comment = uNodeGUI.TextInput(value.comment, "(Summary)", true);
			if(comment != value.comment) {
				option.RegisterUndo();
				value.comment = comment;
			}
			EditorGUI.BeginDisabledGroup(true);
			EditorGUILayout.IntField(new GUIContent("ID"), value.id);
			EditorGUI.EndDisabledGroup();
			EditorGUILayout.EndVertical();
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
		}

		public override void DrawLayouted(DrawerOption option) {
			if(option.property is UBindGraphElement) {
				DrawDecorators(option);
				DrawHeader(option);
				DoDraw(option);
				DrawErrors(option);
			}
			else {
				base.DrawLayouted(option);
			}
		}

		protected virtual void DoDraw(DrawerOption option) {
			DrawChilds(option);
		}

		public override void Draw(Rect position, DrawerOption option) {
			if(option.property is UBindGraphElement) {
				EditorGUI.BeginDisabledGroup(true);
				EditorGUI.DropdownButton(position, new GUIContent(option.label.text, uNodeEditorUtility.GetTypeIcon(option.type)), FocusType.Keyboard, EditorStyles.objectField);
				EditorGUI.EndDisabledGroup();
			}
			else {
				position = EditorGUI.PrefixLabel(position, option.label);
				EditorGUI.BeginDisabledGroup(true);
				var text = (option.value as UGraphElement)?.name ?? "null";
				EditorGUI.DropdownButton(position, new GUIContent(text, uNodeEditorUtility.GetTypeIcon(option.type)), FocusType.Keyboard, EditorStyles.objectField);
				EditorGUI.EndDisabledGroup();
			}
		}

		protected virtual void DrawErrors(DrawerOption option) {
			var value = option.value as UGraphElement;
			GraphUtility.ErrorChecker.DrawErrorMessages(value);
		}
	}

	public abstract class UGraphElementDrawer<T> : UGraphElementDrawer where T : UGraphElement {
		public override bool IsValid(Type type, bool layouted) {
			return type == typeof(T) || type.IsSubclassOf(typeof(T));
		}
	}
}


namespace MaxyGames.UNode.Editors.Drawer {
	class DefaultUGraphElementDrawer : UGraphElementDrawer {
		public override int order => 10000;
	}

	class NodeObjectDrawer : UGraphElementDrawer<NodeObject> {
		public override int order => 1000;

		protected override void DoDraw(DrawerOption option) {
			var node = GetValue<NodeObject>(option.property);
			if(node.node == null) {
				EditorGUILayout.HelpBox("Missing node type: " + node.serializedData.serializedType, MessageType.Error);
				if(GUILayout.Button("Change Node Type")) {
					var win = ItemSelector.ShowType(option.unityObject, new FilterAttribute(typeof(Node)) { DisplayAbstractType = false }, member => {
						option.property.RegisterUndo();
						node.serializedData.type = member.startType;
						(node as ISerializationCallbackReceiver).OnAfterDeserialize();
						uNodeGUIUtility.GUIChangedMajor(node);
					}).ChangePosition(Event.current.mousePosition.ToScreenPoint());
					win.usingNamespaces = new() { "MaxyGames", "MaxyGames.UNode", "MaxyGames.UNode.Nodes" };
					foreach(var ns in node.graphContainer.GetUsingNamespaces()) {
						win.usingNamespaces.Add(ns);
					}
				}
			}
			else {
				UInspector.Draw(option.property[nameof(NodeObject.node)], label: GUIContent.none);
			}
		}
	}
}
using UnityEngine;
using UnityEditor;
using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using Object = UnityEngine.Object;
using System.IO;

namespace MaxyGames.UNode.Editors {
	class NodeCreatorWindow : EditorWindow {
		public string nodeName;
		public string nodeCategory = "Custom Nodes";
		public string nodeDescription;
		public string @namespace = "MaxyGames.UNode.Nodes";
		public List<string> usingNamespaces = new List<string>() {
			"System",
			"System.Collections.Generic",
			"UnityEngine",
			"MaxyGames.UNode",
		};
		[Tooltip("If true, each node is instantiated and can use variable")]
		public bool instanceNode;

		public int primaryValueOutput;
		public int primaryFlowInput;
		public int primaryFlowOutput;

		public List<ValueInputData> valueInputs = new();
		public List<ValueOutputData> valueOutputs = new();
		public List<FlowInputData> flowInputs = new();
		public List<FlowOutputData> flowOutputs = new();

		[SerializeField]
		private int m_currentID;
		private static int currentID {
			get {
				if(window != null) {
					return window.m_currentID;
				}
				return 0;
			}
			set {
				if(window != null) {
					window.m_currentID = value;
				}
			}
		}

		public enum ExitKind {
			None,
			Regular,
			Enum,
			Conditional,
		}

		public enum FlowOutputKind {
			Regular,
			Enum,
			Conditional,
		}

		[Serializable]
		public abstract class PortData {
			public string name;
			public string description;
			public int id = ++currentID;
		}

		[Serializable]
		public class ValueInputData : PortData {
			public SerializedType type = typeof(object);
		}

		[Serializable]
		public class ValueOutputData : PortData {
			public SerializedType type = typeof(object);
			public bool useProperty;
			public int[] requiredInputs = new int[0];
		}

		[Serializable]
		public class FlowInputData : PortData {
			public ExitKind exitKind = ExitKind.None;
			public int regularExitID, enumExitID, conditionalExitID;
		}

		[Serializable]
		public class FlowOutputData : PortData {
			public FlowOutputKind kind;
			public List<FlowOutputEnumData> enums = new();
		}

		[Serializable]
		public class FlowOutputEnumData : PortData {

		}

		private static NodeCreatorWindow window;
		[MenuItem("Tools/uNode/Node Creator Wizard", false, 104)]
		public static NodeCreatorWindow ShowWindow() {
			window = GetWindow<NodeCreatorWindow>(true);
			window.minSize = new Vector2(300, 350);
			window.titleContent = new GUIContent("Node Creator Wizard");
			window.Show();
			return window;
		}

		private Vector2 scroll;
		private void OnGUI() {
			scroll = EditorGUILayout.BeginScrollView(scroll);
			nodeName = EditorGUILayout.TextField("Node Name", nodeName);
			nodeCategory = EditorGUILayout.TextField("Node Category", nodeCategory);
			nodeDescription = EditorGUILayout.TextField("Node Description", nodeDescription);
			@namespace = EditorGUILayout.TextField("Namespace", @namespace);
			uNodeGUI.DrawNamespace("Using Namespaces", usingNamespaces, null);
			uNodeGUIUtility.EditValueLayouted(nameof(instanceNode), this);
			uNodeGUI.DrawCustomList(valueInputs, "Value Inputs",
				drawElement: (position, index, value) => {
					position.height = EditorGUIUtility.singleLineHeight;
					value.name = EditorGUI.TextField(position, "Name", value.name);
					position.y += EditorGUIUtility.singleLineHeight;
					value.description = EditorGUI.TextField(position, "Description", value.description);
					position.y += EditorGUIUtility.singleLineHeight;
					uNodeGUIUtility.EditValue(position, nameof(value.type), value);
				},
				add: position => {
					valueInputs.Add(new() { name = "Input" + valueInputs.Count });
				},
				remove: index => {
					valueInputs.RemoveAt(index);
				},
				elementHeight: index => {
					return EditorGUIUtility.singleLineHeight * 3;
				});

			uNodeGUI.DrawCustomList(valueOutputs, "Value Outputs",
				drawElement: (position, index, value) => {
					position.height = EditorGUIUtility.singleLineHeight;
					value.name = uNodeUtility.AutoCorrectName(EditorGUI.DelayedTextField(position, "Name", value.name));
					position.y += EditorGUIUtility.singleLineHeight;
					value.description = EditorGUI.TextField(position, "Description", value.description);
					position.y += EditorGUIUtility.singleLineHeight;
					uNodeGUIUtility.EditValue(position, nameof(value.type), value);
					{
						position.y += EditorGUIUtility.singleLineHeight;
						var isPrimary = value.id == primaryValueOutput;
						var primary = EditorGUI.Toggle(position, "Is Primary", isPrimary);
						if(primary != isPrimary) {
							if(primary) {
								primaryValueOutput = value.id;
							}
							else {
								primaryValueOutput = 0;
							}
						}
					}
					if(instanceNode) {
						position.y += EditorGUIUtility.singleLineHeight;
						uNodeGUIUtility.EditValue(position, nameof(value.useProperty), value);
					}
				},
				add: position => {
					valueOutputs.Add(new() { name = "Output" + valueOutputs.Count });
				},
				remove: index => {
					valueOutputs.RemoveAt(index);
				},
				elementHeight: index => {
					if(instanceNode) {
						return EditorGUIUtility.singleLineHeight * 5;
					}
					return EditorGUIUtility.singleLineHeight * 4;
				});


			uNodeGUI.DrawCustomList(flowInputs, "Flow Inputs",
				drawElement: (position, index, value) => {
					position.height = EditorGUIUtility.singleLineHeight;
					value.name = uNodeUtility.AutoCorrectName(EditorGUI.DelayedTextField(position, "Name", value.name));
					position.y += EditorGUIUtility.singleLineHeight;
					value.description = EditorGUI.TextField(position, "Description", value.description);
					{
						position.y += EditorGUIUtility.singleLineHeight;
						var isPrimary = value.id == primaryFlowInput;
						var primary = EditorGUI.Toggle(position, "Is Primary", isPrimary);
						if(primary != isPrimary) {
							if(primary) {
								primaryFlowInput = value.id;
							}
							else {
								primaryFlowInput = 0;
							}
						}
					}
					position.y += EditorGUIUtility.singleLineHeight;
					uNodeGUIUtility.EditValue(position, nameof(value.exitKind), value);
					if(value.exitKind == ExitKind.Regular) {
						position.y += EditorGUIUtility.singleLineHeight;

						var outputs = flowOutputs.Where(p => p.kind == FlowOutputKind.Regular).ToList();
						if(outputs.Count == 0) {
							EditorGUI.HelpBox(position, "No regular output is defined, please add new one", MessageType.Error);
						}
						else {
							int selectedIndex = outputs.FindIndex(p => p.id == value.regularExitID);
							if(selectedIndex < 0) {
								selectedIndex = 0;
								value.regularExitID = flowOutputs[flowOutputs.IndexOf(outputs[selectedIndex])].id;
							}
							var newIndex = EditorGUI.Popup(position, "Exit", selectedIndex, outputs.Select(p => p.name).ToArray());
							if(newIndex != selectedIndex) {
								value.regularExitID = flowOutputs[flowOutputs.IndexOf(outputs[newIndex])].id;
							}
						}
					}
					else if(value.exitKind == ExitKind.Enum) {
						position.y += EditorGUIUtility.singleLineHeight;

						var outputs = flowOutputs.Where(p => p.kind == FlowOutputKind.Enum).ToList();
						if(outputs.Count == 0) {
							EditorGUI.HelpBox(position, "No enum output is defined, please add new one", MessageType.Error);
						}
						else {
							int selectedIndex = outputs.FindIndex(p => p.id == value.enumExitID);
							if(selectedIndex < 0) {
								selectedIndex = 0;
								value.enumExitID = flowOutputs[flowOutputs.IndexOf(outputs[selectedIndex])].id;
							}
							var newIndex = EditorGUI.Popup(position, "Exit", selectedIndex, outputs.Select(p => p.name).ToArray());
							if(newIndex != selectedIndex) {
								value.enumExitID = flowOutputs[flowOutputs.IndexOf(outputs[newIndex])].id;
							}
						}
					}
					else if(value.exitKind == ExitKind.Conditional) {
						position.y += EditorGUIUtility.singleLineHeight;

						var outputs = flowOutputs.Where(p => p.kind == FlowOutputKind.Conditional).ToList();
						if(outputs.Count == 0) {
							EditorGUI.HelpBox(position, "No conditional output is defined, please add new one", MessageType.Error);
						}
						else {
							int selectedIndex = outputs.FindIndex(p => p.id == value.conditionalExitID);
							if(selectedIndex < 0) {
								selectedIndex = 0;
								value.conditionalExitID = flowOutputs[flowOutputs.IndexOf(outputs[selectedIndex])].id;
							}
							var newIndex = EditorGUI.Popup(position, "Exit", selectedIndex, outputs.Select(p => p.name).ToArray());
							if(newIndex != selectedIndex) {
								value.conditionalExitID = flowOutputs[flowOutputs.IndexOf(outputs[newIndex])].id;
							}
						}
					}
				},
				add: position => {
					flowInputs.Add(new() { name = "Execute" + flowInputs.Count });
				},
				remove: index => {
					flowInputs.RemoveAt(index);
				},
				elementHeight: index => {
					if(flowInputs[index].exitKind != ExitKind.None) {
						return EditorGUIUtility.singleLineHeight * 5;
					}
					return EditorGUIUtility.singleLineHeight * 4;
				});


			uNodeGUI.DrawCustomList(flowOutputs, "Flow Outputs",
				drawElement: (position, index, value) => {
					position.height = EditorGUIUtility.singleLineHeight;
					value.name = uNodeUtility.AutoCorrectName(EditorGUI.DelayedTextField(position, "Name", value.name));
					position.y += EditorGUIUtility.singleLineHeight;
					uNodeGUIUtility.EditValue(position, nameof(value.kind), value);
					if(value.kind != FlowOutputKind.Enum) {
						position.y += EditorGUIUtility.singleLineHeight;
						value.description = EditorGUI.TextField(position, "Description", value.description);
						position.y += EditorGUIUtility.singleLineHeight;
						var isPrimary = value.id == primaryFlowOutput;
						var primary = EditorGUI.Toggle(position, "Is Primary", isPrimary);
						if(primary != isPrimary) {
							if(primary) {
								primaryFlowOutput = value.id;
							}
							else {
								primaryFlowOutput = 0;
							}
						}
					}
					else {
						position.y += EditorGUIUtility.singleLineHeight;
						var reorderable = uNodeGUI.GetReorderableList(value.enums, "Enums",
							drawElement: (pos, idx, val) => {
								pos.height = EditorGUIUtility.singleLineHeight;
								val.name = uNodeUtility.AutoCorrectName(EditorGUI.DelayedTextField(pos, "Name", val.name));
								pos.y += EditorGUIUtility.singleLineHeight;
								val.description = EditorGUI.TextField(pos, "Description", val.description);
							},
							add: idx => value.enums.Add(new()),
							remove: idx => value.enums.RemoveAt(idx),
							elementHeight: idx => {
								return EditorGUIUtility.singleLineHeight * 2;
							});
						reorderable.DoList(position);
					}
				},
				add: position => {
					flowOutputs.Add(new() { name = "Exit" + flowOutputs.Count });
				},
				remove: index => {
					flowOutputs.RemoveAt(index);
				},
				elementHeight: index => {
					if(flowOutputs[index].kind == FlowOutputKind.Enum) {
						var enums = flowOutputs[index].enums;
						float addition = EditorGUIUtility.singleLineHeight;
						if(enums.Count > 0) {
							addition = (EditorGUIUtility.singleLineHeight * 2 * enums.Count);
						}
						return (EditorGUIUtility.singleLineHeight * 5) + addition;
					}
					return EditorGUIUtility.singleLineHeight * 4;
				});

			EditorGUILayout.EndScrollView();
			GUILayout.FlexibleSpace();
			EditorGUILayout.BeginHorizontal();
			if(GUILayout.Button("Save")) {
				OnSave();
			}
			if(GUILayout.Button("Preview")) {
				OnPreview();
			}
			EditorGUILayout.EndHorizontal();
		}

		public void OnSave() {
			string script = GenerateScript();
			string path = EditorUtility.SaveFilePanelInProject("Create new graph asset",
				uNodeUtility.AutoCorrectName(nodeName) + ".cs",
				"cs",
				"Please enter a file name to save the script to");
			if(path.Length != 0) {
				File.WriteAllText(path, script);
				AssetDatabase.Refresh();
			}
		}

		public void OnPreview() {
			string script = GenerateScript();
			var originalScript = script;
			string highlight = EditorBinding.HighlightSyntax(script);
			if(!string.IsNullOrEmpty(highlight)) {
				script = highlight;
			}
			PreviewSourceWindow.ShowWindow(script, originalScript);
		}

		public string GenerateScript() {
			IGraph obj = null;
			var setting = new CG.GeneratorSetting(obj) {
				onInitialize = Generate,
				disableScriptWarning = false,
			};
			setting.nameSpace = @namespace;
			setting.usingNamespace = usingNamespaces.ToHashSet();

			var data = CG.Generate(setting);
			return data.ToScript();
		}

		void Generate(CG.GeneratedData generatedData) {
			string nodeName = this.nodeName;
			string nodeCategory = this.nodeCategory;
			string className = uNodeUtility.AutoCorrectName(nodeName);

			var classBuilder = new CG.ClassData(className, instanceNode ? typeof(IInstanceNode) : typeof(IStaticNode));
			classBuilder.SetToPublic();

			var menuAtt = new CG.AData(typeof(NodeMenu), CG.Value(nodeCategory), CG.Value(nodeName));
			menuAtt.namedParameters = new();
			if(string.IsNullOrWhiteSpace(nodeDescription) == false) {
				menuAtt.namedParameters.Add(nameof(NodeMenu.tooltip), CG.Value(nodeDescription));
			}
			if(flowInputs.Count > 0) {
				menuAtt.namedParameters.Add(nameof(NodeMenu.hasFlowInput), CG.Value(true));
			}
			if(flowOutputs.Count > 0) {
				menuAtt.namedParameters.Add(nameof(NodeMenu.hasFlowOutput), CG.Value(true));
			}
			if(valueInputs.Count > 0) {
				menuAtt.namedParameters.Add(nameof(NodeMenu.inputs), CG.MakeArray(typeof(Type), valueInputs.Select(p => CG.Value(p.type.type)).ToArray()));
			}
			if(valueOutputs.Count > 0) {
				menuAtt.namedParameters.Add(nameof(NodeMenu.outputs), CG.MakeArray(typeof(Type), valueOutputs.Select(p => CG.Value(p.type.type)).ToArray()));
			}
			classBuilder.attributes = new List<CG.AData>() { menuAtt };

			foreach(var data in valueInputs) {
				CG.VData variable = new CG.VData(data.name, typeof(ValuePortDefinition));
				variable.modifier = new FieldModifier() {
					Public = true,
					Static = instanceNode == false,
				};
				var att = new CG.AData(typeof(InputAttribute), CG.Value(data.type.type));
				if(string.IsNullOrWhiteSpace(data.description) == false) {
					att.namedParameters = new Dictionary<string, string>() {
						{ nameof(PortDescriptionAttribute.description), CG.Value(data.description) }
					};
				}
				variable.attributes = new CG.AData[] { att };
				classBuilder.RegisterVariable(variable.GenerateCode());
			}
			foreach(var data in valueOutputs) {
				if(instanceNode && data.useProperty) {
					CG.PData prop = new CG.PData(data.name, data.type);
					prop.modifier.SetPublic();
					var att = new CG.AData(typeof(OutputAttribute));
					att.namedParameters = new();
					if(string.IsNullOrWhiteSpace(data.description) == false) {
						att.namedParameters.Add(nameof(PortDescriptionAttribute.description), CG.Value(data.description));
					}
					if(primaryValueOutput == data.id) {
						att.namedParameters.Add(nameof(OutputAttribute.primary), CG.Value(true));
					}
					prop.attributes = new List<CG.AData>() { att };
					prop.getContents = CG.Throw(CG.New(typeof(NotImplementedException)));
					classBuilder.RegisterProperty(prop.GenerateCode());
				}
				else {
					CG.MData method = new CG.MData(data.name, data.type.type);
					method.modifier = new FunctionModifier() {
						Public = true,
						Static = instanceNode == false,
					};

					var parameters = new List<CG.MPData>(valueInputs.Count);
					foreach(var input in valueInputs) {
						parameters.Add(new CG.MPData(input.name, input.type));
					}
					method.parameters = parameters;

					var att = new CG.AData(typeof(OutputAttribute));
					att.namedParameters = new();
					if(string.IsNullOrWhiteSpace(data.description) == false) {
						att.namedParameters.Add(nameof(PortDescriptionAttribute.description), CG.Value(data.description));
					}
					if(primaryValueOutput == data.id) {
						att.namedParameters.Add(nameof(OutputAttribute.primary), CG.Value(true));
					}
					method.attributes = new List<CG.AData>() { att };
					method.code = CG.Flow(parameters.Count > 0 ? CG.Comment("Tips: remove any parameter if not used.") : null, CG.Comment("Insert code here"), CG.Throw(CG.New(typeof(NotImplementedException))));
					classBuilder.RegisterFunction(method.GenerateCode());
				}
			}

			Dictionary<int, string> enumTypeMap = new();
			foreach(var data in flowOutputs) {
				CG.VData variable = new CG.VData(data.name, typeof(FlowPortDefinition));
				variable.modifier = new FieldModifier() {
					Public = true,
					Static = instanceNode == false,
				};
				var att = new CG.AData(typeof(OutputAttribute));
				att.namedParameters = new();

				if(data.kind == FlowOutputKind.Enum) {
					string enumName = uNodeUtility.AutoCorrectName(data.name + "Kind");
					enumTypeMap.Add(data.id, enumName);
					att.namedParameters.Add(nameof(OutputAttribute.type), CG.Typeof(enumName));
					var enums = new CG.ClassData(enumName);
					enums.SetTypeToEnum();
					enums.modifier = new ClassModifier() {
						Public = true,
					};
					var strs = new List<string> {
						CG.Flow(CG.Attribute(typeof(PortDiscardAttribute)), "None,")
					};
					strs.AddRange(data.enums.Select(p => {
						if(string.IsNullOrEmpty(p.description)) {
							return p.name + ",";
						}
						else {
							return CG.Flow(CG.Attribute(typeof(PortDescriptionAttribute), new string[] { CG.Value(p.description) }), p.name + ",");
						}
					}));
					enums.variables = CG.Flow(strs);
					classBuilder.RegisterNestedType(enums.GenerateCode());
				}
				else {
					if(data.kind == FlowOutputKind.Conditional) {
						att.namedParameters.Add(nameof(OutputAttribute.type), CG.Value(typeof(bool)));
					}
					if(string.IsNullOrWhiteSpace(data.description) == false) {
						att.namedParameters.Add(nameof(PortDescriptionAttribute.description), CG.Value(data.description));
					}
					if(primaryFlowOutput == data.id) {
						att.namedParameters.Add(nameof(OutputAttribute.primary), CG.Value(true));
					}
				}
				variable.attributes = new CG.AData[] { att };
				classBuilder.RegisterVariable(variable.GenerateCode());
			}

			foreach(var data in flowInputs) {
				CG.MData method = new CG.MData(data.name, typeof(void));
				method.modifier = new FunctionModifier() {
					Public = true,
					Static = instanceNode == false,
				};

				var parameters = new List<CG.MPData>(valueInputs.Count + flowOutputs.Count);
				foreach(var input in valueInputs) {
					parameters.Add(new CG.MPData(input.name, input.type));
				}
				method.parameters = parameters;

				var att = new CG.AData(typeof(InputAttribute));
				att.namedParameters = new();
				if(string.IsNullOrWhiteSpace(data.description) == false) {
					att.namedParameters.Add(nameof(PortDescriptionAttribute.description), CG.Value(data.description));
				}
				if(primaryFlowInput == data.id) {
					att.namedParameters.Add(nameof(InputAttribute.primary), CG.Value(true));
				}
				method.attributes = new List<CG.AData>() { att };

				if(data.exitKind == ExitKind.Regular) {
					var exit = flowOutputs.FirstOrDefault(p => p.id == data.regularExitID);
					if(exit != null) {
						att.namedParameters.Add(nameof(InputAttribute.exit), CG.Nameof(exit.name));
					}
				}
				else if(data.exitKind == ExitKind.Conditional) {
					var exit = flowOutputs.FirstOrDefault(p => p.id == data.conditionalExitID);
					if(exit != null) {
						parameters.Add(new CG.MPData(exit.name, typeof(bool), RefKind.Out));
					}
				}
				else if(data.exitKind == ExitKind.Enum) {
					var exit = flowOutputs.FirstOrDefault(p => p.id == data.enumExitID);
					if(exit != null) {
						parameters.Add(new CG.MPData(exit.name, RuntimeType.FromMissingType(enumTypeMap[exit.id]), RefKind.Out));
					}
				}

				method.code = CG.Flow(parameters.Count > 0 ? CG.Comment("Tips: remove any parameter if not used.") : null, CG.Comment("Insert code here"), CG.Throw(CG.New(typeof(NotImplementedException))));
				classBuilder.RegisterFunction(method.GenerateCode());
			}

			generatedData.RegisterClass(this, classBuilder);
		}
	}
}
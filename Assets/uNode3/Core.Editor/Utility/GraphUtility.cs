using UnityEngine;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System;
using Object = UnityEngine.Object;
using System.Collections;
using System.IO.Compression;
using System.IO;

namespace MaxyGames.UNode.Editors {
	public static class GraphUtility {
		public const string KEY_TEMP_OBJECT = "[uNode_Temp_";

		#region Copy & Paste
		public static class CopyPaste {
			class CopiedData {
				public SerializedGraph serializedGraph;
				public HashSet<int> IDs;
				public bool isAllNodes;
			}

			struct PasteOption {
				public bool removeOtherConnections;
			}

			private static CopiedData data;

			public static bool HasCopiedData => data != null;
			public static bool IsCopiedNodes => data != null && data.isAllNodes;

			public static void Duplicate(UGraphElement element) {
				var data = DoCopy(element);
				Paste(element.parent, data, new PasteOption() { removeOtherConnections = false });
			}

			public static void Clear() => data = null;

			public static void Copy(params UGraphElement[] elements) {
				data = DoCopy(elements);
			}

			public static UGraphElement[] Paste(UGraphElement parent, bool removeOtherConnections = false) {
				return Paste(parent, data, new PasteOption() {
					removeOtherConnections = removeOtherConnections,
				});
			}

			private static CopiedData DoCopy(params UGraphElement[] elements) {
				if(elements.Length == 0)
					throw new Exception("Must specify the element to copy");
				var elementList = new List<UGraphElement>(elements);
				for(int i = 0; i < elements.Length; i++) {
					var p = elements[i].parent;
					while(p != null) {
						if(elements.Contains(p)) {
							elementList.Remove(elements[i]);
							break;
						}
						p = p.parent;
					}
				}
				var parent = elementList[0].parent;
				for(int i = 0; i < elementList.Count; i++) {
					if(elementList[i].parent != parent) {
						throw new Exception("The copied element must have same parent");
					}
				}
				var data = new CopiedData() {
					serializedGraph = new SerializedGraph(parent.graph),
					IDs = elementList.Select(e => e.id).ToHashSet(),
					isAllNodes = elementList.All(e => e is NodeObject),
				};
				data.serializedGraph.SerializeGraph();
				return data;
			}

			private static UGraphElement[] Paste(UGraphElement parent, CopiedData data, PasteOption option = default) {
				if(data == null)
					throw new Exception("There's no data to paste.");
				var guids = data.IDs;
				data.serializedGraph.DeserializeGraph();
				data.serializedGraph.Graph.owner = parent.graphContainer;
				var elements = data.serializedGraph.Graph.GetObjectsInChildren<UGraphElement>(e => guids.Any(guid => e.id == guid), true).ToArray();

				//var parentOfNode = elements.FirstOrDefault();
				//foreach(var element in parentOfNode) {
				//	if(element is NodeObject node) {
				//		node.EnsureRegistered();
				//	}
				//}

				//Do paste
				var result = DoPaste(parent, elements, option);
				data.serializedGraph.Graph.owner = null;
				//Clean-up data.
				data.serializedGraph.Graph.Destroy();
				//Return the result
				return result;
			}

			private static UGraphElement[] DoPaste(UGraphElement parent, UGraphElement[] elements, PasteOption option = default) {
				//The all copied element including the childrens
				var allElements = new HashSet<UGraphElement>(elements);
				//The all of old guids of object to paste
				var oldGuids = new HashSet<int>();
				//The key map of all guids for mapping the object
				var elementsMap = new Dictionary<int, UGraphElement>();
				for(int i = 0; i < elements.Length; i++) {
					var element = elements[i];

					oldGuids.Add(element.id);
					elementsMap.Add(element.id, element);

					if(element.childCount > 0) {
						//Include the childrens
						foreach(var e in element.GetObjectsInChildren(true)) {
							oldGuids.Add(e.id);
							allElements.Add(e);
							elementsMap.Add(e.id, e);
						}

					}
				}

				bool ValidateReference(UGraphElement graphElement, out UGraphElement validElement) {
					if(elementsMap.TryGetValue(graphElement.id, out var tElement) && tElement.GetType() == graphElement.GetType()) {
						validElement = tElement;
						return true;
					}
					else {
						var matchedElement = parent.GetObjectInChildren<UGraphElement>(e => e.id == graphElement.id);
						if(matchedElement != null && matchedElement.GetType() == graphElement.GetType()) {
							validElement = matchedElement;
							return true;
						}
					}
					validElement = null;
					return false;
				}

				Action postAction = null;

				foreach(var element in allElements) {
					if(element is NodeObject nodeObject) {
						var invalidConnections = new HashSet<Connection>();
						var needUpdateConnections = new HashSet<Connection>();
						foreach(var con in nodeObject.Connections) {
							if(con.Input == null || con.Output == null || con.Input.node == null || con.Output.node == null) {
								invalidConnections.Add(con);
								continue;
							}
							if(!oldGuids.Contains(con.Input.node.id) || !oldGuids.Contains(con.Output.node.id)) {
								if(con is ValueConnection) {
									if(con.Input.node == nodeObject) {
										needUpdateConnections.Add(con);
										continue;
									}
								}
								else if(con is FlowConnection) {
									if(con.Output.node == nodeObject) {
										needUpdateConnections.Add(con);
										continue;
									}
								}
								invalidConnections.Add(con);
							}
						}
						foreach(var con in needUpdateConnections) {
							if(con is ValueConnection) {
								var tGuid = con.Output.node.id;
								NodeObject tNode;
								if(elementsMap.TryGetValue(tGuid, out var tElement) && tElement is NodeObject) {
									tNode = tElement as NodeObject;
								}
								else {
									tNode = parent.GetObjectInChildren<NodeObject>(e => e.id == tGuid);
								}
								if(tNode != null) {
									var tPort = tNode.ValueOutputs.FirstOrDefault(p => p.id == con.Output.id);
									if(tPort != null) {
										con.Output = tPort;
										continue;
									}
								}
							}
							else if(con is FlowConnection) {
								var tGuid = con.Input.node.id;
								NodeObject tNode;
								if(elementsMap.TryGetValue(tGuid, out var tElement) && tElement is NodeObject) {
									tNode = tElement as NodeObject;
								}
								else {
									tNode = parent.GetObjectInChildren<NodeObject>(e => e.id == tGuid);
								}
								if(tNode != null) {
									var tPort = tNode.FlowInputs.FirstOrDefault(p => p.id == con.Input.id);
									if(tPort != null) {
										con.Input = tPort;
										continue;
									}
								}
							}
							//In case the connection cannot be updated then we remove it instead.
							con.Disconnect();
						}
						foreach(var con in invalidConnections) {
							con.Disconnect();
						}

						//Pre validate references
						bool referenceChanged = false;
						var references = nodeObject.serializedData.references;
						for(int i = 0; i < references.Count; i++) {
							if(references[i] is UGraphElement graphElement) {
								if(!oldGuids.Contains(graphElement.id) && ValidateReference(graphElement, out var validElement)) {
									references[i] = validElement;
									referenceChanged = true;
								}
							}
						}

						if(referenceChanged) {
							nodeObject.node = null;
							(nodeObject as ISerializationCallbackReceiver).OnAfterDeserialize();
						}
					}

					Analizer.AnalizeObject(element, obj => {
						if(obj is MemberData mData) {
							if(mData.IsTargetingUNode) {
								for(int i = 0; i < mData.Items.Length; i++) {
									var item = mData.Items[i];
									if(item != null && item.reference != null) {
										var refVal = item.GetReferenceValue();
										if(refVal is UGraphElement graphElement) {
											if(ValidateReference(graphElement, out var validElement)) {
												postAction += () => {
													var reference = item.reference;
													if(reference is UReference) {
														(reference as UReference).SetGraphElement(validElement);
													}
													else {
														item.reference = BaseReference.FromValue(validElement);
													}
												};
											}
										}
										else if(item.reference is ParameterRef parameterRef) {
											if(ValidateReference(parameterRef.reference, out var validElement)) {
												postAction += () => {
													var reference = item.reference;
													if(reference is UReference) {
														(reference as UReference).SetGraphElement(validElement);
													}
													else {
														throw new InvalidOperationException();
													}
												};
											}
										}
									}
								}
							}
							else if(mData.targetType == MemberData.TargetType.Self && parent.graphContainer != null) {
								var self = mData.Get(null);
								if(self != parent.graphContainer) {
									mData.CopyFrom(MemberData.This(parent.graphContainer));
								}
							}
						}
						return false;
					});
				}

				if(elements.Length > 0) {
					elements[0].graph.owner = null;
				}
				//We just need to set the parent of all root of the copied elements.
				for(int i = 0; i < elements.Length; i++) {
					elements[i].SetParent(parent);
				}

				postAction?.Invoke();
				//Post validate references
				foreach(var element in allElements) {
					if(element is NodeObject nodeObject) {
						bool referenceChanged = false;
						var references = nodeObject.serializedData.references;
						for(int i = 0; i < references.Count; i++) {
							if(references[i] is MemberData mData) {
								if(mData.Items != null) {
									foreach(var item in mData.Items) {
										if(item.reference?.ReferenceValue is UGraphElement referenceElement) {
											if(ValidateReference(referenceElement, out var validReference)) {
												var reference = item.reference;
												if(reference is UReference) {
													(reference as UReference).SetGraphElement(validReference);
												}
												else {
													item.reference = BaseReference.FromValue(validReference);
												}
												referenceChanged = true;
											}
										}
									}
								}
							}
						}
						if(referenceChanged) {
							nodeObject.node = null;
							(nodeObject as ISerializationCallbackReceiver).OnAfterDeserialize();
						}
					}
				}

				if(option.removeOtherConnections) {
					foreach(var element in allElements) {
						if(element is NodeObject nodeObject) {
							foreach(var con in nodeObject.Connections.ToArray()) {
								if(!allElements.Contains(con.Input.node) || !allElements.Contains(con.Output.node)) {
									con.Disconnect();
								}
							}
						}
					}
				}

				//Return the result
				return elements;
			}
		}
		#endregion

		#region Error Checks
		public static class ErrorChecker {
			internal static GraphErrorChecker defaultAnalizer = new GraphErrorChecker();

			internal class GraphErrorChecker : ErrorAnalyzer {
				internal Dictionary<IGraph, Dictionary<UGraphElement, uNodeUtility.GraphErrorData>> graphErrors = new Dictionary<IGraph, Dictionary<UGraphElement, uNodeUtility.GraphErrorData>>();

				public override void ClearErrors(UGraphElement element) {
					if(element == null)
						return;
					var graph = element.graphContainer;
					if(graph == null)
						return;
					if(graphErrors.TryGetValue(graph, out var map)) {
						if(map.TryGetValue(element, out var errorData)) {
							errorData.ClearErrors();
						}
					}
				}

				public override void ClearErrors(IGraph graph) {
					if(graph == null)
						return;
					if(graphErrors.TryGetValue(graph, out var map)) {
						map.Clear();
					}
				}

				public override void ClearErrors() {
					graphErrors.Clear();
				}

				public override void CheckErrors(IGraph graph) {
					CheckGraphErrors(graph, this);
				}

				public override void RegisterError(UGraphElement element, uNodeUtility.ErrorMessage error) {
					if(element == null)
						throw new ArgumentNullException(nameof(element));
					var graph = element.graphContainer;
					if(graph == null)
						throw new Exception("The container graph is null");
					if(!graphErrors.TryGetValue(graph, out var map)) {
						map = new Dictionary<UGraphElement, uNodeUtility.GraphErrorData>();
						graphErrors[graph] = map;
					}
					if(!map.TryGetValue(element, out var errorData)) {
						errorData = new uNodeUtility.GraphErrorData() {
							element = new UGraphElementRef(element),
						};
						map[element] = errorData;
					}
					errorData.AddError(error);
				}
			}

			public static void DrawErrorMessages(UGraphElement element) {
				var errors = ErrorChecker.GetErrorMessages(element);
				{
					EditorGUILayout.Space();
					foreach(var e in errors) {
						var errorRect = EditorGUILayout.BeginVertical();
						var message = e.niceMessage;
						if(e.autoFix != null) {
							message += "\n[Click to fix]";
						}
						EditorGUILayout.HelpBox(message, (MessageType)e.type);
						EditorGUILayout.EndVertical();
						if(e.autoFix != null && Event.current.button == 0 && Event.current.type == EventType.MouseDown && errorRect.Contains(Event.current.mousePosition)) {
							e.autoFix(Event.current.mousePosition);
						}
					}
				}
			}

			public static IEnumerable<uNodeUtility.ErrorMessage> GetErrorMessages(UGraphElement element, InfoType type) {
				if(element == null)
					return Enumerable.Empty<uNodeUtility.ErrorMessage>();
				var graph = element.graphContainer;
				if(graph == null)
					return null;
				if(defaultAnalizer.graphErrors.TryGetValue(graph, out var map)) {
					if(map.TryGetValue(element, out var graphError)) {
						return graphError.GetErrors(type);
					}

				}
				return Enumerable.Empty<uNodeUtility.ErrorMessage>();
			}

			public static IEnumerable<uNodeUtility.ErrorMessage> GetErrorMessages(UGraphElement element) {
				if(element == null)
					return Enumerable.Empty<uNodeUtility.ErrorMessage>();
				var graph = element.graphContainer;
				if(graph == null)
					return null;
				if(defaultAnalizer.graphErrors.TryGetValue(graph, out var map)) {
					if(map.TryGetValue(element, out var graphError)) {
						return graphError.GetErrors();
					}

				}
				return Enumerable.Empty<uNodeUtility.ErrorMessage>();
			}

			public static void RegisterErrorMessage(UGraphElement element, uNodeUtility.ErrorMessage error) {
				defaultAnalizer.RegisterError(element, error);
			}

			public static void ClearErrorMessages() {
				defaultAnalizer.ClearErrors();
			}

			public static void ClearErrorMessages(IGraph graph) {
				defaultAnalizer.ClearErrors(graph);
			}

			public static void ClearErrorMessages(UGraphElement element) {
				defaultAnalizer.ClearErrors(element);
			}

			public static void CheckGraphErrors(IList<Object> graphs = null, bool checkProjectGraphs = true) {
				HashSet<Object> allGraphs = new HashSet<Object>();
				if(graphs != null) {
					foreach(var g in graphs) {
						allGraphs.Add(g);
					}
				}
				if(checkProjectGraphs) {
					var graphAssets = GraphUtility.FindAllGraphAssets();
					foreach(var asset in graphAssets) {
						if(asset != null && (asset is IGraph || asset is IScriptGraph)) {
							allGraphs.Add(asset);
						}
					}
				}
				if(allGraphs.Count > 0) {
					var analizer = defaultAnalizer;
					ClearErrorMessages();
					foreach(var g in allGraphs) {
						if(g is IGraph graph) {
							CheckGraphErrors(graph, analizer);
						}
						else if(g is IScriptGraph scriptGraph) {
							foreach(var type in scriptGraph.TypeList.references) {
								if(type is IGraph gr) {
									CheckGraphErrors(gr, analizer);
								}
							}
						}
					}
					if(analizer.graphErrors != null && analizer.graphErrors.Count > 0) {
						ErrorCheckWindow.ShowWindow();
						ErrorCheckWindow.UpdateErrorMessages();
						return;
					}
				}
				uNodeEditorUtility.DisplayMessage("", "No error found.");
			}

			private static List<UGraphElement> _cachedErrorChecks = new List<UGraphElement>(32);
			private static void CheckGraphErrors(IGraph graph, ErrorAnalyzer errorAnalizer) {
				if(graph == null) return;
				var graphData = graph.GraphData;
				if(graphData != null) {
					{
						var analizers = NodeEditorUtility.GetGraphAnalizers(graph.GetType());
						foreach(var analizer in analizers) {
							try {
								analizer.CheckGraphErrors(errorAnalizer, graph);
							}
							catch(Exception ex) {
								errorAnalizer.RegisterError(graphData, ex.ToString());
							}
						}
					}
					_cachedErrorChecks.Clear();
					var elements = _cachedErrorChecks;
					graphData.ForeachInChildrens((element) => {
						elements.Add(element);
					}, true);
					foreach(var e in elements) {
						var elementAnalizers = NodeEditorUtility.GetGraphElementAnalizers(e.GetType());
						foreach(var analizer in elementAnalizers) {
							try {
								analizer.CheckElementErrors(errorAnalizer, e);
							}
							catch(Exception ex) {
								errorAnalizer.RegisterError(e, ex.ToString());
							}
						}
						if(e is NodeObject nodeObject && nodeObject.node != null) {
							var nodeAnalizers = NodeEditorUtility.GetNodeAnalizers(nodeObject.node.GetType());
							foreach(var analizer in nodeAnalizers) {
								try {
									analizer.CheckNodeErrors(errorAnalizer, nodeObject.node);
								}
								catch(Exception ex) {
									errorAnalizer.RegisterError(e, ex.ToString());
								}
							}
						}
					}
				}
			}
		}
		#endregion

		#region Analizer
		internal static class Analizer {
			/// <summary>
			/// Perform field reflection in obj.
			/// </summary>
			/// <param name="obj">The object to analize</param>
			/// <param name="validation">The analize validation, should return true when condition is meet or you change something.</param>
			/// <param name="doAction">The action to perform when the validation is true</param>
			/// <returns>True when validation is valid</returns>
			public static bool AnalizeObject(object obj, Func<object, bool> validation, Action<object> doAction = null) {
				if(object.ReferenceEquals(obj, null) || validation == null)
					return false;
				if(!(obj is UnityEngine.Object) && validation(obj)) {
					if(doAction != null)
						doAction(obj);
					if(obj is MemberData) {
						var mInstane = (obj as MemberData).instance;
						if(mInstane != null && !(mInstane is Object)) {
							if(AnalizeObject(mInstane, validation, doAction)) {
								//This make sure to serialize the data.
								(obj as MemberData).instance = mInstane;
							}
						}
					}
					return true;
				}
				if(obj is MemberData) {
					MemberData mData = obj as MemberData;
					if(mData != null && mData.instance != null && !(mData.instance is UnityEngine.Object)) {
						bool flag = AnalizeObject(mData.instance, validation, doAction);
						if(flag) {
							//This make sure to serialize the data.
							mData.instance = mData.instance;
						}
						return flag;
					}
					return false;
				}
				bool changed = false;
				if(obj is IGraph) {
					var graphData = (obj as IGraph).GraphData;
					foreach(var element in graphData.GetObjectsInChildren(true)) {
						changed = AnalizeObject(element, validation, doAction) || changed;
					}
				}
				else if(obj is NodeObject) {
					var nodeObject = obj as NodeObject;
					var references = nodeObject.serializedData.references;
					foreach(var reference in references) {
						if(reference is not Object/* || reference is not IGraphElement*/) {
							changed = AnalizeObject(reference, validation, doAction) || changed;
						}
					}
					foreach(var port in nodeObject.ValueInputs) {
						if(port != null && port.UseDefaultValue) {
							changed = AnalizeObject(port.defaultValue, validation, doAction) || changed;
						}
					}
				}
				bool ValidateReference(object reference) {
					if(reference is UnityEngine.Object || reference is IGraphElement) {
						if(validation(reference)) {
							if(doAction != null)
								doAction(reference);
							changed = true;
						}
						return true;
					}
					return false;
				}
				if(obj is IList) {
					IList list = obj as IList;
					for(int i = 0; i < list.Count; i++) {
						object element = list[i];
						if(element == null)
							continue;
						if(ValidateReference(element)) {
							continue;
						}
						changed = AnalizeObject(element, validation, doAction) || changed;
					}
					return changed;
				}
				FieldInfo[] fieldInfo = ReflectionUtils.GetFieldsCached(obj.GetType());
				foreach(FieldInfo field in fieldInfo) {
					Type fieldType = field.FieldType;
					if(!fieldType.IsClass)
						continue;
					if(!IsValidField(field))
						continue;
					object value = field.GetValueOptimized(obj);
					if(object.ReferenceEquals(value, null))
						continue;
					if(ValidateReference(value)) {
						continue;
					}
					changed = AnalizeObject(value, validation, doAction) || changed;
				}
				return changed;
			}

			private static bool IsValidField(FieldInfo field) {
				if(field.IsPublic) {
					return !field.IsNotSerialized;
				}
				if(field.IsDefined(typeof(SerializeField)) || field.IsDefined(typeof(SerializeReference))) {
					return true;
				}
				return false;
			}
		}

		public static List<object> SearchReferences(Func<UGraphElement, bool> searchValidation) {
			List<object> references = new List<object>();
			var assets = GraphUtility.FindAllGraphAssets();

			void Validate(IGraph graph) {
				var graphData = graph.GraphData;
				graphData.ForeachInChildrens(element => {
					if(searchValidation(element)) {
						references.Add(element);
					}
				}, true);
			}

			foreach(var asset in assets) {
				if(asset is IScriptGraph scriptGraph) {
					foreach(var scriptType in scriptGraph.TypeList) {
						if(scriptType is IGraph graph) {
							try {
								Validate(graph);
							}
							catch(System.Exception ex) {
								Debug.LogException(ex, scriptType);
							}
						}
					}
				}
				else if(asset is IGraph graph) {
					try {
						Validate(graph);
					}
					catch(System.Exception ex) {
						Debug.LogException(ex, asset);
					}
				}
			}
			references = references.Distinct().ToList();
			return references;
		}

		/// <summary>
		/// Find references of MemberInfo from all graphs in the project.
		/// </summary>
		/// <param name="memberInfo"></param>
		/// <returns></returns>
		public static List<object> FindReferences(MemberInfo memberInfo) {
			List<object> references = new List<object>();
			var assets = GraphUtility.FindAllGraphAssets();

			void Validate(IGraph graph) {
				var graphData = graph.GraphData;
				graphData.ForeachInChildrens(element => {
					if(Analizer.AnalizeObject(element, ValidateValue)) {
						references.Add(element);
					}
				}, true);
			}

			bool ValidateValue(object obj) {
				MemberData member = obj as MemberData;
				if(member != null) {
					if(memberInfo is Type) {
						if(member.startType == memberInfo || member.type == memberInfo) {
							return true;
						}
					}
					var members = member.GetMembers(false);
					if(members != null) {
						for(int i = 0; i < members.Length; i++) {
							var m = members[i];
							if(members.Length == i + 1) {
								if(m == memberInfo) {
									return true;
								}
							}
						}
					}
				}
				return false;
			};

			foreach(var asset in assets) {
				if(asset is IScriptGraph scriptGraph) {
					foreach(var scriptType in scriptGraph.TypeList) {
						if(scriptType is IGraph graph) {
							try {
								Validate(graph);
							}
							catch(System.Exception ex) {
								Debug.LogException(ex, scriptType);
							}
						}
					}
				}
				else if(asset is IGraph graph) {
					try {
						Validate(graph);
					}
					catch(System.Exception ex) {
						Debug.LogException(ex, asset);
					}
				}
			}
			references = references.Distinct().ToList();

			if(memberInfo is INativeMember) {
				var nativeMember = (memberInfo as INativeMember).GetNativeMember();
				if(nativeMember != null) {
					references.AddRange(FindReferences(nativeMember));
				}
			}

			return references;
		}

		public static List<object> FindVariableUsages(Variable variable, bool allGraphs = true) {
			if(variable == null)
				throw new ArgumentNullException(nameof(variable));
			var references = new List<object>();
			var graph = variable.graphContainer;
			if(graph == null)
				return references;

			bool Validate(object obj) {
				MemberData member = obj as MemberData;
				if(member != null && member.targetType == MemberData.TargetType.uNodeVariable) {
					if(member.startItem.GetReference<UReference>()?.GetGraphElement() == variable) {
						return true;
					}
				}
				return false;
			};

			var graphData = graph.GraphData;
			graphData.ForeachInChildrens(element => {
				if(Analizer.AnalizeObject(element, Validate)) {
					references.Add(element);
				}
			}, true);
			if(allGraphs) {
				var runtimeInfo = ReflectionUtils.GetRuntimeType(graph)?.GetField(variable.name);
				if(runtimeInfo != null) {
					references.AddRange(FindReferences(runtimeInfo));
				}
			}
			references = references.Distinct().ToList();
			return references;
		}

		public static List<object> FindPropertyUsages(Property property, bool allGraphs = true) {
			if(property == null)
				throw new ArgumentNullException(nameof(property));
			var references = new List<object>();
			var graph = property.graphContainer;
			if(graph == null)
				return references;
			bool Validate(object obj) {
				MemberData member = obj as MemberData;
				if(member != null && member.targetType == MemberData.TargetType.uNodeProperty) {
					if(member.startItem.GetReference<UReference>()?.GetGraphElement() == property) {
						return true;
					}
				}
				return false;
			};

			var graphData = graph.GraphData;
			graphData.ForeachInChildrens(element => {
				if(Analizer.AnalizeObject(element, Validate)) {
					references.Add(element);
				}
			}, true);
			if(allGraphs) {
				var runtimeInfo = ReflectionUtils.GetRuntimeType(graph)?.GetProperty(property.name);
				if(runtimeInfo != null) {
					references.AddRange(FindReferences(runtimeInfo));
				}
			}
			references = references.Distinct().ToList();
			return references;
		}

		public static List<object> FindFunctionUsages(Function function, bool allGraphs = true) {
			if(function == null)
				throw new ArgumentNullException(nameof(function));
			var references = new List<object>();
			var graph = function.graphContainer;
			if(graph == null)
				return references;
			bool Validate(object obj) {
				MemberData member = obj as MemberData;
				if(member != null && member.targetType == MemberData.TargetType.uNodeFunction) {
					if(member.startItem.GetReference<UReference>()?.GetGraphElement() == function) {
						return true;
					}
				}
				return false;
			};

			var graphData = graph.GraphData;
			graphData.ForeachInChildrens(element => {
				if(Analizer.AnalizeObject(element, Validate)) {
					references.Add(element);
				}
			}, true);
			if(allGraphs) {
				var runtimeInfo = ReflectionUtils.GetRuntimeType(graph)?.GetMethod(function.name, function.parameters.Select(p => p.Type).ToArray());
				if(runtimeInfo != null) {
					references.AddRange(FindReferences(runtimeInfo));
				}
			}
			references = references.Distinct().ToList();
			return references;
		}

		public static List<object> FindLocalVariableUsages(Variable variable) {
			if(variable == null)
				throw new ArgumentNullException(nameof(variable));
			var references = new List<object>();
			var graph = variable.graphContainer;
			if(graph == null)
				return references;

			bool Validate(object obj) {
				MemberData member = obj as MemberData;
				if(member != null && (member.targetType == MemberData.TargetType.uNodeLocalVariable)) {
					if(member.startItem.GetReference<UReference>()?.GetGraphElement() == variable) {
						return true;
					}
				}
				return false;
			};

			var graphData = graph.GraphData;
			graphData.ForeachInChildrens(element => {
				if(Analizer.AnalizeObject(element, Validate)) {
					references.Add(element);
				}
			}, true);
			references = references.Distinct().ToList();
			return references;
		}



		public static List<object> FindNodeUsages(Func<Node, bool> validation) {
			var references = new List<object>();
			var assets = GraphUtility.FindAllGraphAssets();

			void Validate(IGraph graph) {
				var graphData = graph.GraphData;
				graphData.ForeachInChildrens(element => {
					if(element is NodeObject nodeObject && nodeObject.node != null) {
						if(validation(nodeObject.node)) {
							references.Add(element);
						}
					}
				}, true);
			}

			foreach(var asset in assets) {
				if(asset is IScriptGraph scriptGraph) {
					foreach(var scriptType in scriptGraph.TypeList) {
						if(scriptType is IGraph graph) {
							Validate(graph);
						}
					}
				}
				else if(asset is IGraph graph) {
					Validate(graph);
				}
			}
			return references;
		}

		public static List<object> FindNodeUsages(Type type) {
			var references = new List<object>();
			var assets = GraphUtility.FindAllGraphAssets();

			void Validate(IGraph graph) {
				var graphData = graph.GraphData;
				graphData.ForeachInChildrens(element => {
					if(element is NodeObject nodeObject && nodeObject.node != null) {
						if(nodeObject.node.GetType() == type) {
							references.Add(element);
						}
						else if(nodeObject.node is Nodes.HLNode hlNode && hlNode.type == type) {
							references.Add(element);
						}
					}
				}, true);
			}

			foreach(var asset in assets) {
				if(asset is IScriptGraph scriptGraph) {
					foreach(var scriptType in scriptGraph.TypeList) {
						if(scriptType is IGraph graph) {
							Validate(graph);
						}
					}
				}
				else if(asset is IGraph graph) {
					Validate(graph);
				}
			}
			return references;
		}

		/// <summary>
		/// Show specific variable usage in window
		/// </summary>
		/// <param name="graph"></param>
		/// <param name="name"></param>
		/// <param name="allGraphs"></param>
		public static void ShowVariableUsages(Variable variable, bool allGraphs = true) {
			if(variable == null)
				throw new ArgumentNullException(nameof(variable));
			var references = FindVariableUsages(variable, allGraphs);
			ShowReferencesInWindow(references);
		}

		/// <summary>
		/// Show node usages
		/// </summary>
		/// <param name="validation"></param>
		public static void ShowNodeUsages(Func<Node, bool> validation) {
			var references = FindNodeUsages(validation);
			ShowReferencesInWindow(references);
		}

		/// <summary>
		/// Show specific node usage in window
		/// </summary>
		/// <param name="type"></param>
		public static void ShowNodeUsages(Type type) {
			var references = FindNodeUsages(type);
			ShowReferencesInWindow(references);
		}

		/// <summary>
		/// Show inheritance heirarchy for graphs
		/// </summary>
		/// <param name="graph"></param>
		/// <exception cref="ArgumentNullException"></exception>
		public static void ShowGraphInheritanceHeirarchy(IGraph graph) {
			if(graph is null) {
				throw new ArgumentNullException(nameof(graph));
			}
			if(uNodeEditorUtility.DisplayRequiredProVersion()) {
				return;
			}
			List<object> references = new List<object>();
			{
				var type = graph.GetGraphType();
				if(type != null && type != graph.GetGraphInheritType()) {
					List<Type> list = new List<Type>();
					Type inheritType = type.BaseType;
					while(inheritType != null) {
						list.Insert(0, inheritType);
						inheritType = inheritType.BaseType;
					}
					list.Add(graph.GetGraphType());

					var assets = GraphUtility.FindAllGraphAssets();
					foreach(var asset in assets) {
						if(asset is IScriptGraph scriptGraph) {
							foreach(var scriptType in scriptGraph.TypeList) {
								if(scriptType is IGraph g) {
									var graphType = g.GetGraphType();
									if(graphType != null && graphType.IsSubclassOf(type)) {
										list.Add(graphType);
									}
								}
							}
						}
						else if(asset is IGraph g) {
							var graphType = g.GetGraphType();
							if(graphType != null && graphType.IsSubclassOf(type)) {
								list.Add(graphType);
							}
						}
					}
					var hierarchy = new ReferenceTree.TypeHierarchy();
					hierarchy.type = list[0];
					list.RemoveAt(0);

					bool Recursive(ReferenceTree.TypeHierarchy hierarchy, Type type) {
						if(hierarchy.type == type.BaseType) {
							hierarchy.nestedTypes.Add(new ReferenceTree.TypeHierarchy() { type = type });
							return true;
						}
						else {
							foreach(var nested in hierarchy.nestedTypes) {
								if(Recursive(nested, type)) {
									return true;
								}
							}
						}
						return false;
					}

					while(list.Count > 0) {
						for(int i=0;i<list.Count;i++) {
							if(Recursive(hierarchy, list[i])) {
								list.RemoveAt(i);
								i--;
							}
						}
					}

					references.Add(hierarchy);
				}
			}
			if(references.Count > 0) {
				GUIStyle selectedStyle = new GUIStyle(EditorStyles.label);
				selectedStyle.normal.textColor = Color.white;
				var tree = new ReferenceTree(references);
				var win = ActionWindow.ShowWindow(() => {
					tree.OnGUI(GUILayoutUtility.GetRect(0, 100000, 0, 100000));
				});
				win.titleContent = new GUIContent("Found: " + references.Count + " references");
			}
			else {
				uNodeEditorUtility.DisplayMessage("", "No references found.");
			}
		}

		/// <summary>
		/// Show specific property usage in window
		/// </summary>
		/// <param name="graph"></param>
		/// <param name="name"></param>
		/// <param name="allGraphs"></param>
		public static void ShowPropertyUsages(Property property, bool allGraphs = true) {
			if(property is null) {
				throw new ArgumentNullException(nameof(property));
			}

			var references = FindPropertyUsages(property, allGraphs);
			ShowReferencesInWindow(references);
		}

		/// <summary>
		/// Show specific function usage in window
		/// </summary>
		/// <param name="graph"></param>
		/// <param name="name"></param>
		/// <param name="allGraphs"></param>
		public static void ShowFunctionUsages(Function function, bool allGraphs = true) {
			if(function is null) {
				throw new ArgumentNullException(nameof(function));
			}

			var references = FindFunctionUsages(function, allGraphs);
			ShowReferencesInWindow(references);
		}

		/// <summary>
		/// Show specific local variable usage in window
		/// </summary>
		/// <param name="root"></param>
		/// <param name="name"></param>
		public static void ShowLocalVariableUsages(Variable variable) {
			if(variable is null) {
				throw new ArgumentNullException(nameof(variable));
			}

			var references = FindLocalVariableUsages(variable);
			ShowReferencesInWindow(references);
		}

		//TODO: fix me
#if false
		/// <summary>
		/// Show specific parameter usage in window
		/// </summary>
		/// <param name="root"></param>
		/// <param name="name"></param>
		public static void ShowParameterUsages(RootObject root, string name) {
			if(root == null)
				return;
			var references = new List<Object>();
			var scripts = root.GetComponentsInChildren<MonoBehaviour>(true);
			Func<object, bool> scriptValidation = (obj) => {
				MemberData member = obj as MemberData;
				if(member != null) {
					if(member.targetType == MemberData.TargetType.uNodeParameter && member.startName == name) {
						return true;
					}
				}
				return false;
			};
			Array.ForEach(scripts, script => {
				bool flag = AnalizerUtility.AnalizeObject(script, scriptValidation);
				if(flag) {
					references.Add(script);
				}
			});
			ShowReferencesInWindow(references);
		}

		/// <summary>
		/// Show specific generic parameter usage in window
		/// </summary>
		/// <param name="root"></param>
		/// <param name="name"></param>
		public static void ShowGenericParameterUsages(RootObject root, string name) {
			if(root == null)
				return;
			var references = new List<Object>();
			var scripts = root.GetComponentsInChildren<MonoBehaviour>(true);
			Func<object, bool> scriptValidation = (obj) => {
				MemberData member = obj as MemberData;
				if(member != null) {
					if(member.targetType == MemberData.TargetType.uNodeGenericParameter && member.startName == name) {
						return true;
					}
				}
				return false;
			};
			Array.ForEach(scripts, script => {
				bool flag = AnalizerUtility.AnalizeObject(script, scriptValidation);
				if(flag) {
					references.Add(script);
				}
			});
			ShowReferencesInWindow(references);
		}
#endif


		/// <summary>
		/// Find references of MemberInfo from all graphs in the project and show it in window.
		/// </summary>
		/// <param name="info"></param>
		public static void ShowMemberUsages(MemberInfo info) {
			var references = FindReferences(info);
			ShowReferencesInWindow(references);
		}

		internal class ReferenceTree : TreeView {
			private List<object> m_references;
			public List<object> references {
				get => m_references;
				set {
					m_references = value;
					for(int i = 0; i < m_references.Count; i++) {
						if(m_references[i] is UGraphElement element) {
							m_references[i] = new UGraphElementRef(element);
						}
					}
					Reload();
				}
			}
			public Dictionary<IGraph, Dictionary<UGraphElement, uNodeUtility.GraphErrorData>> errors;

			class ReferenceTreeView : TreeViewItem {
				public UGraphElement reference;
				public List<(string, Texture)> paths;

				public ReferenceTreeView(UGraphElement reference) : base(uNodeEditorUtility.GetUIDFromString("R:" + (reference.graphContainer as Object).GetInstanceID() + "-" + reference.id), -1, reference.name) {
					this.reference = reference;
					paths = ErrorCheckWindow.GetElementPathWithIcon(reference, richText: true);
				}
			}

			public class TypeHierarchy {
				public Type type;
				public List<TypeHierarchy> nestedTypes = new List<TypeHierarchy>();
			}

			class ErrorTreeView : TreeViewItem {
				public uNodeUtility.ErrorMessage error;

				public ErrorTreeView(uNodeUtility.ErrorMessage error) : base(error.GetHashCode(), -1, error.message) {
					this.error = error;
				}
			}

			public ReferenceTree(List<object> references) : base(new TreeViewState()) {
				showBorder = true;
				showAlternatingRowBackgrounds = true;
				this.references = new List<object>(references);
			}

			public ReferenceTree(Dictionary<IGraph, Dictionary<UGraphElement, uNodeUtility.GraphErrorData>> errors) : base(new TreeViewState()) {
				this.errors = errors;
				showBorder = true;
				showAlternatingRowBackgrounds = true;
				Reload();
			}

			protected override TreeViewItem BuildRoot() {
				var root = new TreeViewItem { id = 0, depth = -1 };
				if(references != null) {
					var map = new Dictionary<Object, HashSet<object>>();
					foreach(var r in references) {
						if(r == null)
							continue;
						if(r is Object unityObject) {
							if(unityObject == null)
								continue;
							if(!map.TryGetValue(unityObject, out var list)) {
								list = new HashSet<object>();
								map[unityObject] = list;
							}
							list.Add(unityObject);
						}
						else if(r is UReference uReference) {
							var element = uReference.GetGraphElement();
							if(element == null)
								continue;
							var owner = element.graphContainer as Object;
							if(owner == null)
								continue;
							if(!map.TryGetValue(owner, out var list)) {
								list = new HashSet<object>();
								map[owner] = list;
							}
							list.Add(r);
						}
						else {
							if(r is Object) {
								var value = r as Object;
								root.AddChild(new TreeViewItem(value.GetInstanceID(), -1, uNodeUtility.GetObjectName(value)) {
									icon = uNodeEditorUtility.GetTypeIcon(value) as Texture2D
								});
							}
							else if(r is Type) {
								var value = r as Type;
								root.AddChild(new TypeTreeView(value) {
									displayName = value.FullName,
									icon = uNodeEditorUtility.GetTypeIcon(value) as Texture2D
								});
							}
							else if(r is TypeHierarchy hierarchy) {
								void Recursive(TypeHierarchy hierarchy, TreeViewItem tree) {
									var value = hierarchy.type;
									var parent = new TypeTreeView(value) {
										displayName = value.FullName,
										icon = uNodeEditorUtility.GetTypeIcon(value) as Texture2D
									};
									tree.AddChild(parent);
									SetExpanded(parent.id, true);
									foreach(var nested in hierarchy.nestedTypes) {
										Recursive(nested, parent);
									}
								}
								Recursive(hierarchy, root);
							}
						}
					}
					foreach(var pair in map) {
						if(pair.Value.Count > 0) {
							if(pair.Value.Count == 1 && pair.Value.Contains(pair.Key)) {
								//TODO: fix me
								var tree = new TreeViewItem(pair.Key.GetInstanceID(), -1, uNodeUtility.GetObjectName(pair.Key)) {
									icon = uNodeEditorUtility.GetTypeIcon(pair.Key) as Texture2D
								};
								root.AddChild(tree);
							}
							else {
								var tree = new TreeViewItem(pair.Key.GetInstanceID(), -1, uNodeUtility.GetObjectName(pair.Key)) {
									icon = uNodeEditorUtility.GetTypeIcon(pair.Key) as Texture2D
								};
								foreach(var val in pair.Value) {
									if(val is UReference reference && reference.GetGraphElement() is UGraphElement element) {
										tree.AddChild(new ReferenceTreeView(element));
									}
								}
								root.AddChild(tree);
								SetExpanded(tree.id, true);
							}
						}
					}
				}
				else if(errors != null) {
					foreach(var pair in errors) {
						if(pair.Key == null)
							continue;
						if(pair.Key is Object obj && pair.Value.Count > 0) {
							var tree = new TreeViewItem(obj.GetInstanceID(), -1, pair.Key.GetGraphName()) {
								icon = uNodeEditorUtility.GetTypeIcon(pair.Key) as Texture2D
							};
							foreach(var (element, errorData) in pair.Value) {
								var reference = new ReferenceTreeView(element);
								foreach(var error in errorData.GetErrors(InfoType.Error)) {
									reference.AddChild(new ErrorTreeView(error));
								}
								tree.AddChild(reference);
								SetExpanded(reference.id, true);
							}
							root.AddChild(tree);
							SetExpanded(tree.id, true);
						}
					}
				}
				if(root.children != null) {
					root.children.Sort((x, y) => string.Compare(x.displayName, y.displayName));
				}
				else {
					root.children = new List<TreeViewItem>();
				}
				SetupDepthsFromParentsAndChildren(root);
				return root;
			}

			protected override void RowGUI(RowGUIArgs args) {
				Event evt = Event.current;
				if(evt.type == EventType.Repaint) {
					if(args.item is ReferenceTreeView tree) {
						args.label = "";
						Rect labelRect = args.rowRect;
						labelRect.x += GetContentIndent(args.item);
						var style = uNodeGUIStyle.itemNormal;
						bool flag = false;
						foreach(var (path, icon) in tree.paths) {
							if(flag) {
								uNodeGUIStyle.itemNext.Draw(new Rect(labelRect.x, labelRect.y, 13, 16), GUIContent.none, false, false, false, false);
								labelRect.x += 13;
								labelRect.width -= 13;
							}
							flag = true;
							if(icon != null) {
								GUI.DrawTexture(new Rect(labelRect.x, labelRect.y, 16, 16), icon);
								labelRect.x += 16;
								labelRect.width -= 16;
							}
							var content = new GUIContent(path);
							style.Draw(labelRect, content, false, false, false, false);
							labelRect.x += style.CalcSize(content).x;
						}
					}
				}
				else if(evt.type == EventType.MouseDown && args.rowRect.Contains(evt.mousePosition)) {
					if(args.item is ReferenceTreeView reference) {
						if(evt.button == 1) {
							//GenericMenu menu = new GenericMenu();
							//menu.AddItem(new GUIContent("Highlight Node"), false, () => {
							//	uNodeEditor.HighlightNode(errors.Key);
							//});
							//menu.AddItem(new GUIContent("Select Node"), false, () => {
							//	uNodeEditor.ChangeSelection(errors.Key, true);
							//});
							//menu.ShowAsContext();
						}
						else if(evt.button == 0 && evt.clickCount == 2) {
							if(reference.reference is NodeObject nodeObject) {
								uNodeEditor.HighlightNode(nodeObject);
							}
							else {
								//TODO: highlight element
							}
						}
					}
				}
				base.RowGUI(args);
			}
		}

		private static void ShowErrorsInWindow(Dictionary<IGraph, Dictionary<UGraphElement, uNodeUtility.GraphErrorData>> errors) {
			if(errors.Count > 0) {
				GUIStyle selectedStyle = new GUIStyle(EditorStyles.label);
				selectedStyle.normal.textColor = Color.white;
				var tree = new ReferenceTree(errors);
				var win = ActionWindow.ShowWindow(() => {
					tree.OnGUI(GUILayoutUtility.GetRect(0, 100000, 0, 100000));
				});
				win.titleContent = new GUIContent("Error Checkers");
			}
			else {
				uNodeEditorUtility.DisplayMessage("", "No error found.");
			}
		}

		private static void ShowReferencesInWindow(List<object> references) {
			if(uNodeEditorUtility.DisplayRequiredProVersion()) {
				return;
			}
			if(references.Count > 0) {
				GUIStyle selectedStyle = new GUIStyle(EditorStyles.label);
				selectedStyle.normal.textColor = Color.white;
				var tree = new ReferenceTree(references);
				var win = ActionWindow.ShowWindow(() => {
					tree.OnGUI(GUILayoutUtility.GetRect(0, 100000, 0, 100000));
				});
				win.titleContent = new GUIContent("Found: " + references.Count + " references");
			}
			else {
				uNodeEditorUtility.DisplayMessage("", "No references found.");
			}
		}
		#endregion

		private static List<GraphSystemAttribute> _graphSystems;
		/// <summary>
		/// Find all graph system attributes.
		/// </summary>
		/// <returns></returns>
		public static List<GraphSystemAttribute> FindGraphSystemAttributes() {
			if(_graphSystems == null) {
				_graphSystems = new List<GraphSystemAttribute>();
				foreach(var assembly in EditorReflectionUtility.GetAssemblies()) {
					try {
						foreach(System.Type type in EditorReflectionUtility.GetAssemblyTypes(assembly)) {
							if(type.IsDefined(typeof(GraphSystemAttribute), false)) {
								var menuItem = (GraphSystemAttribute)type.GetCustomAttributes(typeof(GraphSystemAttribute), false)[0];
								menuItem.type = type;
								_graphSystems.Add(menuItem);
							}
						}
					}
					catch { continue; }
				}
			}
			return _graphSystems;
		}

		private static List<GraphConverter> _graphConverters;
		/// <summary>
		/// Find all available graph converters
		/// </summary>
		/// <returns></returns>
		public static List<GraphConverter> FindGraphConverters() {
			if(_graphConverters == null) {
				_graphConverters = new List<GraphConverter>();
				foreach(var assembly in EditorReflectionUtility.GetAssemblies()) {
					try {
						foreach(System.Type type in EditorReflectionUtility.GetAssemblyTypes(assembly)) {
							if(!type.IsAbstract && type.IsSubclassOf(typeof(GraphConverter))) {
								var converter = System.Activator.CreateInstance(type, true);
								_graphConverters.Add(converter as GraphConverter);
							}
						}
					}
					catch { continue; }
				}
				_graphConverters.Sort((x, y) => Comparer<int>.Default.Compare(x.order, y.order));
			}
			return _graphConverters;
		}

		/// <summary>
		/// Get a graph system from a type
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		public static GraphSystemAttribute GetGraphSystem(UnityEngine.Object unityObject) {
			if(unityObject == null)
				return null;
			return GetGraphSystem(unityObject.GetType());
		}

		/// <summary>
		/// Get a graph system from a type
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		public static GraphSystemAttribute GetGraphSystem(Type type) {
			var graphs = FindGraphSystemAttributes();
			return graphs.FirstOrDefault((g) => g.type == type) ?? GraphSystemAttribute.Default;
		}

		// static bool hasCompilingInit;
		internal static void Initialize() {
			CG.OnSuccessGeneratingGraph += (generatedData, settings) => {
				if(settings.isPreview)
					return;
				foreach(var type in generatedData.types) {
					if(type is ITypeWithScriptData typeWithScriptData) {
						if(generatedData.classNames.TryGetValue(typeWithScriptData, out var className)) {
							typeWithScriptData.ScriptData.selfTypeData.typeName = settings.nameSpace.Add(".") + className;
							uNodeEditorUtility.MarkDirty(typeWithScriptData as UnityEngine.Object);//this will ensure the graph will be saved
							// Skip on generating in background
							// if (!settings.isAsync) { 
							// 	graph.graphData.lastCompiled = UnityEngine.Random.Range(1, int.MaxValue);
							// 	graph.graphData.lastSaved = graph.graphData.lastCompiled;
							// }
						}
					}
				}
				UpdateDatabase(new[] { generatedData });
				uNodeDatabase.ClearCache();
			};
			EditorBinding.onSceneSaving += (UnityEngine.SceneManagement.Scene scene, string path) => {
				//Save all graph.
				AutoSaveAllGraph();
			};
			EditorApplication.quitting += () => {
				SaveAllGraph();
				uNodeEditorUtility.SaveEditorData("", "EditorTabData");
			};
		}

		#region Saving
		/// <summary>
		/// Write all unsaved graph assets to disks.
		/// </summary>
		/// <param name="destroyRoot"></param>
		public static void SaveAllGraph() {
			//AssetDatabase.SaveAssets();
			var assets = FindAllGraphAssets().ToArray();
			List<string> assetPath = new List<string>();
			foreach(var asset in assets) {
				if(EditorUtility.IsDirty(asset)) {
					assetPath.Add(AssetDatabase.GetAssetPath(asset));
				}
				AssetDatabase.SaveAssetIfDirty(asset);
			}
			EditorReflectionUtility.UpdateRuntimeTypes();

			//Auto backup
			if(assetPath.Count > 0 && uNodePreference.preferenceData.autoBackupOnSave) {
				CreateBackup(assetPath);
			}
		}

		internal static void CreateBackup(IEnumerable<string> assets, string directory = "Graphs") {
			var dic = uNodePreference.backupPath + Path.DirectorySeparatorChar + directory;
			Directory.CreateDirectory(dic);
			string fileName = DateTime.Now.ToString("yyyy-dd-M--HH-mm-ss") + "---" + Path.GetRandomFileName() + ".zip";
			try {
				using(var zipToOpen = new FileStream(dic + Path.DirectorySeparatorChar + fileName, FileMode.OpenOrCreate)) {
					using(var archive = new ZipArchive(zipToOpen, ZipArchiveMode.Update)) {
						foreach(var path in assets.Distinct()) {
							archive.CreateEntryFromFile(path, path, System.IO.Compression.CompressionLevel.NoCompression);
						}
					}
				}
				RemoveBackup(directory);
			}
			catch(Exception ex) {
				Debug.LogError("Create backup error: " + ex.ToString());
			}
		}

		internal static void RemoveBackup(string directory = "Graphs", int maxBackup = 100) {
			directory = uNodePreference.backupPath + (string.IsNullOrEmpty(directory) ? "" : Path.DirectorySeparatorChar + directory);
			var dir = Directory.CreateDirectory(directory);
			var files = dir.GetFiles();
			if(files.Length > maxBackup) {
				for(int i = 0; i < files.Length; i++) {
					if(files.Length - i <= maxBackup)
						break;
					files[i].Delete();
				}
			}
		}

		/// <summary>
		/// Save the graph into prefab.
		/// Note: this only work on not in play mode as it for auto save.
		/// </summary>
		/// <param name="graphAsset"></param>
		/// <param name="graph"></param>
		public static void AutoSaveGraph(GameObject graph) {
			if(Application.isPlaying)
				return;
			// EditorUtility.DisplayProgressBar("Saving", "Saving graph assets.", 1);
			SaveGraph(graph);
			// EditorUtility.ClearProgressBar();
		}

		/// <summary>
		/// Save all graphs into prefab.
		/// Note: this only work on not in play mode as it for auto save.
		/// </summary>
		/// <param name="destroyRoot"></param>
		public static void AutoSaveAllGraph() {
			if(Application.isPlaying)
				return;
			SaveAllGraph();
		}

		/// <summary>
		/// Save the runtime graph to a prefab
		/// </summary>
		/// <param name="runtimeGraph"></param>
		/// <param name="graphAsset"></param>
		public static void SaveRuntimeGraph(IGraph runtimeGraph) {
			//if(!Application.isPlaying)
			//	throw new System.Exception("Saving runtime graph can only be done in playmode");
			//if(runtimeGraph.originalGraph == null)
			//	throw new System.Exception("Cannot save runtime graph because the original graph was null / missing");
			//var graph = runtimeGraph.originalGraph;
			//if(!EditorUtility.IsPersistent(graph))
			//	throw new System.Exception("Cannot save graph to unpersistent asset");
			//var prefabContent = PrefabUtility.LoadPrefabContents(AssetDatabase.GetAssetPath(graph));
			//var originalGraph = uNodeHelper.GetGraphComponent(prefabContent, graph.GraphName);
			//if(originalGraph != null) {
			//	if(runtimeGraph.RootObject != null) {
			//		//Duplicate graph data
			//		var tempRoot = Object.Instantiate(runtimeGraph.RootObject);
			//		tempRoot.name = "Root";
			//		//Move graph data to original graph
			//		tempRoot.transform.SetParent(originalGraph.transform);
			//		//Retarget graph data owner
			//		AnalizerUtility.RetargetNodeOwner(runtimeGraph, originalGraph, tempRoot.GetComponentsInChildren<MonoBehaviour>(true));
			//		if(originalGraph.RootObject != null) {
			//			//Destroy old graph data
			//			Object.DestroyImmediate(originalGraph.RootObject);
			//		}
			//		//Update graph data to new
			//		originalGraph.RootObject = tempRoot;
			//		//Save the graph to prefab
			//		uNodeEditorUtility.SavePrefabAsset(prefabContent, graph.gameObject);
			//		//GraphUtility.DestroyTempGraphObject(originalGraph.gameObject);

			//		//This will update the original graph
			//		GraphUtility.DestroyTempGraphObject(graph.gameObject);
			//		//Refresh uNode Editor window
			//		uNodeEditor.window?.Refresh();
			//	}
			//} else {
			//	Debug.LogError("Cannot save instanced graph because the cannot find original graph with id:" + graph.GraphName);
			//}
			//PrefabUtility.UnloadPrefabContents(prefabContent);
		}

		public static void SaveGraph(Object graphAsset) {
			//	if(graphAsset.name != graph.name) { //Ensure the name is same.
			//		graph.name = graphAsset.name;
			//	}
			//	{//Reset cache data & update last saved data.
			//		var roots = (graphAsset as GameObject).GetComponents<uNodeRoot>();
			//		var tempRoots = (graph as GameObject).GetComponents<uNodeRoot>();
			//		//Reset cached data
			//		if(roots.Length != tempRoots.Length) {
			//			UGraphView.ClearCache();
			//		} else {
			//			for(int i = 0; i < roots.Length; i++) {
			//				if(roots[i].Name != tempRoots[i].Name) {
			//					UGraphView.ClearCache();
			//					break;
			//				}
			//			}
			//		}
			//		//Update last saved data
			//		//var dateUID = DateTime.Now.GetTimeUID();
			//		//var scriptData = GenerationUtility.persistenceData.GetGraphData(graphAsset);
			//		//scriptData.compiledHash = default;
			//	}
			if(EditorUtility.IsPersistent(graphAsset)) {
				AssetDatabase.SaveAssetIfDirty(graphAsset);
			}
			else {
				uNodeEditorUtility.MarkDirty(graphAsset);
			}
			//	//Reset the cache data
			//	foreach(var r in graphs) {
			//		if(r == null)
			//			continue;
			//		var rType = ReflectionUtils.GetRuntimeType(r);
			//		if(rType is RuntimeGraphType graphType) {
			//			graphType.RebuildMembers();
			//		}
			//	}
		}
		#endregion

		#region Utilities
		/// <summary>
		/// Accept input: <see cref="GraphAsset"/>, <see cref="IScriptGraph"/>, <see cref="UGlobalEvent"/> and <see cref="CG.GeneratedData"/>
		/// </summary>
		/// <param name="assets"></param>
		public static void UpdateDatabase(IEnumerable<object> assets) {
			var db = uNodeUtility.GetDatabase();
			foreach(var asset in assets) {
				if(asset == null) continue;
				if(asset is GraphAsset graphAsset) {
					if(EditorUtility.IsPersistent(graphAsset) == false) continue;
					if(db.graphDatabases.Any(g => g.asset == graphAsset)) {
						continue;
					}
					db.graphDatabases.Add(new uNodeDatabase.RuntimeGraphDatabase() {
						asset = graphAsset,
					});
				}
				else if(asset is IScriptGraph scriptGraph) {
					if(db.nativeGraphDatabases.Any(g => g.ScriptGraph == scriptGraph)) {
						continue;
					}
					db.nativeGraphDatabases.Add(new uNodeDatabase.NativeGraphDatabase() {
						ScriptGraph = scriptGraph,
					});
				}
				else if(asset is UGlobalEvent globalEvent) {
					if(EditorUtility.IsPersistent(globalEvent) == false) continue;
					if(db.globalEventDatabases.Any(g => g.asset == globalEvent)) {
						continue;
					}
					db.globalEventDatabases.Add(new uNodeDatabase.RuntimeGlobalEventDatabase() {
						asset = globalEvent,
						guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(globalEvent))
					});
				}
				else if(asset is CG.GeneratedData generatedData) {
					UpdateDatabase(generatedData.types);
					if(generatedData.graphOwner is IScriptGraph) {
						UpdateDatabase(new[] { generatedData.graphOwner });
					}
				}
				//else {
				//	throw new InvalidOperationException();
				//}
			}
			EditorUtility.SetDirty(db);
			uNodeDatabase.ClearCache();
		}

		public static void UpdateDatabase(bool canCreateDB = true) {
			var db = uNodeUtility.GetDatabase();
			if(db == null && canCreateDB && EditorUtility.DisplayDialog("No graph database", "There's no graph database found in the project, do you want to create new?", "Ok", "Cancel")) {
				while(true) {
					var path = EditorUtility.SaveFolderPanel("Select resources folder to save database to", "Assets", "").Replace('/', Path.DirectorySeparatorChar);
					if(!string.IsNullOrEmpty(path)) {
						if(path.StartsWith(Directory.GetCurrentDirectory(), StringComparison.Ordinal) && path.ToLower().EndsWith("resources")) {
							db = ScriptableObject.CreateInstance<uNodeDatabase>();
							path = path.Remove(0, Directory.GetCurrentDirectory().Length + 1) + Path.DirectorySeparatorChar + "uNodeDatabase.asset";
							AssetDatabase.CreateAsset(db, path);
						}
						else {
							uNodeEditorUtility.DisplayErrorMessage("Please select 'Resources' folder in project");
							continue;
						}
					}
					break;
				}
			}
			if(db != null) {
				UpdateDatabase(GraphUtility.FindAllGraphAssets());

				var graphEvents = uNodeEditorUtility.FindAssetsByType<UGlobalEvent>();
				db.globalEventDatabases.Clear();
				UpdateDatabase(graphEvents);
			}
		}

		public static bool ReorderMoveUp<T>(UGraphElement element) where T : UGraphElement {
			var index = element.GetSiblingIndex();
			if(index == 0) {
				if(element.parent is T) {
					return false;
				}
				else {
					var parentIndex = element.parent.GetSiblingIndex();
					element.SetParent(element.parent.parent);
					element.SetSiblingIndex(parentIndex);
					return true;
				}
			}
			else {
				element.SetSiblingIndex(--index);
				return true;
			}
		}

		public static bool ReorderMoveDown<T>(UGraphElement element) where T : UGraphElement {
			var index = element.GetSiblingIndex();
			if(index + 1 >= element.parent.childCount) {
				if(element.parent is T) {
					return false;
				}
				else {
					var parentIndex = element.parent.GetSiblingIndex();
					element.SetParent(element.parent.parent);
					element.SetSiblingIndex(parentIndex + 1);
					return true;
				}
			}
			else {
				element.SetSiblingIndex(++index);
				return true;
			}
		}

		public static void RefactorFunctionName(Vector2 mousePosition, Function function, Action onRenamed) {
			string tempName = function.name;
			ActionPopupWindow.ShowWindow(Vector2.zero,
				() => {
					tempName = EditorGUILayout.TextField("Function Name", tempName);
				},
				onGUIBottom: () => {
					if(GUILayout.Button("Apply") || Event.current.keyCode == KeyCode.Return) {
						if(tempName != function.name) {
							function.name = GetUniqueName(tempName, function.graph);
							uNodeGUIUtility.GUIChangedMajor(function);
							onRenamed?.Invoke();
						}
						ActionPopupWindow.CloseLast();
					}
				}).ChangePosition(mousePosition).headerName = "Rename Function";
		}

		public static void RefactorPropertyName(Vector2 mousePosition, Property property, Action onRenamed) {
			string tempName = property.name;
			ActionPopupWindow.ShowWindow(Vector2.zero,
				() => {
					tempName = EditorGUILayout.TextField("Property Name", tempName);
				},
				onGUIBottom: () => {
					if(GUILayout.Button("Apply") || Event.current.keyCode == KeyCode.Return) {
						if(tempName != property.name) {
							property.name = GetUniqueName(tempName, property.graph);
							uNodeGUIUtility.GUIChangedMajor(property);
							onRenamed?.Invoke();
						}
						ActionPopupWindow.CloseLast();
					}
				}).ChangePosition(mousePosition).headerName = "Rename Property";
		}

		public static void RefactorVariableName(Vector2 mousePosition, Variable variable, Action onRenamed) {
			string tempName = variable.name;
			ActionPopupWindow.ShowWindow(Vector2.zero,
				() => {
					tempName = EditorGUILayout.TextField("Variable Name", tempName);
				},
				onGUIBottom: () => {
					if(GUILayout.Button("Apply") || Event.current.keyCode == KeyCode.Return) {
						if(tempName != variable.name) {
							variable.name = GetUniqueName(tempName, variable.graph);
							uNodeGUIUtility.GUIChangedMajor(variable);
							onRenamed?.Invoke();
						}
						ActionPopupWindow.CloseLast();
					}
				}).ChangePosition(mousePosition).headerName = "Rename Variable";
		}

		public static string GetUniqueName(string name, Graph graph) {
			name = uNodeUtility.AutoCorrectName(name);
			var index = 0;
			var fixName = name;
			var variables = graph.variableContainer.GetObjects(true);
			while(variables.Any(v => v.name == fixName)) {
				fixName = name + (++index);
			}
			var properties = graph.propertyContainer.GetObjects(true);
			while(properties.Any(v => v.name == fixName)) {
				fixName = name + (++index);
			}
			var functions = graph.functionContainer.GetObjects(true);
			while(functions.Any(v => v.name == fixName)) {
				fixName = name + (++index);
			}
			return fixName;
		}

		/// <summary>
		/// Find all graphs assets ( all type of graphs )
		/// </summary>
		/// <returns></returns>
		public static IEnumerable<Object> FindAllGraphAssets() {
			var assets = uNodeEditorUtility.FindAssetsByType<ScriptableObject>(type => {
				return type.IsCastableTo(typeof(IGraph)) || type.IsCastableTo(typeof(IScriptGraph));
			});
			foreach(var asset in assets) {
				yield return asset;
				//if(asset is IGraph || asset is IScriptGraph) {
				//	yield return asset;
				//}
			}
		}

		/// <summary>
		/// Find all graphs assets ( excluding the ScriptGraph )
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public static IEnumerable<T> FindGraphs<T>() where T : GraphAsset {
			return uNodeEditorUtility.FindAssetsByType<T>();
		}

		/// <summary>
		/// Find graph asset by Type
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		public static IEnumerable<Object> FindGraphs(Type type) {
			return uNodeEditorUtility.FindAssetsByType(type);
		}
		#endregion
	}
}
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;
using MaxyGames.UNode.Nodes;
using System.Collections.Concurrent;

namespace MaxyGames.UNode {
	public static class RuntimeGraphUtility {
		internal class GraphRunner : ScriptableObject, IGraph {
			public GraphAsset asset;
			public WeakReference reference;
			private int m_hashcode;

			internal static System.Runtime.CompilerServices.ConditionalWeakTable<object, GraphRunner> cachedRunners = new System.Runtime.CompilerServices.ConditionalWeakTable<object, GraphRunner>();

			internal void Initialize(GraphAsset asset, object instance) {
				this.asset = asset;
				m_hashcode = uNodeUtility.GetObjectID(asset);
				if(cachedRunners.TryGetValue(instance, out var runner)) {
					runner.Destroy();
				}
				cachedRunners.AddOrUpdate(instance, this);
				reference = new WeakReference(instance);
				UpdateGraph();
			}

			public Graph GraphData { get; private set; }

			//For live editing
			public void UpdateGraph() {
				var version = asset.GraphData.version;
				if(object.ReferenceEquals(GraphData, null) || GraphData.version != version) {
					var data = SerializedGraph.Serialize(asset.GraphData);
					for(int i = 0; i < data.references.Count; i++) {
						if(data.references[i] == asset) {
							data.references[i] = this;
						}
					}
					var newGraph = SerializedGraph.Deserialize(data);
					newGraph.owner = this;
					newGraph.version = version;
					newGraph.InitializeElement();

					{
						var referenceID = reference.GetHashCode();
						var graphID = asset.GetHashCode();
						foreach(var element in newGraph.GetObjectsInChildren(true)) {
							element.runtimeID = new RuntimeGraphID(graphID, element.id, referenceID);
						}
					}
					var oldGraph = GraphData;
					if(oldGraph != null) {
						oldGraph.MarkInvalid();
					}
					GraphData = newGraph;
					//For testing purpose
					//var elemt = newGraph.GetElementByID(17);
					//if(elemt != null && elemt is NodeObject node) {
					//	Debug.Log(node.runtimeID + "|" + (node.node as MultipurposeNode).target.Get(null));
					//}
				}
			}

			public void Destroy() {
				GraphData = null;
				object runnerKey = null;
				if(reference.IsAlive) {
					runnerKey = reference.Target;
				}
				if(runnerKey != null)
					cachedRunners.Remove(runnerKey);
				DestroyImmediate(this);
			}

			public static void UpdateAllRunners() {
				//Debug.Log("Update Graph Runners");
				foreach(var (obj, value) in cachedRunners) {
					if(obj == null || obj is Object unityObject && unityObject == null)
						continue;
					value.UpdateGraph();
				}
			}

			internal static void DestroyAllRunners() {
				foreach(var (_, value) in cachedRunners) {
					if(value != null)
						value.Destroy();
				}
				cachedRunners.Clear();
			}

			public override int GetHashCode() {
				return m_hashcode;
			}
		}

		private static class Cached {
			public static ConcurrentDictionary<(IGraph, object), GraphInstance> cachedObjectGraphInstance = new ConcurrentDictionary<(IGraph, object), GraphInstance>();
		}

		public enum GraphKind {
			Component,
			Asset,
			Object,
		}

		public static IGraph GetOrCreateGraphRunner(GraphAsset graphAsset, object instance) {
			if(GraphRunner.cachedRunners.TryGetValue(instance, out var result)) {
				return result;
			}
			result = ScriptableObject.CreateInstance<GraphRunner>();
			result.Initialize(graphAsset, instance);
			return result;
		}

		public static void DestroyGraphRunner(IGraph runner) {
			if(runner is GraphRunner graphRunner) {
				graphRunner.Destroy();
			}
		}

		//public static Graph InstantiateGraph(IGraph from, IGraph to, IList<VariableData> variables) {
		//	try {
		//		var graph = from.GraphData;
		//		if(to is UnityEngine.Object) {
		//			var data = SerializedGraph.Serialize(graph);
		//			for(int i = 0; i < data.references.Count; i++) {
		//				if(data.references[i] == from as Object) {
		//					data.references[i] = to as Object;
		//				}
		//			}
		//			graph = SerializedGraph.Deserialize(data) as Graph;
		//		}
		//		else {
		//			//TODO: add support for class object

		//		}
		//		graph.owner = to;
		//		if(variables != null) {//This is for set variable value to same with overridden variable in instanced graph
		//			for(int i = 0; i < graph.variableContainer.collections.Count; i++) {
		//				var var = graph.variableContainer.collections[i];
		//				for(int x = 0; x < variables.Count; x++) {
		//					if(var.name.Equals(variables[x].name)) {
		//						var.defaultValue = variables[x].Get();
		//					}
		//				}
		//			}
		//		}
		//		return graph;
		//	}
		//	catch(Exception ex) {
		//		Debug.LogError($"Error on trying to initialize graph: {from} to the {to}.\nError: {ex.ToString()}");
		//		throw;
		//	}
		//}

		/// <summary>
		/// Get the inherited graph
		/// </summary>
		/// <param name="graph"></param>
		/// <returns></returns>
		public static IGraph GetInheritedGraph(IGraph graph) {
			if(graph is IClassGraph classGraph) {
				var baseType = classGraph.InheritType;
				if(baseType is IRuntimeMemberWithRef typeWithRef) {
					var reference = typeWithRef.GetReference();
					if(reference is GraphRef graphRef && graphRef.graph != null) {
						var result = graphRef.graph.graphContainer;
						if(result != graph) {
							return result;
						}
					}
				}
			}
			return null;
		}

		public static void InitializeInstanceGraphValue(IGraph graphReference, GraphInstance instance, IList<VariableData> variables) {
			if(variables == null) return;
			try {
				var baseGraph = GetInheritedGraph(graphReference);
				if(baseGraph != null) {
					InitializeInstanceGraphValue(baseGraph, instance, variables);
				}
				var graph = graphReference.GraphData;
				if(variables != null) {
					//This is for set variable value to same with overridden variable in instanced graph
					for(int i = 0; i < graph.variableContainer.collections.Count; i++) {
						var var = graph.variableContainer.collections[i];
						for(int x = 0; x < variables.Count; x++) {
							if(var.name.Equals(variables[x].name)) {
								//Change the default variable value
								var.Set(instance, variables[x].Get());
							}
						}
					}
				}
			}
			catch(Exception ex) {
				Debug.LogError($"Error on trying to initialize graph: {graphReference} to the {instance.target}.\nError: {ex.ToString()}");
				throw;
			}
        }

		/// <summary>
		/// Execute graph function ( support both runtime and in editor )
		/// </summary>
		/// <param name="name"></param>
		/// <param name="instance"></param>
		/// <param name="graphReference"></param>
		/// <param name="target"></param>
		/// <param name="variables"></param>
		public static void ExecuteFunction(string name, ref GraphInstance instance, IGraph graphReference, MonoBehaviour target, IList<VariableData> variables) {
            if(!Application.isPlaying) {
                var version = graphReference.GraphData.version;
                if(instance == null || instance.version != version) {
                    if(graphReference.GraphData.functionContainer.collections.Any(f => f.name == name)) {
                        instance = InitializeComponentGraph(graphReference, target);
                        instance.version = version;
                        InitializeInstanceGraphValue(graphReference, instance, variables);
                    }
                }
            }
			if(instance != null) {
				var func = graphReference.GetFunction(name);
				if(func != null) {
                    if(variables != null) {//This is for set variable value to same with overridden variable in instanced graph
                        foreach(var var in graphReference.GraphData.variableContainer.collections) {
                            for(int x = 0; x < variables.Count; x++) {
                                if(var.name.Equals(variables[x].name)) {
									var.Set(instance, variables[x].Get());
								}
                            }
                        }
                    }
                    func.Invoke(instance);
				}
            }
        }

		/// <summary>
		/// Execute custom graph event ( support both runtime and in editor )
		/// </summary>
		/// <param name="name"></param>
		/// <param name="instance"></param>
		/// <param name="graphReference"></param>
		/// <param name="target"></param>
		/// <param name="variables"></param>
		public static void ExecuteCustomEvent(string name, ref GraphInstance instance, IGraph graphReference, MonoBehaviour target, IList<VariableData> variables) {
			if(!Application.isPlaying) {
				var version = graphReference.GraphData.version;
				if(instance == null || instance.version != version) {
					instance = InitializeComponentGraph(graphReference, target);
					instance.version = version;
					InitializeInstanceGraphValue(graphReference, instance, variables);
				}
			}
			var data = instance?.eventData;
			if(data != null /*&& data.HasCustomEvent(name)*/) {
				if(variables != null) {//This is for set variable value to same with overridden variable in instanced graph
					foreach(var var in graphReference.GraphData.variableContainer.collections) {
						for(int x = 0; x < variables.Count; x++) {
							if(var.name.Equals(variables[x].name)) {
								var.Set(instance, variables[x].Get());
							}
						}
					}
				}
				data.ExecuteCustomEvent(name, instance);
			}
		}

		public static void DrawGizmos(ref GraphInstance instance, IGraph graphReference, MonoBehaviour target, IList<VariableData> variables) {
			if(!Application.isPlaying) {
				var version = graphReference.GraphData.version;
				if(instance == null || instance.version != version) {
					if(graphReference.GraphData.functionContainer.collections.Any(f => f.name == "OnDrawGizmos")) {
						instance = InitializeComponentGraph(graphReference, target);
						instance.version = version;
						InitializeInstanceGraphValue(graphReference, instance, variables);
					}
				}
			}
			var data = instance?.eventData;
			if(data != null && data.onDrawGizmos != null) {
				if(variables != null) {//This is for set variable value to same with overridden variable in instanced graph
					foreach(var var in graphReference.GraphData.variableContainer.collections) {
						for(int x = 0; x < variables.Count; x++) {
							if(var.name.Equals(variables[x].name)) {
								var.Set(instance, variables[x].Get());
							}
						}
					}
				}
				data.onDrawGizmos.Invoke(instance);
			}
		}

		public static void DrawGizmosSelected(ref GraphInstance instance, IGraph graphAsset, MonoBehaviour target, IList<VariableData> variables) {
			if(!Application.isPlaying) {
				var version = graphAsset.GraphData.version;
				if(instance == null || instance.version != version) {
					if(graphAsset.GraphData.functionContainer.collections.Any(f => f.name == "OnDrawGizmosSelected")) {
						instance = InitializeComponentGraph(graphAsset, target);
						instance.version = version;
						InitializeInstanceGraphValue(graphAsset, instance, variables);
					}
				}
			}
			var data = instance?.eventData;
			if(data != null && data.onDrawGizmosSelected != null) {
				if(variables != null) {//This is for set variable value to same with overridden variable in instanced graph
					foreach(var var in graphAsset.GraphData.variableContainer.collections) {
						for(int x = 0; x < variables.Count; x++) {
							if(var.name.Equals(variables[x].name)) {
								var.Set(instance, variables[x].Get());
							}
						}
					}
				}
				data.onDrawGizmosSelected.Invoke(instance);
			}
		}

		public static GraphInstance InitializeComponentGraph(IGraph graphReference, MonoBehaviour target, IList<VariableData> variables = null) {
			var graph = graphReference.GraphData;
			var data = new RuntimeGraphEventData();
			GraphInstance graphInstance = new GraphInstance(target, graphReference, data);
			if(variables != null) {
				InitializeInstanceGraphValue(graphReference, graphInstance, variables);
			}
			graphInstance.Initialize();

			foreach(var evt in graph.functionContainer.GetObjectsInChildren<Function>(true)) {
				if(evt.Parameters.Count == 0) {
					switch(evt.name) {
						case "Awake":
							data.onAwake += (instance) => evt.Invoke(instance);
							break;
						case "Start":
							data.onStart += (instance) => evt.Invoke(instance);
							break;
						case nameof(UEventID.Update):
							UEvent.Register(UEventID.Update, target, () => {
								evt.Invoke(graphInstance);
							});
							break;
						case nameof(UEventID.FixedUpdate):
							UEvent.Register(UEventID.FixedUpdate, target, () => {
								evt.Invoke(graphInstance);
							});
							break;
						case nameof(UEventID.LateUpdate):
							UEvent.Register(UEventID.LateUpdate, target, () => {
								evt.Invoke(graphInstance);
							});
							break;
						case nameof(UEventID.OnAnimatorMove):
							UEvent.Register(UEventID.OnAnimatorMove, target, () => {
								evt.Invoke(graphInstance);
							});
							break;
						case nameof(UEventID.OnApplicationQuit):
							UEvent.Register(UEventID.OnApplicationQuit, target, () => {
								evt.Invoke(graphInstance);
							});
							break;
						case nameof(UEventID.OnBecameInvisible):
							UEvent.Register(UEventID.OnBecameInvisible, target, () => {
								evt.Invoke(graphInstance);
							});
							break;
						case nameof(UEventID.OnBecameVisible):
							UEvent.Register(UEventID.OnBecameVisible, target, () => {
								evt.Invoke(graphInstance);
							});
							break;
						case nameof(UEventID.OnDestroy):
							data.onDestroy += (instance) => evt.Invoke(instance);
							break;
						case nameof(UEventID.OnDisable):
							data.onDisable += (instance) => evt.Invoke(instance);
							break;
						case nameof(UEventID.OnEnable):
							data.onEnable += (instance) => evt.Invoke(instance);
							break;
						case nameof(UEventID.OnGUI):
							UEvent.Register(UEventID.OnGUI, target, () => {
								evt.Invoke(graphInstance);
							});
							break;
						case nameof(UEventID.OnMouseDown):
							UEvent.Register(UEventID.OnMouseDown, target, () => {
								evt.Invoke(graphInstance);
							});
							break;
						case nameof(UEventID.OnMouseDrag):
							UEvent.Register(UEventID.OnMouseDrag, target, () => {
								evt.Invoke(graphInstance);
							});
							break;
						case nameof(UEventID.OnMouseEnter):
							UEvent.Register(UEventID.OnMouseEnter, target, () => {
								evt.Invoke(graphInstance);
							});
							break;
						case nameof(UEventID.OnMouseExit):
							UEvent.Register(UEventID.OnMouseExit, target, () => {
								evt.Invoke(graphInstance);
							});
							break;
						case nameof(UEventID.OnMouseOver):
							UEvent.Register(UEventID.OnMouseOver, target, () => {
								evt.Invoke(graphInstance);
							});
							break;
						case nameof(UEventID.OnMouseUp):
							UEvent.Register(UEventID.OnMouseUp, target, () => {
								evt.Invoke(graphInstance);
							});
							break;
						case nameof(UEventID.OnMouseUpAsButton):
							UEvent.Register(UEventID.OnMouseUpAsButton, target, () => {
								evt.Invoke(graphInstance);
							});
							break;
						case nameof(UEventID.OnPostRender):
							UEvent.Register(UEventID.OnPostRender, target, () => {
								evt.Invoke(graphInstance);
							});
							break;
						case nameof(UEventID.OnPreCull):
							UEvent.Register(UEventID.OnPreCull, target, () => {
								evt.Invoke(graphInstance);
							});
							break;
						case nameof(UEventID.OnPreRender):
							UEvent.Register(UEventID.OnPreRender, target, () => {
								evt.Invoke(graphInstance);
							});
							break;
						case nameof(UEventID.OnRenderObject):
							UEvent.Register(UEventID.OnRenderObject, target, () => {
								evt.Invoke(graphInstance);
							});
							break;
						case nameof(UEventID.OnWillRenderObject):
							UEvent.Register(UEventID.OnWillRenderObject, target, () => {
								evt.Invoke(graphInstance);
							});
							break;
						case "OnDrawGizmos":
							data.onDrawGizmos += (instance) => evt.Invoke(instance);
							break;
						case "OnDrawGizmosSelected":
							data.onDrawGizmosSelected += (instance) => evt.Invoke(instance);
							break;
						case nameof(UEventID.OnTransformChildrenChanged):
							UEvent.Register(UEventID.OnTransformChildrenChanged, target, () => {
								evt.Invoke(graphInstance);
							});
							break;
						case nameof(UEventID.OnTransformParentChanged):
							UEvent.Register(UEventID.OnTransformParentChanged, target, () => {
								evt.Invoke(graphInstance);
							});
							break;
					}
				} else if(evt.Parameters.Count == 1) {
					switch(evt.name) {
						case nameof(UEventID.OnAnimatorIK):
							UEvent.Register<int>(UEventID.OnAnimatorIK, target, (arg) => {
								evt.Invoke(graphInstance, new object[] { arg });
							});
							break;
						case nameof(UEventID.OnApplicationFocus):
							UEvent.Register<bool>(UEventID.OnApplicationFocus, target, (arg) => {
								evt.Invoke(graphInstance, new object[] { arg });
							});
							break;
						case nameof(UEventID.OnApplicationPause):
							UEvent.Register<bool>(UEventID.OnApplicationPause, target, (arg) => {
								evt.Invoke(graphInstance, new object[] { arg });
							});
							break;
						case nameof(UEventID.OnCollisionEnter):
							UEvent.Register<Collision>(UEventID.OnCollisionEnter, target, (arg) => {
								evt.Invoke(graphInstance, new object[] { arg });
							});
							break;
						case nameof(UEventID.OnCollisionEnter2D):
							UEvent.Register<Collision2D>(UEventID.OnCollisionEnter2D, target, (arg) => {
								evt.Invoke(graphInstance, new object[] { arg });
							});
							break;
						case nameof(UEventID.OnCollisionExit):
							UEvent.Register<Collision>(UEventID.OnCollisionExit, target, (arg) => {
								evt.Invoke(graphInstance, new object[] { arg });
							});
							break;
						case nameof(UEventID.OnCollisionExit2D):
							UEvent.Register<Collision2D>(UEventID.OnCollisionExit2D, target, (arg) => {
								evt.Invoke(graphInstance, new object[] { arg });
							});
							break;
						case nameof(UEventID.OnCollisionStay):
							UEvent.Register<Collision>(UEventID.OnCollisionStay, target, (arg) => {
								evt.Invoke(graphInstance, new object[] { arg });
							});
							break;
						case nameof(UEventID.OnCollisionStay2D):
							UEvent.Register<Collision2D>(UEventID.OnCollisionStay2D, target, (arg) => {
								evt.Invoke(graphInstance, new object[] { arg });
							});
							break;
						case nameof(UEventID.OnParticleCollision):
							UEvent.Register<GameObject>(UEventID.OnParticleCollision, target, (arg) => {
								evt.Invoke(graphInstance, new object[] { arg });
							});
							break;
						//case nameof(UEventID.OnPointerClick):
						//	UEvent.Register(UEventID.OnPointerClick, target, () => {
						//		evt.Invoke();
						//	});
						//	break;
						//case nameof(UEventID.OnPointerDown):
						//	UEvent.Register(UEventID.OnPointerDown, target, () => {
						//		evt.Invoke();
						//	});
						//	break;
						//case nameof(UEventID.OnPointerEnter):
						//	UEvent.Register(UEventID.OnPointerEnter, target, () => {
						//		evt.Invoke();
						//	});
						//	break;
						//case nameof(UEventID.OnPointerExit):
						//	UEvent.Register(UEventID.OnPointerExit, target, () => {
						//		evt.Invoke();
						//	});
						//	break;
						//case nameof(UEventID.OnPointerMove):
						//	UEvent.Register(UEventID.OnPointerMove, target, () => {
						//		evt.Invoke();
						//	});
						//	break;
						//case nameof(UEventID.OnPointerUp):
						//	UEvent.Register(UEventID.OnPointerUp, target, () => {
						//		evt.Invoke();
						//	});
						//	break;
						case nameof(UEventID.OnTriggerEnter):
							UEvent.Register<Collider>(UEventID.OnTriggerEnter, target, (arg) => {
								evt.Invoke(graphInstance, new object[] { arg });
							});
							break;
						case nameof(UEventID.OnTriggerEnter2D):
							UEvent.Register<Collider2D>(UEventID.OnTriggerEnter2D, target, (arg) => {
								evt.Invoke(graphInstance, new object[] { arg });
							});
							break;
						case nameof(UEventID.OnTriggerExit):
							UEvent.Register<Collider>(UEventID.OnTriggerExit, target, (arg) => {
								evt.Invoke(graphInstance, new object[] { arg });
							});
							break;
						case nameof(UEventID.OnTriggerExit2D):
							UEvent.Register<Collider2D>(UEventID.OnTriggerExit2D, target, (arg) => {
								evt.Invoke(graphInstance, new object[] { arg });
							});
							break;
						case nameof(UEventID.OnTriggerStay):
							UEvent.Register<Collider>(UEventID.OnTriggerStay, target, (arg) => {
								evt.Invoke(graphInstance, new object[] { arg });
							});
							break;
						case nameof(UEventID.OnTriggerStay2D):
							UEvent.Register<Collider2D>(UEventID.OnTriggerStay2D, target, (arg) => {
								evt.Invoke(graphInstance, new object[] { arg });
							});
							break;
					}
				}
			}
			foreach(var evt in graph.mainGraphContainer.GetNodesInChildren<BaseGraphEvent>(true)) {
				if(evt is StartEvent) {
					data.onStart += evt.Trigger;
				} else if(evt is AwakeEvent) {
					data.onAwake += evt.Trigger;
				} else if(evt is OnDestroyEvent) {
					data.onDestroy += evt.Trigger;
				} else if(evt is OnDisableEvent) {
					data.onDisable += evt.Trigger;
				} else if(evt is OnEnableEvent) {
					data.onEnable += evt.Trigger;
				}
			}
			return graphInstance;
		}

		public static GraphInstance InitializeAssetGraph(IGraph graphReference, ScriptableObject target, IList<VariableData> variables = null) {
			var graph = graphReference.GraphData;
			var data = new RuntimeGraphEventData();
			GraphInstance graphInstance = new GraphInstance(target, graphReference, data);
			if(variables != null) {
				InitializeInstanceGraphValue(graphReference, graphInstance, variables);
			}
			graphInstance.Initialize();
			foreach (var evt in graph.functionContainer.GetObjectsInChildren<Function>(true)) {
				if (evt.Parameters.Count == 0) {
					switch (evt.name) {
						case "Awake":
							data.onAwake += (instance) => evt.Invoke(instance);
							break;
						case nameof(UEventID.OnDestroy):
							data.onDestroy += (instance) => evt.Invoke(instance);
							break;
						case nameof(UEventID.OnDisable):
							data.onDisable += (instance) => evt.Invoke(instance);
							break;
						case nameof(UEventID.OnEnable):
							data.onEnable += (instance) => evt.Invoke(instance);
							break;
					}
				}
			}
			foreach (var evt in graph.mainGraphContainer.GetNodesInChildren<BaseGraphEvent>(true)) {
				if (evt is AwakeEvent) {
					data.onAwake += evt.Trigger;
				}
				else if (evt is OnDestroyEvent) {
					data.onDestroy += evt.Trigger;
				}
				else if (evt is OnDisableEvent) {
					data.onDisable += evt.Trigger;
				}
				else if (evt is OnEnableEvent) {
					data.onEnable += evt.Trigger;
				}
			}
			return graphInstance;
		}

		public static GraphInstance InitializeObjectGraph(IGraph graphReference, object target, IList<VariableData> variables = null) {
			var data = new RuntimeGraphEventData();
			GraphInstance graphInstance = new GraphInstance(target, graphReference, data);
			if(variables != null) {
				InitializeInstanceGraphValue(graphReference, graphInstance, variables);
			}
			graphInstance.Initialize();

			return graphInstance;
		}

		public static GraphInstance GetObjectGraphInstance(IGraph graphReference, object target) {
			if(target is IInstancedGraph instancedGraph) {
				if(instancedGraph.Instance != null) {
					return instancedGraph.Instance;
				}
				else {
					if(!Cached.cachedObjectGraphInstance.TryGetValue((graphReference, target), out var instance)) {
						instance = InitializeObjectGraph(graphReference, target);
						Cached.cachedObjectGraphInstance[(graphReference, target)] = instance;
					}
					else {
						var version = graphReference.GraphData.version;
						if(instance.version != version) {
							instance = InitializeObjectGraph(graphReference, target);
							instance.version = version;
							Cached.cachedObjectGraphInstance[(graphReference, target)] = instance;
						}
					}
					return instance;
				}
			}
			else {
				return null;
			}

		}

		public static IEnumerable<MacroPortNode> GetMacroInputFlows(Graph graph) {
			var container = graph.mainGraphContainer;
			foreach(var child in container) {
				if(child is NodeObject node && node.node is MacroPortNode macroPort && macroPort.kind == PortKind.FlowInput) {
					yield return macroPort;
				}
			}
		}
		public static IEnumerable<MacroPortNode> GetMacroInputValues(Graph graph) {
			var container = graph.mainGraphContainer;
			foreach(var child in container) {
				if(child is NodeObject node && node.node is MacroPortNode macroPort && macroPort.kind == PortKind.ValueInput) {
					yield return macroPort;
				}
			}
		}
		public static IEnumerable<MacroPortNode> GetMacroOutputFlows(Graph graph) {
			var container = graph.mainGraphContainer;
			foreach(var child in container) {
				if(child is NodeObject node && node.node is MacroPortNode macroPort && macroPort.kind == PortKind.FlowOutput) {
					yield return macroPort;
				}
			}
		}
		public static IEnumerable<MacroPortNode> GetMacroOutputValues(Graph graph) {
			var container = graph.mainGraphContainer;
			foreach(var child in container) {
				if(child is NodeObject node && node.node is MacroPortNode macroPort && macroPort.kind == PortKind.ValueOutput) {
					yield return macroPort;
				}
			}
		}
	}
}
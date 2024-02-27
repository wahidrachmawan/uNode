using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MaxyGames.UNode.Editors {
	/// <summary>
	/// This is base class for convert a graph into other graph type
	/// </summary>
	public abstract class GraphConverter {
		public virtual int order => 0;
		public abstract bool IsValid(IGraph graph);
		public abstract string GetMenuName(IGraph graph);
		public abstract void Convert(IGraph graph);

		protected void ValidateGraph(
			IGraph graph,
			bool supportAttribute = true,
			bool supportGeneric = true,
			bool supportModifier = true,
			bool supportConstructor = true) {
			if(!supportAttribute) {
				if(graph is IGraphWithAttributes AS && AS.Attributes?.Count > 0) {
					Debug.LogWarning("The target graph contains 'Attributes' which is not supported, the converted graph will not include it.");
					AS.Attributes.Clear();
				}
			}
			if(!supportGeneric) {
				if(graph is IGenericParameterSystem GPS && GPS.GenericParameters?.Count > 0) {
					Debug.LogWarning("The target graph contains 'GenericParameter' which is not supported, the converted graph will not include it.");
					GPS.GenericParameters = new GenericParameterData[0];
				}
			}
			if(!supportModifier) {
				if(graph is IClassModifier clsModifier) {
					var modifier = clsModifier.GetModifier();
					if(modifier.Static || modifier.Abstract || modifier.Partial || modifier.Sealed) {
						Debug.LogWarning("The target graph contains unsupported class modifier, the converted graph will ignore it.");
						modifier.Static = false;
						modifier.Abstract = false;
						modifier.Partial = false;
						modifier.Sealed = false;
					}
				}
			}
			if(!supportModifier && graph is IGraphWithVariables variableSystem) {
				var variables = variableSystem.GetVariables();
				if(variables != null) {
					foreach(var v in variables) {
						if(!supportModifier) {
							if(v.modifier.Static || v.modifier.ReadOnly || v.modifier.Const) {
								Debug.LogWarning("The target graph contains unsupported variable modifier, the converted graph will ignore it.");
								v.modifier.Static = false;
								v.modifier.ReadOnly = false;
								v.modifier.Const = false;
							}
						}
					}
				}
			}
			if((!supportAttribute || !supportGeneric || !supportModifier) && graph is IGraphWithFunctions functionSystem) {
				var functions = functionSystem.GetFunctions();
				if(functions != null) {
					foreach(var f in functions) {
						if(!supportAttribute) {
							if(f.attributes?.Count > 0) {
								Debug.LogWarning("The target graph contains function 'Attributes' which is not supported, the converted graph will not include it.");
								f.attributes.Clear();
							}
						}
						if(!supportGeneric) {
							if(f.GenericParameters?.Count > 0) {
								Debug.LogWarning("The target graph contains function generic parameter which is not supported, the converted graph will not include it.");
								f.GenericParameters = new GenericParameterData[0];
							}
						}
						if(!supportModifier) {
							if(f.modifier.Abstract || f.modifier.Async || f.modifier.Extern || f.modifier.New || f.modifier.Override || f.modifier.Partial || f.modifier.Static || f.modifier.Unsafe || f.modifier.Virtual) {
								Debug.LogWarning("The target graph contains unsupported function modifier, the converted graph will ignore it.");
								f.modifier = new FunctionModifier() {
									Public = f.modifier.isPublic,
									Private = !f.modifier.isPublic
								};
							}
						}
					}
				}
			}
			if((!supportAttribute || !supportModifier) && graph is IGraphWithProperties propertySystem) {
				var properties = propertySystem.GetProperties();
				if(properties != null) {
					foreach(var f in properties) {
						if(!supportAttribute) {
							if(f.attributes?.Count > 0) {
								Debug.LogWarning("The target graph contains property 'Attributes' which is not supported, the converted graph will not include it.");
								f.attributes.Clear();
							}
						}
						if(!supportModifier) {
							if(f.modifier.Abstract || f.modifier.Static || f.modifier.Virtual) {
								Debug.LogWarning("The target graph contains unsupported property modifier, the converted graph will ignore it.");
								f.modifier = new PropertyModifier() {
									Public = f.modifier.isPublic,
									Private = !f.modifier.isPublic
								};
							}
						}
					}
				}
			}
			if(!supportConstructor) {
				if(graph is IGraphWithConstructors CS) {
					var ctors = CS.GetConstructors().ToArray();
					if(ctors?.Length > 0) {
						Debug.LogWarning("The target graph contains constructor which is not supported, the converted graph will not include it.");
						for(int i = 0; i < ctors.Length; i++) {
							if(ctors[i] != null) {
								ctors[i].Destroy();
							}
						}
					}
				}
			}
		}
	}
}

namespace MaxyGames.UNode.Editors.Converter {
	class ConverterToClassScript : GraphConverter {
		public override void Convert(IGraph graph) {
			if(graph is GraphComponent) {
				var sourceGraph = graph as GraphComponent;

				var scriptGraph = ScriptableObject.CreateInstance<ScriptGraph>();
				scriptGraph.name = graph.GetGraphName();
				var graphAsset = ScriptGraph.CreateInstance<ClassScript>();
				graphAsset.inheritType = typeof(MonoBehaviour);
				SerializedGraph.Copy(sourceGraph.serializedGraph, graphAsset.serializedGraph, sourceGraph, graphAsset);
				scriptGraph.TypeList.AddType(graphAsset, scriptGraph);
				var path = EditorUtility.SaveFilePanelInProject("Save graph", graph.GetGraphName(), "asset", "");
				if(string.IsNullOrEmpty(path) == false) {
					AssetDatabase.CreateAsset(scriptGraph, path);
					AssetDatabase.AddObjectToAsset(graphAsset, scriptGraph);
					ValidateGraph(graphAsset);
					AssetDatabase.SaveAssets();
					EditorGUIUtility.PingObject(scriptGraph);
				}
			}
			else if(graph is ClassDefinition) {
				var sourceGraph = graph as ClassDefinition;

				var scriptGraph = ScriptableObject.CreateInstance<ScriptGraph>();
				scriptGraph.name = graph.GetGraphName();
				var graphAsset = ScriptGraph.CreateInstance<ClassScript>();
				graphAsset.inheritType = sourceGraph.model.InheritType;
				SerializedGraph.Copy(sourceGraph.serializedGraph, graphAsset.serializedGraph, sourceGraph, graphAsset);
				scriptGraph.TypeList.AddType(graphAsset, scriptGraph);
				var path = EditorUtility.SaveFilePanelInProject("Save graph", graph.GetGraphName(), "asset", "", System.IO.Path.GetDirectoryName(AssetDatabase.GetAssetPath(sourceGraph)));
				if(string.IsNullOrEmpty(path) == false) {
					AssetDatabase.CreateAsset(scriptGraph, path);
					AssetDatabase.AddObjectToAsset(graphAsset, scriptGraph);
					ValidateGraph(graphAsset);
					AssetDatabase.SaveAssets();
					EditorGUIUtility.PingObject(scriptGraph);
				}
			}
		}

		public override string GetMenuName(IGraph graph) {
			return "Convert to C# Class";
		}

		public override bool IsValid(IGraph graph) {
			if(graph is ClassDefinition definition) {
				return ReflectionUtils.IsNativeType(definition.InheritType);
			}
			if(graph is GraphComponent) {
				return true;
			}
			return false;
		}
	}


	class ConverterToClassDefinition : GraphConverter {
		public override void Convert(IGraph graph) {
			if(graph is GraphComponent) {
				var sourceGraph = graph as GraphComponent;

				var graphAsset = ScriptGraph.CreateInstance<ClassDefinition>();
				graphAsset.name = graph.GetGraphName();
				graphAsset.model = new ClassComponentModel();
				graphAsset.@namespace = sourceGraph.GetGraphNamespace();
				graphAsset.usingNamespaces = new List<string>(sourceGraph.GetUsingNamespaces());

				SerializedGraph.Copy(sourceGraph.serializedGraph, graphAsset.serializedGraph, sourceGraph, graphAsset);
				var path = EditorUtility.SaveFilePanelInProject("Save graph", graph.GetGraphName(), "asset", "");
				if(string.IsNullOrEmpty(path) == false) {
					AssetDatabase.CreateAsset(graphAsset, path);
					ValidateGraph(graphAsset, supportAttribute: false, supportGeneric: false, supportModifier: false, supportConstructor: false);
					AssetDatabase.SaveAssets();
					EditorGUIUtility.PingObject(graphAsset);
				}
			}
			else if(graph is ClassScript) {
				var sourceGraph = graph as ClassScript;

				var graphAsset = ScriptGraph.CreateInstance<ClassDefinition>();
				graphAsset.name = graph.GetGraphName();
				if(sourceGraph.inheritType == typeof(MonoBehaviour)) {
					graphAsset.model = new ClassComponentModel();
				}
				else if(sourceGraph.inheritType == typeof(ScriptableObject)) {
					graphAsset.model = new ClassAssetModel();
				}
				else {
					graphAsset.model = new ClassObjectModel();
				}
				graphAsset.@namespace = sourceGraph.GetGraphNamespace();
				graphAsset.usingNamespaces = new List<string>(sourceGraph.GetUsingNamespaces());

				SerializedGraph.Copy(sourceGraph.serializedGraph, graphAsset.serializedGraph, sourceGraph, graphAsset);
				var path = EditorUtility.SaveFilePanelInProject("Save graph", graph.GetGraphName(), "asset", "", System.IO.Path.GetDirectoryName(AssetDatabase.GetAssetPath(sourceGraph)));
				if(string.IsNullOrEmpty(path) == false) {
					AssetDatabase.CreateAsset(graphAsset, path);
					ValidateGraph(graphAsset, supportAttribute: false, supportGeneric: false, supportModifier: false, supportConstructor: false);
					AssetDatabase.SaveAssets();
					EditorGUIUtility.PingObject(graphAsset);
				}
			}
		}

		public override string GetMenuName(IGraph graph) {
			return "Convert to Class Definition";
		}

		public override bool IsValid(IGraph graph) {
			if(graph is ClassScript classScript) {
				return classScript.inheritType == typeof(MonoBehaviour) || classScript.inheritType == typeof(ScriptableObject) || classScript.inheritType == typeof(object);
			}
			if(graph is GraphComponent) {
				return true;
			}
			return false;
		}
	}

	class ConverterToSingleton : GraphConverter {
		public override void Convert(IGraph graph) {
			if(graph is ClassDefinition) {
				var sourceGraph = graph as ClassDefinition;

				var graphAsset = ScriptGraph.CreateInstance<GraphSingleton>();
				graphAsset.name = graph.GetGraphName();
				graphAsset.@namespace = sourceGraph.GetGraphNamespace();
				graphAsset.usingNamespaces = new List<string>(sourceGraph.GetUsingNamespaces());

				SerializedGraph.Copy(sourceGraph.serializedGraph, graphAsset.serializedGraph, sourceGraph, graphAsset);
				var path = EditorUtility.SaveFilePanelInProject("Save graph", graph.GetGraphName(), "asset", "");
				if(string.IsNullOrEmpty(path) == false) {
					AssetDatabase.CreateAsset(graphAsset, path);
					ValidateGraph(graphAsset, supportAttribute: false, supportGeneric: false, supportModifier: false, supportConstructor: false);
					AssetDatabase.SaveAssets();
					EditorGUIUtility.PingObject(graphAsset);
				}
			}
			else if(graph is ClassScript) {
				var sourceGraph = graph as ClassScript;

				var graphAsset = ScriptGraph.CreateInstance<GraphSingleton>();
				graphAsset.name = graph.GetGraphName();
				graphAsset.@namespace = sourceGraph.GetGraphNamespace();
				graphAsset.usingNamespaces = new List<string>(sourceGraph.GetUsingNamespaces());

				SerializedGraph.Copy(sourceGraph.serializedGraph, graphAsset.serializedGraph, sourceGraph, graphAsset);
				var path = EditorUtility.SaveFilePanelInProject("Save graph", graph.GetGraphName(), "asset", "", System.IO.Path.GetDirectoryName(AssetDatabase.GetAssetPath(sourceGraph)));
				if(string.IsNullOrEmpty(path) == false) {
					AssetDatabase.CreateAsset(graphAsset, path);
					ValidateGraph(graphAsset, supportAttribute: false, supportGeneric: false, supportModifier: false, supportConstructor: false);
					AssetDatabase.SaveAssets();
					EditorGUIUtility.PingObject(graphAsset);
				}
			}
		}

		public override string GetMenuName(IGraph graph) {
			return "Convert to Singleton";
		}

		public override bool IsValid(IGraph graph) {
			if(graph is ClassScript classScript) {
				return classScript.inheritType == typeof(MonoBehaviour);
			}
			if(graph is ClassDefinition classDefinition && classDefinition.InheritType == typeof(MonoBehaviour)) {
				return true;
			}
			return false;
		}
	}
}
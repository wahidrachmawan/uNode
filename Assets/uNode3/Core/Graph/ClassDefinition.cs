using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;

namespace MaxyGames.UNode {
	public abstract class ClassDefinitionModel {
		/// <summary>
		/// The title of a model
		/// </summary>
		public abstract string title { get; }
		/// <summary>
		/// The native c# inherit type
		/// </summary>
		public abstract Type InheritType { get; }
		/// <summary>
		/// The inherit type for generated c# script.
		/// </summary>
		public abstract Type ScriptInheritType { get; }
		/// <summary>
		/// The base proxy type for generated c# script.
		/// </summary>
		public abstract Type ProxyScriptType { get; }

		public virtual object CreateInstance(string graphUID) => null;
		public virtual object CreateWrapperInstance(string graphUID) => null;
	}

	[Serializable]
	public class InheritedModel : ClassDefinitionModel {
		public ClassDefinition inheritFrom;

		public override string title => "Inherited";

		public override Type InheritType => inheritFrom?.ReflectionType ?? typeof(object);

		public override Type ScriptInheritType => InheritType;

		public override Type ProxyScriptType => InheritType;

		public override object CreateInstance(string graphUID) => inheritFrom?.model?.CreateInstance(graphUID);

		public override object CreateWrapperInstance(string graphUID) => inheritFrom?.model.CreateWrapperInstance(graphUID);
	}

	[GraphSystem(
		supportAttribute = false,
		supportGeneric = false,
		supportModifier = true,
		allowAutoCompile = true,
		isScriptGraph = false,
		generationKind = GenerationKind.Performance)]
	public class ClassDefinition : GraphAsset, IClassDefinition, IGraphWithVariables, IGraphWithProperties, IGraphWithFunctions, IStateGraph, IIndependentGraph, IClassIdentifier, IReflectionType, IInterfaceSystem {
		public string @namespace;
		public List<string> usingNamespaces = new List<string>() { "UnityEngine", "System.Collections", "System.Collections.Generic" };

		[SerializeReference]
		public ClassDefinitionModel model = new ClassComponentModel();
		public List<SerializedType> interfaces = new List<SerializedType>();

		[HideInInspector, SerializeField]
		public GeneratedScriptData scriptData = new GeneratedScriptData();

		#region Properties
		public string GeneratedTypeName => scriptData?.typeName;

		public ClassDefinitionModel GetModel() {
			if(model == null) {
				model = new ClassComponentModel();
			}
			return model;
		}

		public virtual string GraphName {
			get {
				var nm = GraphData.name;
				//return !string.IsNullOrEmpty(Name) ? Name.Replace(' ', '_') : "_" + Mathf.Abs(GetHashCode());
				if(string.IsNullOrEmpty(nm)) {
					if(string.IsNullOrEmpty(scriptData.fileName) && uNodeUtility.IsInMainThread) {
						try {
							scriptData.fileName = this.name;
						}
						//Ensure to skip any error regarding `GetName` is not allowed to be called during serialization.
						catch { }
					}
					return scriptData.fileName;
				}
				return nm;
			}
		}

		public virtual string FullGraphName {
			get {
				string ns = Namespace;
				if(!string.IsNullOrEmpty(ns)) {
					return ns + "." + GraphName;
				} else {
					return GraphName;
				}
			}
		}

		public string Namespace {
			get {
				return @namespace;
			}
			set => @namespace = value;
		}

		public Type InheritType => GetModel().InheritType ?? typeof(object);
		public bool CanCreateStateGraph => InheritType.IsCastableTo(typeof(MonoBehaviour));

		public List<string> UsingNamespaces {
			get => usingNamespaces;
			set => usingNamespaces = value;
		}

		public string uniqueIdentifier => FullGraphName;

		GeneratedScriptData ITypeWithScriptData.ScriptData => scriptData;

		RuntimeType _runtimeType;
		public RuntimeType ReflectionType {
			get {
				if(_runtimeType == null) {
					_runtimeType = new RuntimeGraphType(this);
				}
				return _runtimeType;
			}
		}

		List<SerializedType> IInterfaceSystem.Interfaces => interfaces;
		#endregion

		private void OnValidate() {
			scriptData.fileName = this.name;
		}
	}
}
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;

namespace MaxyGames.UNode {
	[GraphSystem(
		supportAttribute = false,
		supportGeneric = false,
		supportModifier = false,
		allowAutoCompile = true,
		isScriptGraph = false,
		generationKind = GenerationKind.Performance)]
	public class GraphInterface : GraphAsset, IClassGraph, IGraphWithProperties, IGraphWithFunctions, IIndependentGraph, IClassIdentifier, IReflectionType, IInterfaceSystem {
		public string @namespace;
		public List<string> usingNamespaces = new List<string>() { "UnityEngine", "System.Collections", "System.Collections.Generic" };

		public List<SerializedType> interfaces = new List<SerializedType>();

		[HideInInspector, SerializeField]
		public GeneratedScriptData scriptData = new GeneratedScriptData();

		#region Properties
		public string GeneratedTypeName => scriptData?.typeName;

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
				}
				else {
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

		public Type InheritType => null;

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

		#region Unity Events
		private void OnValidate() {
			scriptData.fileName = this.name;
		}
		#endregion

		public override Type GetIcon() {
			if(icon == null) {
				return typeof(TypeIcons.InterfaceIcon);
			}
			return base.GetIcon();
		}
	}
}
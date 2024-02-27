using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;

namespace MaxyGames.UNode {
	[GraphSystem(isScriptGraph = true, supportGeneric = true, generationKind = GenerationKind.Compatibility)]
	public class ClassScript : GraphAsset, IScriptGraphType, IClassGraph, IClassSystem, IStateGraph, IReflectionType, IClassModifier, IInterfaceSystem {
		public ScriptTypeData scriptTypeData = new ScriptTypeData();
		public SerializedType inheritType = typeof(object);
		public ClassModifier modifier = new ClassModifier();

		public List<SerializedType> interfaces = new List<SerializedType>();

		[HideInInspector, SerializeField]
		public GeneratedScriptData scriptData = new GeneratedScriptData();

		public override Type GetIcon() {
			if(icon == null) {
				if(inheritType == typeof(ValueType)) {
					return typeof(TypeIcons.StructureIcon);
				}
				else {
					return typeof(TypeIcons.ClassIcon);
				}
			}
			return base.GetIcon();
		}

		/// <summary>
		/// The graph name, by default this should be same with the DisplayName.
		/// This also used for the class name for generating script so this should be unique without spaces or symbol.
		/// </summary>
		public string GraphName {
			get {
				if(uNodeUtility.IsInMainThread) {
					try {
						GraphData.name = name;
					}
					//Ensure to skip any error regarding `GetName` is not allowed to be called during serialization.
					catch { }
				}
				var nm = GraphData.name;
				if (string.IsNullOrEmpty(nm)) {
					if(string.IsNullOrEmpty(scriptData.fileName)) {
						if(scriptTypeData.scriptGraphReference != null) {
							if(uNodeUtility.IsInMainThread) {
								scriptData.fileName = scriptTypeData.scriptGraphReference.name;
							} else if(scriptTypeData.scriptGraphReference is ScriptGraph scriptGraph) {
								scriptData.fileName = scriptGraph.ScriptData.fileName;
							}
						}
					}
					return scriptData.fileName;
				}
				return nm;
			}
		}

		/// <summary>
		/// The full graph name including the namespaces
		/// </summary>
		public string FullGraphName {
			get {
				string ns = Namespace;
				if (!string.IsNullOrEmpty(ns)) {
					return ns + "." + GraphName;
				}
				else {
					return GraphName;
				}
			}
		}

		bool IStateGraph.CanCreateStateGraph => inheritType != null && inheritType.type.IsCastableTo(typeof(MonoBehaviour));
		public string GeneratedTypeName => scriptData.typeName;
		public string Namespace => scriptTypeData.scriptGraph.Namespace;
		Type IClassGraph.InheritType => inheritType?.type ?? typeof(object);
		GeneratedScriptData ITypeWithScriptData.ScriptData => scriptData;
		ScriptTypeData IScriptGraphType.ScriptTypeData => scriptTypeData;
		string IScriptGraphType.ScriptName => GraphName;

		RuntimeType _runtimeType;
		RuntimeType IReflectionType.ReflectionType {
			get {
				if(_runtimeType == null) {
					_runtimeType = new RuntimeNativeGraph(this);
				}
				return _runtimeType;
			}
		}

		List<SerializedType> IInterfaceSystem.Interfaces {
			get => interfaces;
		}

		private void OnValidate() {
			GraphData.name = name;
			if(scriptTypeData.scriptGraphReference != null) {
				scriptData.fileName = scriptTypeData.scriptGraphReference.name;
			}
		}

		ClassModifier IClassModifier.GetModifier() {
			return modifier;
		}
	}
}

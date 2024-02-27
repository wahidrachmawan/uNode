using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;

namespace MaxyGames.UNode {
	[GraphSystem(isScriptGraph = true, generationKind = GenerationKind.Compatibility)]
	public class InterfaceScript : GraphAsset, IScriptGraphType, IScriptInterface, IInterfaceSystem, IReflectionType, IGraphWithAttributes, IGraphWithProperties, IGraphWithFunctions {
        public InterfaceModifier modifier = new InterfaceModifier();
        public List<SerializedType> interfaces = new List<SerializedType>();

        public ScriptTypeData scriptTypeData = new ScriptTypeData();
		[HideInInspector, SerializeField]
		public GeneratedScriptData scriptData = new GeneratedScriptData();

		public override Type GetIcon() {
			if(icon == null) {
				return typeof(TypeIcons.InterfaceIcon);
			}
			return base.GetIcon();
		}

		public string ScriptName {
			get {
				if(uNodeUtility.IsInMainThread) {
					try {
						GraphData.name = name;
					}
					//Ensure to skip any error regarding `GetName` is not allowed to be called during serialization.
					catch { }
				}
				var nm = GraphData.name;
				if(string.IsNullOrEmpty(nm)) {
					if(scriptTypeData.scriptGraphReference != null) {
						if(uNodeUtility.IsInMainThread) {
							nm = scriptTypeData.scriptGraphReference.name;
							GraphData.name = nm;
						}
						else if(scriptTypeData.scriptGraphReference is ScriptGraph scriptGraph) {
							nm = scriptGraph.ScriptData.fileName;
							GraphData.name = nm;
						}
					}
				}
				return nm;
			}
		}

		private void OnValidate() {
			GraphData.name = name;
		}


		RuntimeType _runtimeType;
		RuntimeType IReflectionType.ReflectionType {
			get {
				if(_runtimeType == null) {
					_runtimeType = new RuntimeNativeGraph(this);
				}
				return _runtimeType;
			}
		}

		ScriptTypeData IScriptGraphType.ScriptTypeData => scriptTypeData;

        List<SerializedType> IInterfaceSystem.Interfaces => interfaces;
		GeneratedScriptData ITypeWithScriptData.ScriptData => scriptData;
		InterfaceModifier IInterfaceModifier.GetModifier() => modifier;
	}
}
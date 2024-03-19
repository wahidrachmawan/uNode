using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;

namespace MaxyGames.UNode {
	public class EnumScript : ScriptableObject, IIcon, IScriptGraphType, ITypeWithScriptData, IReflectionType, IAttributeSystem, ISummary {
		[System.Serializable]
		public class Enumerator {
			public string id = uNodeUtility.GenerateUID();
			public string name;
			//public string value;
		}
		public Texture2D icon;
		public string summary;
		public EnumModifier modifier = new EnumModifier();
		public List<Enumerator> enumerators = new List<Enumerator>();
		public List<AttributeData> attributes = new List<AttributeData>();

		public ScriptTypeData scriptTypeData = new ScriptTypeData();
		[HideInInspector, SerializeField]
		public GeneratedScriptData scriptData = new GeneratedScriptData();

		private string _scriptName;
		public string ScriptName {
			get {
				if(uNodeUtility.IsInMainThread) {
					try {
						_scriptName = name;
					}
					//Ensure to skip any error regarding `GetName` is not allowed to be called during serialization.
					catch { }
				}
				var nm = _scriptName;
				if(string.IsNullOrEmpty(nm)) {
					if(scriptTypeData.scriptGraphReference != null) {
						if(uNodeUtility.IsInMainThread) {
							return _scriptName = scriptTypeData.scriptGraphReference.name;
						}
						else if(scriptTypeData.scriptGraphReference is ScriptGraph scriptGraph) {
							return _scriptName = scriptGraph.ScriptData.fileName;
						}
					}
				}
				return nm;
			}
		}

		ScriptTypeData IScriptGraphType.ScriptTypeData => scriptTypeData;
		List<AttributeData> IAttributeSystem.Attributes => attributes;
		string ISummary.GetSummary() => summary;

		public Type GetIcon() {
			if(icon != null) {
				return TypeIcons.FromTexture(icon);
			}
			return typeof(TypeIcons.EnumIcon);
		}

		RuntimeType _runtimeType;
		RuntimeType IReflectionType.ReflectionType {
			get {
				if(_runtimeType == null) {
					_runtimeType = new RuntimeNativeEnum(this);
				}
				return _runtimeType;
			}
		}

		GeneratedScriptData ITypeWithScriptData.ScriptData => scriptData;
	}
}
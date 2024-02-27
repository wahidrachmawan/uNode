using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MaxyGames.UNode {
	[System.Serializable]
	public sealed class ScriptTypeList : IEnumerable<Object> {
		[SerializeField]
		internal List<Object> references = new List<Object>();

		public IEnumerable<IScriptGraphType> GetTypes() {
			foreach(var r in references) {
				if (r is IScriptGraphType type)
					yield return type;
			}
		}

		public void AddType(IScriptGraphType type, IScriptGraph scriptGraph) {
			if(type is Object) {
				references.Add(type as Object);
				type.ScriptTypeData.scriptGraph = scriptGraph;
			} else {
				throw null;
			}
		}

		public void RemoveType(IScriptGraphType type) {
			references.Remove(type as Object);
		}

		IEnumerator<Object> IEnumerable<Object>.GetEnumerator() {
			return references.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator() {
			return references.GetEnumerator();
		}
	}

	[System.Serializable]
	public sealed class ScriptTypeData {
		[SerializeField]
		public Object scriptGraphReference;
		[SerializeField]
		public string typeName;

		public IScriptGraph scriptGraph {
			get => scriptGraphReference as IScriptGraph;
			set => scriptGraphReference = value as Object;
		}
	}

	public sealed class ScriptGraph : ScriptableObject, IScriptGraph, IIcon {
		[SerializeField]
		private string @namespace;
		[SerializeField]
		private List<string> usingNamespaces = new List<string>() { "UnityEngine", "System.Collections.Generic" };
		[SerializeField]
		private ScriptTypeList typeList = new ScriptTypeList();
		[SerializeField]
		private GeneratedScriptData scriptData = new GeneratedScriptData();

		public string Namespace {
			get => @namespace;
			set => @namespace = value;
		}
		public List<string> UsingNamespaces {
			get => usingNamespaces;
			set => usingNamespaces = value;
		}
		public ScriptTypeList TypeList => typeList;

		public GeneratedScriptData ScriptData => scriptData;

		public System.Type GetIcon() {
			return typeof(TypeIcons.ClassIcon);
		}

		private void OnValidate() {
			scriptData.fileName = name;
		}
	}
}
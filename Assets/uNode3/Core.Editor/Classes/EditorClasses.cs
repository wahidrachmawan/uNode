using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Object = UnityEngine.Object;

namespace MaxyGames.UNode.Editors {
	[Serializable]
	public class GraphSearchQuery {
		public enum SearchType {
			None,
			Node,
			Port,
			NodeType,
		}

		public List<string> query = new List<string>();
		public SearchType type = SearchType.None;

		public static HashSet<string> csharpKeyword = new HashSet<string>() {
			"false",
			"true",
			"null",
			"bool",
			"byte",
			"char",
			"decimal",
			"double",
			"float",
			"int",
			"long",
			"object",
			"sbyte",
			"short",
			"string",
			"uint",
			"ulong",
		};
	}

	public abstract class CreateNodeProcessor {
		public virtual int order { get; }

		/// <summary>
		/// Proccess create node processor.
		/// </summary>
		/// <param name="member">The member value</param>
		/// <param name="editorData">The graph editor data</param>
		/// <param name="position">The position for node to be created</param>
		/// <param name="onCreated">Invoke this when node is succesfull created</param>
		/// <returns></returns>
		public abstract bool Process(MemberData member, GraphEditorData editorData, Vector2 position, Action<Node> onCreated);
	}
}
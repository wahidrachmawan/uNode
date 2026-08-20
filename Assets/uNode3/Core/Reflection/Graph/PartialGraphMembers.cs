using System;
using System.Collections.Generic;

namespace MaxyGames.UNode {
	/// <summary>
	/// The kind of member declared in the hand-written half of a partial graph.
	/// </summary>
	public enum PartialMemberKind {
		Field,
		Property,
		Method,
	}

	/// <summary>
	/// Describes a single parameter of a <see cref="PartialMemberInfo"/>.
	/// </summary>
	public class PartialParameterInfo {
		public string name;
		public Type type;
		public RefKind refKind;
		public bool hasDefaultValue;
		public object defaultValue;
	}

	/// <summary>
	/// Describes a member that exists in the hand-written half of a `partial` graph.
	/// These are discovered from the project source, they are not authored in the graph.
	/// </summary>
	public class PartialMemberInfo {
		public PartialMemberKind kind;
		public string name;
		/// <summary>
		/// The field/property type, or the return type of a method.
		/// </summary>
		public Type type;
		public PartialParameterInfo[] parameters = Array.Empty<PartialParameterInfo>();
		public bool isStatic;
		public bool isPublic = true;
		public bool canRead = true;
		public bool canWrite = true;
		public string summary;

		/// <summary>
		/// The source file this member was found in, for diagnostics.
		/// </summary>
		public string sourcePath;

		public Type[] ParameterTypes() {
			if(parameters == null || parameters.Length == 0)
				return Type.EmptyTypes;
			var result = new Type[parameters.Length];
			for(int i = 0; i < parameters.Length; i++) {
				result[i] = parameters[i].type;
			}
			return result;
		}

		/// <summary>
		/// A signature usable for de-duplicating against graph-authored members.
		/// </summary>
		public string Signature() {
			if(kind != PartialMemberKind.Method)
				return name;
			var result = name + "(";
			for(int i = 0; i < parameters.Length; i++) {
				if(i != 0)
					result += ",";
				result += parameters[i].type != null ? parameters[i].type.FullName : "?";
			}
			return result + ")";
		}
	}

	/// <summary>
	/// The seam between the runtime reflection types (this assembly) and the editor-side
	/// source scanner that discovers the other half of a `partial` graph.
	/// The editor installs <see cref="provider"/> on load; in a build it stays null and
	/// every graph simply reports no external members.
	/// </summary>
	public static class PartialGraphMembers {
		private static readonly PartialMemberInfo[] none = Array.Empty<PartialMemberInfo>();

		/// <summary>
		/// Installed by the editor. Returns the members declared in the hand-written
		/// half of the given graph, or null when the graph is not partial.
		/// </summary>
		public static Func<GraphAsset, IList<PartialMemberInfo>> provider;

		public static IList<PartialMemberInfo> Get(GraphAsset graph) {
			if(provider == null || graph == null)
				return none;
			try {
				return provider(graph) ?? none;
			}
			catch(Exception ex) {
				//Never let a scanner failure break reflection on the graph.
				UnityEngine.Debug.LogException(ex);
				return none;
			}
		}
	}
}

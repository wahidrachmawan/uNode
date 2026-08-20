using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using UnityEditor;
using UnityEngine;

namespace MaxyGames.UNode.Editors {
	/// <summary>
	/// Discovers the hand-written half of a `partial` graph by scanning the project source,
	/// so the graph can bind to members it does not declare itself.
	///
	/// This only ever runs for graphs whose class modifier has `Partial` set, so projects
	/// that do not use the feature pay nothing.
	/// </summary>
	[InitializeOnLoad]
	public static class PartialGraphSourceScanner {
		/// <summary>
		/// A sibling `partial` declaration found in the project source.
		/// </summary>
		public class Declaration {
			public string path;
			public string typeName;
			public string typeNamespace;
			public List<PartialMemberInfo> members = new List<PartialMemberInfo>();
			/// <summary>
			/// Names of members whose type could not be resolved from source alone.
			/// </summary>
			public List<string> unresolved = new List<string>();
		}

		public class ScanResult {
			public List<Declaration> declarations = new List<Declaration>();
			public List<PartialMemberInfo> members = new List<PartialMemberInfo>();
		}

		static PartialGraphSourceScanner() {
			PartialGraphMembers.provider = graph => Scan(graph)?.members;
		}

		#region Cache
		private static Dictionary<string, ScanResult> m_cache = new Dictionary<string, ScanResult>();
		private static Dictionary<string, List<string>> m_partialIndex;

		/// <summary>
		/// Matches a partial type declaration, capturing the type name. Used only as a
		/// prefilter so Roslyn is handed a handful of files instead of the whole project.
		/// </summary>
		private static readonly System.Text.RegularExpressions.Regex partialPattern =
			new System.Text.RegularExpressions.Regex(
				@"\bpartial\s+(?:class|struct)\s+([A-Za-z_][A-Za-z0-9_]*)",
				System.Text.RegularExpressions.RegexOptions.Compiled);

		/// <summary>
		/// Drops every cached scan. Called whenever project scripts change.
		/// </summary>
		public static void InvalidateCache() {
			m_cache.Clear();
			m_typeCache.Clear();
			m_partialIndex = null;
		}

		private class Watcher : AssetPostprocessor {
			private static void OnPostprocessAllAssets(
				string[] imported, string[] deleted, string[] moved, string[] movedFrom) {
				if(HasScript(imported) || HasScript(deleted) || HasScript(moved) || HasScript(movedFrom)) {
					InvalidateCache();
				}
			}

			private static bool HasScript(string[] paths) {
				for(int i = 0; i < paths.Length; i++) {
					if(paths[i].EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
						return true;
				}
				return false;
			}
		}

		/// <summary>
		/// Every hand-written `partial class`/`partial struct` in the project, keyed by type name.
		/// Built once per script reload with a regex prefilter, so a lookup costs nothing after that.
		/// </summary>
		private static Dictionary<string, List<string>> GetPartialIndex() {
			if(m_partialIndex == null) {
				m_partialIndex = new Dictionary<string, List<string>>();
				//Only project scripts: the hand-written half of a graph lives in Assets,
				//and including packages made this scan several seconds slower.
				foreach(var guid in AssetDatabase.FindAssets("t:MonoScript", new[] { "Assets" })) {
					var path = AssetDatabase.GUIDToAssetPath(guid);
					if(string.IsNullOrEmpty(path) || !path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
						continue;
					//uNode own generated output is the half the graph already knows about.
					if(IsGeneratedPath(path))
						continue;
					string text;
					try {
						text = File.ReadAllText(path);
					}
					catch(Exception) {
						continue;
					}
					if(text.IndexOf("partial", StringComparison.Ordinal) < 0)
						continue;
					foreach(System.Text.RegularExpressions.Match match in partialPattern.Matches(text)) {
						var name = match.Groups[1].Value;
						if(!m_partialIndex.TryGetValue(name, out var paths)) {
							paths = new List<string>();
							m_partialIndex[name] = paths;
						}
						if(!paths.Contains(path)) {
							paths.Add(path);
						}
					}
				}
			}
			return m_partialIndex;
		}

		/// <summary>
		/// The source files declaring a hand-written `partial` type with the given name,
		/// regardless of namespace. Cheap: served from the index.
		/// </summary>
		public static IList<string> FindPartialDeclarationPaths(string typeName) {
			if(string.IsNullOrEmpty(typeName))
				return Array.Empty<string>();
			return GetPartialIndex().TryGetValue(typeName, out var paths) ? paths : (IList<string>)Array.Empty<string>();
		}

		/// <summary>
		/// True for the folders uNode generates into, which hold the half the graph already knows about.
		/// </summary>
		private static bool IsGeneratedPath(string path) {
			var normalized = path.Replace('\\', '/');
			return normalized.StartsWith(GenerationUtility.generatedPath.Replace('\\', '/'), StringComparison.OrdinalIgnoreCase) ||
				normalized.StartsWith(GenerationUtility.tempFolder.Replace('\\', '/'), StringComparison.OrdinalIgnoreCase);
		}

		private static string NormalizePath(string path) {
			if(string.IsNullOrEmpty(path))
				return string.Empty;
			try {
				return Path.GetFullPath(path).Replace('\\', '/');
			}
			catch(Exception) {
				return path.Replace('\\', '/');
			}
		}

		/// <summary>
		/// True when the file is this graph own generated C#. Compiling a graph manually writes
		/// the script beside the asset rather than into the generated folder, and that output is
		/// `partial` too, so without this check a compiled graph reports every one of its own
		/// members as a duplicate of itself.
		/// </summary>
		private static bool IsOwnGeneratedScript(GraphAsset graph, string path) {
			var normalized = NormalizePath(path);
			if(normalized.Length == 0)
				return false;

			//The manual-compile convention: `Foo.asset` generates `Foo.cs` next to it.
			var assetPath = AssetDatabase.GetAssetPath(graph);
			if(!string.IsNullOrEmpty(assetPath)) {
				var beside = NormalizePath(Path.ChangeExtension(assetPath, ".cs"));
				if(string.Equals(normalized, beside, StringComparison.OrdinalIgnoreCase))
					return true;
			}

			//And uNode records where it last generated this graph, wherever that was.
			try {
				var data = GenerationUtility.GetGraphData(graph);
				if(data != null && !string.IsNullOrEmpty(data.path)) {
					if(string.Equals(normalized, NormalizePath(data.path), StringComparison.OrdinalIgnoreCase))
						return true;
				}
			}
			catch(Exception) {
				//Persistence data is a convenience here, never a hard requirement.
			}
			return false;
		}

		/// <summary>
		/// The other halves of a graph: every indexed `partial` declaration of that name, minus the
		/// graph own generated output. Usually a hand-written file, but it can also be the generated
		/// output of a second graph that declares the same class.
		/// </summary>
		public static IList<string> FindOtherHalfDeclarationPaths(GraphAsset graph, string typeName) {
			var result = new List<string>();
			foreach(var path in FindPartialDeclarationPaths(typeName)) {
				if(IsOwnGeneratedScript(graph, path))
					continue;
				result.Add(path);
			}
			return result;
		}
		#endregion

		#region Scan
		/// <summary>
		/// Finds the hand-written half of the given graph, or null when the graph is not partial.
		/// </summary>
		public static ScanResult Scan(GraphAsset graph) {
			if(graph == null)
				return null;
			var modifier = graph as IClassModifier;
			if(modifier == null || modifier.GetModifier().Partial == false)
				return null;

			var name = graph.GetGraphName();
			if(string.IsNullOrEmpty(name))
				return null;
			var ns = graph.GetGraphNamespace() ?? string.Empty;
			//Keyed on the asset, not the type name: two graphs in this project can share a
			//name, and each excludes a different generated script.
			var key = uNodeUtility.GetObjectID(graph) + ":" + (string.IsNullOrEmpty(ns) ? name : ns + "." + name);

			if(m_cache.TryGetValue(key, out var cached))
				return cached;

			var result = new ScanResult();
			foreach(var path in FindOtherHalfDeclarationPaths(graph, name)) {
				string text;
				try {
					text = File.ReadAllText(path);
				}
				catch(Exception) {
					//Unreadable file, nothing useful to do about it here.
					continue;
				}
				try {
					CollectFromFile(path, text, name, ns, result);
				}
				catch(Exception ex) {
					Debug.LogWarning($"[uNode] Failed to scan '{path}' for partial members of '{key}':\n{ex}");
				}
			}
			foreach(var declaration in result.declarations) {
				result.members.AddRange(declaration.members);
			}
			m_cache[key] = result;
			return result;
		}

		private static void CollectFromFile(string path, string text, string name, string ns, ScanResult result) {
			var root = RoslynUtility.GetSyntaxTree(text);
			foreach(var syntax in root.DescendantNodes().OfType<TypeDeclarationSyntax>()) {
				if(syntax.Identifier.ValueText != name)
					continue;
				if(!syntax.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)))
					continue;
				//A nested type shares its name with nothing at namespace scope, so
				//`Outer.Thing` must never be mistaken for a graph called `Thing`.
				if(syntax.Parent is TypeDeclarationSyntax)
					continue;
				//`Thing<T>` is a different type from the `Thing` a graph generates.
				if(syntax.TypeParameterList != null)
					continue;
				if(GetNamespace(syntax) != ns)
					continue;
				var declaration = new Declaration() {
					path = path,
					typeName = name,
					typeNamespace = ns,
				};
				CollectMembers(syntax, declaration, GetScopeNamespaces(root, syntax, ns));
				result.declarations.Add(declaration);
			}
		}

		private static string GetNamespace(SyntaxNode node) {
			var names = new List<string>();
			for(var current = node.Parent; current != null; current = current.Parent) {
				if(current is NamespaceDeclarationSyntax ns) {
					names.Insert(0, ns.Name.ToString());
				}
				else if(current is FileScopedNamespaceDeclarationSyntax fileScoped) {
					names.Insert(0, fileScoped.Name.ToString());
				}
			}
			return string.Join(".", names);
		}
		#endregion

		#region Members
		private static void CollectMembers(
			TypeDeclarationSyntax syntax, Declaration declaration, IList<string> namespaces) {
			foreach(var member in syntax.Members) {
				var field = member as FieldDeclarationSyntax;
				if(field != null) {
					var type = ResolveType(field.Declaration.Type, namespaces);
					foreach(var variable in field.Declaration.Variables) {
						if(type == null) {
							declaration.unresolved.Add(variable.Identifier.ValueText);
							continue;
						}
						declaration.members.Add(new PartialMemberInfo() {
							kind = PartialMemberKind.Field,
							name = variable.Identifier.ValueText,
							type = type,
							isStatic = HasModifier(field.Modifiers, SyntaxKind.StaticKeyword),
							isPublic = IsPublic(field.Modifiers),
							summary = GetSummary(field),
							sourcePath = declaration.path,
						});
					}
					continue;
				}
				var property = member as PropertyDeclarationSyntax;
				if(property != null) {
					//An explicit interface implementation is not reachable as a member of the type.
					if(property.ExplicitInterfaceSpecifier != null)
						continue;
					var type = ResolveType(property.Type, namespaces);
					if(type == null) {
						declaration.unresolved.Add(property.Identifier.ValueText);
						continue;
					}
					//An expression bodied property (=> value) is read only.
					bool canRead = true, canWrite = false;
					if(property.AccessorList != null) {
						canRead = property.AccessorList.Accessors.Any(a => a.IsKind(SyntaxKind.GetAccessorDeclaration));
						canWrite = property.AccessorList.Accessors.Any(
							a => a.IsKind(SyntaxKind.SetAccessorDeclaration) || a.IsKind(SyntaxKind.InitAccessorDeclaration));
					}
					declaration.members.Add(new PartialMemberInfo() {
						kind = PartialMemberKind.Property,
						name = property.Identifier.ValueText,
						type = type,
						canRead = canRead,
						canWrite = canWrite,
						isStatic = HasModifier(property.Modifiers, SyntaxKind.StaticKeyword),
						isPublic = IsPublic(property.Modifiers),
						summary = GetSummary(property),
						sourcePath = declaration.path,
					});
					continue;
				}
				var method = member as MethodDeclarationSyntax;
				if(method != null) {
					//Generic methods cannot be represented as a plain reflection member here.
					if(method.TypeParameterList != null)
						continue;
					//An explicit interface implementation is not reachable as a member of the type.
					if(method.ExplicitInterfaceSpecifier != null)
						continue;
					var returnType = ResolveType(method.ReturnType, namespaces);
					if(returnType == null) {
						declaration.unresolved.Add(method.Identifier.ValueText);
						continue;
					}
					var parameters = new List<PartialParameterInfo>();
					bool valid = true;
					foreach(var parameter in method.ParameterList.Parameters) {
						var parameterType = ResolveType(parameter.Type, namespaces);
						if(parameterType == null) {
							valid = false;
							break;
						}
						parameters.Add(new PartialParameterInfo() {
							name = parameter.Identifier.ValueText,
							type = parameterType,
							refKind = GetRefKind(parameter),
							hasDefaultValue = parameter.Default != null,
						});
					}
					if(!valid) {
						declaration.unresolved.Add(method.Identifier.ValueText);
						continue;
					}
					declaration.members.Add(new PartialMemberInfo() {
						kind = PartialMemberKind.Method,
						name = method.Identifier.ValueText,
						type = returnType,
						parameters = parameters.ToArray(),
						isStatic = HasModifier(method.Modifiers, SyntaxKind.StaticKeyword),
						isPublic = IsPublic(method.Modifiers),
						summary = GetSummary(method),
						sourcePath = declaration.path,
					});
				}
			}
		}

		private static RefKind GetRefKind(ParameterSyntax parameter) {
			foreach(var token in parameter.Modifiers) {
				if(token.IsKind(SyntaxKind.OutKeyword))
					return RefKind.Out;
				if(token.IsKind(SyntaxKind.RefKeyword))
					return RefKind.Ref;
				if(token.IsKind(SyntaxKind.InKeyword))
					return RefKind.In;
			}
			return RefKind.None;
		}

		private static bool HasModifier(SyntaxTokenList modifiers, SyntaxKind kind) {
			return modifiers.Any(m => m.IsKind(kind));
		}

		private static bool IsPublic(SyntaxTokenList modifiers) {
			return HasModifier(modifiers, SyntaxKind.PublicKeyword);
		}

		/// <summary>
		/// Resolves a type from syntax alone. Walks the syntax rather than the printed name so
		/// every part of a nested generic gets qualified against the namespaces in scope,
		/// which a plain string lookup cannot do (`Dictionary&lt;string, Vector3&gt;`).
		/// </summary>
		private static Type ResolveType(TypeSyntax syntax, IList<string> namespaces) {
			if(syntax == null)
				return null;

			if(syntax is ArrayTypeSyntax array) {
				var element = ResolveType(array.ElementType, namespaces);
				if(element == null)
					return null;
				//Applied outermost-last so `int[][]` and `int[,]` both come out right.
				for(int i = array.RankSpecifiers.Count - 1; i >= 0; i--) {
					var rank = array.RankSpecifiers[i].Rank;
					element = rank > 1 ? element.MakeArrayType(rank) : element.MakeArrayType();
				}
				return element;
			}
			if(syntax is NullableTypeSyntax nullable) {
				var element = ResolveType(nullable.ElementType, namespaces);
				if(element == null)
					return null;
				//A nullable reference type annotation does not change the runtime type.
				return element.IsValueType ? typeof(Nullable<>).MakeGenericType(element) : element;
			}
			if(syntax is GenericNameSyntax generic) {
				return ResolveGeneric(generic, string.Empty, namespaces);
			}
			if(syntax is QualifiedNameSyntax qualified) {
				//`System.Collections.Generic.List<int>` keeps its generic part on the right.
				if(qualified.Right is GenericNameSyntax nested) {
					return ResolveGeneric(nested, qualified.Left.ToString() + ".", namespaces);
				}
			}

			var name = syntax.ToString();
			//`var` and inferred types cannot be resolved without a semantic model.
			if(name == "var")
				return null;
			return ResolveName(name, namespaces);
		}

		private static Type ResolveGeneric(GenericNameSyntax generic, string prefix, IList<string> namespaces) {
			var arguments = generic.TypeArgumentList.Arguments;
			var types = new Type[arguments.Count];
			for(int i = 0; i < arguments.Count; i++) {
				types[i] = ResolveType(arguments[i], namespaces);
				if(types[i] == null)
					return null;
			}
			var definition = ResolveName(prefix + generic.Identifier.ValueText + "`" + arguments.Count, namespaces);
			if(definition == null || !definition.IsGenericTypeDefinition)
				return null;
			try {
				return definition.MakeGenericType(types);
			}
			catch(Exception) {
				//Generic constraints the source scan cannot check.
				return null;
			}
		}

		/// <summary>
		/// Memoises name lookups. A failed lookup is the expensive case - it walks every namespace
		/// in scope, hitting the assembly search each time - and the same names repeat constantly
		/// across a file, so misses are cached as null just like hits.
		/// </summary>
		private static Dictionary<string, Type> m_typeCache = new Dictionary<string, Type>();

		/// <summary>
		/// Resolves a type name, retrying against every namespace the file has in scope
		/// so unqualified names like `Vector3` are found.
		/// </summary>
		private static Type ResolveName(string name, IList<string> namespaces) {
			var key = ScopeKey(namespaces) + "|" + name;
			if(m_typeCache.TryGetValue(key, out var cached))
				return cached;
			var type = Resolve(name, namespaces);
			m_typeCache[key] = type;
			return type;
		}

		private static Type Resolve(string name, IList<string> namespaces) {
			var type = RoslynUtility.GetTypeFromTypeName(name);
			if(type != null)
				return type;
			type = name.ToType(false);
			if(type != null)
				return type;
			if(namespaces != null) {
				for(int i = 0; i < namespaces.Count; i++) {
					type = (namespaces[i] + "." + name).ToType(false);
					if(type != null)
						return type;
				}
			}
			return null;
		}

		private static IList<string> m_lastScope;
		private static string m_lastScopeKey;
		/// <summary>
		/// A stable id for a set of in-scope namespaces. The same list instance is reused for
		/// every member of a declaration, so the common case is a reference-equality hit.
		/// </summary>
		private static string ScopeKey(IList<string> namespaces) {
			if(namespaces == null)
				return string.Empty;
			if(ReferenceEquals(namespaces, m_lastScope))
				return m_lastScopeKey;
			m_lastScope = namespaces;
			m_lastScopeKey = string.Join(";", namespaces);
			return m_lastScopeKey;
		}

		/// <summary>
		/// The namespaces in scope for a declaration: every `using` in the file, plus the
		/// namespace the type itself is declared in.
		/// </summary>
		private static List<string> GetScopeNamespaces(CompilationUnitSyntax root, TypeDeclarationSyntax syntax, string ns) {
			var result = new List<string>();
			foreach(var directive in root.Usings) {
				//Alias and `using static` directives do not introduce a plain namespace scope.
				if(directive.Alias != null || directive.StaticKeyword.IsKind(SyntaxKind.StaticKeyword))
					continue;
				result.Add(directive.Name.ToString());
			}
			foreach(var declaration in syntax.Ancestors().OfType<BaseNamespaceDeclarationSyntax>()) {
				foreach(var directive in declaration.Usings) {
					if(directive.Alias != null || directive.StaticKeyword.IsKind(SyntaxKind.StaticKeyword))
						continue;
					result.Add(directive.Name.ToString());
				}
			}
			if(!string.IsNullOrEmpty(ns) && !result.Contains(ns)) {
				result.Add(ns);
			}
			return result;
		}

		private static string GetSummary(SyntaxNode node) {
			foreach(var trivia in node.GetLeadingTrivia()) {
				if(trivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia) ||
					trivia.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia)) {
					var text = trivia.ToString();
					var start = text.IndexOf("<summary>", StringComparison.Ordinal);
					var end = text.IndexOf("</summary>", StringComparison.Ordinal);
					if(start >= 0 && end > start) {
						var summary = text.Substring(start + "<summary>".Length, end - start - "<summary>".Length);
						var lines = summary.Split('\n')
							.Select(line => line.Trim().TrimStart('/').Trim())
							.Where(line => line.Length > 0);
						return string.Join(" ", lines);
					}
				}
			}
			return null;
		}
		#endregion
	}
}

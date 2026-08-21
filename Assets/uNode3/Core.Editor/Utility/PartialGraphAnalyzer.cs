using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MaxyGames.UNode.Editors.Analyzer {
	/// <summary>
	/// Validates the relationship between a `partial` graph and its other half.
	/// </summary>
	class PartialGraphAnalyzer : GraphAnalyzer {
		public override bool IsValidAnalyzerForGraph(Type graphType) {
			return graphType.HasImplementInterface(typeof(IScriptGraphType));
		}

		public override void CheckGraphErrors(ErrorAnalyzer analyzer, IGraph graph) {
			var asset = graph as GraphAsset;
			if(asset == null)
				return;
			var graphData = graph.GraphData;
			if(graphData == null)
				return;
			var modifier = asset as IClassModifier;
			if(modifier == null)
				return;

			var name = asset.GetGraphName();
			if(string.IsNullOrEmpty(name))
				return;
			var ns = asset.GetGraphNamespace() ?? string.Empty;
			var fullName = string.IsNullOrEmpty(ns) ? name : ns + "." + name;

			if(modifier.GetModifier().Partial == false) {
				CheckUnmarkedPartial(analyzer, graphData, asset, modifier, name, fullName);
				return;
			}

			var result = PartialGraphSourceScanner.Scan(asset);
			if(result == null)
				return;

			if(result.declarations.Count == 0) {
				CheckMissingHalf(analyzer, graphData, asset, name, ns, fullName);
				return;
			}

			CheckCollisions(analyzer, graphData, asset, result, fullName);
			CheckUnresolved(analyzer, graphData, result);
		}

		/// <summary>
		/// A hand-written `partial` declaration exists but the graph does not generate one,
		/// so the two halves will not merge (CS0260).
		/// </summary>
		private void CheckUnmarkedPartial(
			ErrorAnalyzer analyzer, UGraphElement graphData, GraphAsset asset, IClassModifier modifier, string name, string fullName) {
			var paths = PartialGraphSourceScanner.FindOtherHalfDeclarationPaths(asset, name);
			if(paths.Count == 0)
				return;
			void autoFix() {
				modifier.GetModifier().Partial = true;
				uNodeEditorUtility.MarkDirty(graphData.graphContainer as UnityEngine.Object);
				PartialGraphSourceScanner.InvalidateCache();
			}
			analyzer.RegisterWarning(graphData,
				$"'{paths[0]}' declares a partial type named '{name}', but this graph is not marked 'Partial'.\n" +
				$"The generated '{fullName}' will not merge with it. Enable the 'Partial' class modifier to combine them.",
				autoFix);
		}

		/// <summary>
		/// The graph is marked `partial` but nothing in the project declares the other half.
		/// </summary>
		private void CheckMissingHalf(
			ErrorAnalyzer analyzer, UGraphElement graphData, GraphAsset asset, string name, string ns, string fullName) {
			void autoFix() {
				CreateStub(asset, name, ns);
			}
			analyzer.RegisterWarning(graphData,
				$"This graph is marked 'Partial' but no other 'partial' declaration of '{fullName}' was found in the project.\n" +
				"The generated code is still valid, but nothing is being merged into it.",
				autoFix);
		}

		/// <summary>
		/// A member exists on both sides, which the C# compiler rejects as a duplicate (CS0102/CS0111).
		/// </summary>
		private void CheckCollisions(
			ErrorAnalyzer analyzer, UGraphElement graphData, GraphAsset asset,
			PartialGraphSourceScanner.ScanResult result, string fullName) {
			var declared = new Dictionary<string, string>();
			foreach(var variable in asset.GetVariables()) {
				declared[variable.name] = "variable";
			}
			foreach(var property in asset.GetProperties()) {
				declared[property.name] = "property";
			}
			foreach(var function in asset.GetFunctions()) {
				//Overloads are legal, so functions are keyed on their full signature.
				var key = function.name + "(" +
					string.Join(",", function.Parameters.Select(p => p.Type != null ? p.Type.FullName : "?")) + ")";
				declared[key] = "function";
			}
			foreach(var member in result.members) {
				var key = member.Signature();
				if(declared.TryGetValue(key, out var kind) == false)
					continue;
				//A bodyless `partial` function in the graph is a declaration, not a duplicate.
				if(member.kind == PartialMemberKind.Method && IsPartialDeclaration(asset, member))
					continue;
				analyzer.RegisterError(graphData,
					$"'{member.name}' is declared both by this graph ({kind}) and by the other half in " +
					$"'{member.sourcePath}'. '{fullName}' will not compile until one of them is removed.");
			}
		}

		/// <summary>
		/// True when the graph side of this method is a bodyless `partial` declaration,
		/// which is meant to be implemented by the other half.
		/// </summary>
		private bool IsPartialDeclaration(GraphAsset asset, PartialMemberInfo member) {
			foreach(var function in asset.GetFunctions()) {
				if(function.name != member.name)
					continue;
				if(function.modifier != null && function.modifier.Partial)
					return true;
			}
			return false;
		}

		/// <summary>
		/// Members the scanner had to skip, so they do not silently go missing from the graph.
		/// </summary>
		private void CheckUnresolved(
			ErrorAnalyzer analyzer, UGraphElement graphData, PartialGraphSourceScanner.ScanResult result) {
			var names = new List<string>();
			foreach(var declaration in result.declarations) {
				names.AddRange(declaration.unresolved);
			}
			if(names.Count == 0)
				return;
			analyzer.RegisterWarning(graphData,
				"These members of the other half could not be resolved from source and are not available in the graph: " +
				string.Join(", ", names) + ".\n" +
				"This usually means their type comes from an assembly the graph does not reference, or it is generic or inferred.");
		}

		private static void CreateStub(GraphAsset asset, string name, string ns) {
			var assetPath = AssetDatabase.GetAssetPath(asset);
			var directory = string.IsNullOrEmpty(assetPath) ? "Assets" : Path.GetDirectoryName(assetPath);
			var path = AssetDatabase.GenerateUniqueAssetPath(
				(directory + "/" + name + ".Partial.cs").Replace('\\', '/'));

			var contents = string.Empty;
			var indent = string.Empty;
			if(!string.IsNullOrEmpty(ns)) {
				contents += "namespace " + ns + " {\n";
				indent = "\t";
			}
			contents += indent + "partial class " + name + " {\n";
			contents += indent + "\t//Members declared here become available inside the graph.\n";
			contents += indent + "}\n";
			if(!string.IsNullOrEmpty(ns)) {
				contents += "}\n";
			}

			File.WriteAllText(path, contents);
			AssetDatabase.ImportAsset(path);
			PartialGraphSourceScanner.InvalidateCache();
			Debug.Log($"[uNode] Created the other half of '{name}' at '{path}'.");
		}
	}
}

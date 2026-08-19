using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace MaxyGames.UNode.Editors {
	/// <summary>
	/// Scans all text-serialized (YAML) assets in the project for broken type
	/// references and lets you remap each to a currently-compiled type, then
	/// rewrites the underlying asset text on disk.
	///
	/// REQUIREMENTS / CAVEATS:
	/// - Project must use "Force Text" asset serialization
	///   (Edit > Project Settings > Editor > Asset Serialization Mode).
	///   Binary-serialized files are detected and skipped.
	/// - Commit / back up your project before applying fixes - this edits files on disk.
	/// - Only the type declaration itself is rewritten. The surrounding "data:" /
	///   field blocks are left untouched, so values are preserved as long as the
	///   new type still has matching field names/types. Renamed/removed fields
	///   just fall back to default on load.
	/// - For future SerializeReference renames, prefer adding
	///   [UnityEngine.Scripting.APIUpdating.MovedFrom] on the new class before
	///   shipping - Unity remaps automatically on load. This tool is for
	///   repairing damage that's already happened.
	/// </summary>
	class AssetMissingTypeFixer : EditorWindow {
		private enum TypeRefFormat {
			SerializeReferenceYaml, // type: {class: X, ns: Y, asm: Z}
			UNodeSerializedType     // serializedType: Namespace.Class
		}

		private struct TypeKey : IEquatable<TypeKey> {
			public TypeRefFormat Format;
			// SerializeReferenceYaml: Class = class name, Ns = namespace, Asm = assembly.
			// UNodeSerializedType:    Class = full dotted type name, Ns/Asm unused.
			public string Class;
			public string Ns;
			public string Asm;

			public bool Equals(TypeKey other) =>
				Format == other.Format && Class == other.Class && Ns == other.Ns && Asm == other.Asm;
			public override bool Equals(object obj) => obj is TypeKey k && Equals(k);
			public override int GetHashCode() => (Format + "|" + Class + "|" + Ns + "|" + Asm).GetHashCode();

			public string FullLabel => Format == TypeRefFormat.SerializeReferenceYaml
				? (string.IsNullOrEmpty(Ns) ? $"{Class}  [{Asm}]  (SerializeReference)" : $"{Ns}.{Class}  [{Asm}]  (SerializeReference)")
				: $"{Class}  (uNode serializedType)";

			public string YamlLiteral => Format == TypeRefFormat.SerializeReferenceYaml
				? $"class: {Class}, ns: {Ns}, asm: {Asm}"
				: $"serializedType: {Class}";

			public string ResolveFullName => Format == TypeRefFormat.SerializeReferenceYaml
				? (string.IsNullOrEmpty(Ns) ? Class : $"{Ns}.{Class}")
				: Class;
		}

		private class MissingEntry {
			public TypeKey Key;
			public HashSet<string> Files = new HashSet<string>();
			public int OccurrenceCount;
			public SerializedType serializedType = SerializedType.None;
			public bool UseManual;
			public string ManualClass = "";
			public string ManualNs = "";
			public string ManualAsm = "";
		}

		private class MissingType {
			public string typeName;
			public SerializedType serializedType = SerializedType.None;
			public bool UseManual;
			public string ManualClass = "";
			public HashSet<string> Files = new HashSet<string>();
			public int OccurrenceCount;
		}

		// Unity SerializeReference type header, e.g.:
		//   type: {class: MyBehaviourNode, ns: My.Game.AI, asm: Assembly-CSharp}
		private static readonly Regex SerializeReferenceRegex =
			new Regex(@"type:\s*\{class:\s*(?<class>[^,}]*),\s*ns:\s*(?<ns>[^,}]*),\s*asm:\s*(?<asm>[^}]*)\}",
				RegexOptions.Compiled);

		// uNode's flat serializedType field, e.g.:
		//   serializedType: MaxyGames.UNode.Nodes.BTScriptAction
		private static readonly Regex UNodeSerializedTypeRegex =
			new Regex(@"^[ \t]*serializedType:\s*(?<type>[A-Za-z_][\w\.\+`]*)\s*$",
				RegexOptions.Compiled | RegexOptions.Multiline);

		private static readonly string[] ScannedExtensions = { ".unity", ".prefab", ".asset" };

		private List<MissingEntry> _missingEntries = new List<MissingEntry>();
		private Dictionary<string, MissingType> _missingTypes = new();
		private List<UnityEngine.Object> _missingTypeReferences = new();

		private Vector2 _scroll;
		private bool _hasScanned;
		private string _statusMessage = "";

		//[MenuItem("Tools/uNode/Advanced/Missing Type Resolver")]
		public static void Open() {
			var win = GetWindow<AssetMissingTypeFixer>("Type Fixer");
			win.minSize = new Vector2(700, 440);
		}

		private void OnGUI() {
			EditorGUILayout.Space();
			EditorGUILayout.HelpBox(
				"Scans .unity / .prefab / .asset files for broken type references " +
				"(Unity [SerializeReference] headers AND uNode 'serializedType' fields) and lets you " +
				"remap them to a current type.\n\n" +
				"Requires Force Text asset serialization. BACK UP before applying.",
				MessageType.Info);

			using(new EditorGUILayout.HorizontalScope()) {
				if(GUILayout.Button("Scan Project", GUILayout.Height(28))) {
					ScanProject();
				}
			}

			if(!string.IsNullOrEmpty(_statusMessage))
				EditorGUILayout.HelpBox(_statusMessage, MessageType.None);

			if(!_hasScanned) return;

			if(_missingEntries.Count == 0 && _missingTypes.Count == 0) {
				EditorGUILayout.HelpBox("No missing/unresolvable types found.", MessageType.Info);
				return;
			}

			EditorGUILayout.LabelField($"Found {_missingEntries.Count + _missingTypes.Count} missing type(s):", EditorStyles.boldLabel);

			_scroll = EditorGUILayout.BeginScrollView(_scroll);
			foreach(var entry in _missingEntries) {
				DrawEntry(entry);
			}
			foreach(var (missingType, entry) in _missingTypes) {
				DrawEntry(missingType, entry);
			}
			EditorGUILayout.EndScrollView();

			EditorGUILayout.Space();
			bool anyResolved = _missingEntries.Any(e => e.serializedType.isAssigned || (e.UseManual && !string.IsNullOrEmpty(e.ManualClass)));
			anyResolved |= _missingTypes.Any(pair => pair.Value.UseManual ? !string.IsNullOrEmpty(pair.Value.ManualClass) : pair.Value.serializedType.isAssigned);

			using(new EditorGUI.DisabledScope(!anyResolved)) {
				if(GUILayout.Button("Apply Selected Fixes", GUILayout.Height(32))) {
					ApplyFixes();
				}
			}
		}

		private void DrawEntry(string missingType, MissingType entry) {
			using(new EditorGUILayout.VerticalScope("box")) {
				EditorGUILayout.LabelField("Missing:", missingType, EditorStyles.boldLabel);
				using(new EditorGUILayout.HorizontalScope()) {
					EditorGUILayout.LabelField($"{entry.Files.Count} file(s), {entry.OccurrenceCount} occurrence(s)");
					if(GUILayout.Button("Log files", GUILayout.Width(90))) {
						Debug.Log($"[Type Fixer] Files referencing {entry.typeName}:\n" + string.Join("\n", entry.Files));
					}
				}

				entry.UseManual = EditorGUILayout.ToggleLeft("Enter target type manually", entry.UseManual);

				if(entry.UseManual) {
					entry.ManualClass = EditorGUILayout.TextField("Full type name (Namespace.Class)", entry.ManualClass);
				}
				else {
					uNodeGUIUtility.EditType(entry.serializedType, new GUIContent("Replace with"), type => {
						entry.serializedType = type;
					}, new FilterAttribute() { UnityReference = false });
				}
			}
			EditorGUILayout.Space(4);
		}

		private void DrawEntry(MissingEntry entry) {
			using(new EditorGUILayout.VerticalScope("box")) {
				EditorGUILayout.LabelField("Missing:", entry.Key.FullLabel, EditorStyles.boldLabel);
				using(new EditorGUILayout.HorizontalScope()) {
					EditorGUILayout.LabelField($"{entry.Files.Count} file(s), {entry.OccurrenceCount} occurrence(s)");
					if(GUILayout.Button("Log files", GUILayout.Width(90))) {
						Debug.Log($"[Type Fixer] Files referencing {entry.Key.FullLabel}:\n" + string.Join("\n", entry.Files));
					}
				}

				entry.UseManual = EditorGUILayout.ToggleLeft("Enter target type manually", entry.UseManual);

				if(entry.UseManual) {
					if(entry.Key.Format == TypeRefFormat.SerializeReferenceYaml) {
						entry.ManualClass = EditorGUILayout.TextField("Class", entry.ManualClass);
						entry.ManualNs = EditorGUILayout.TextField("Namespace", entry.ManualNs);
						entry.ManualAsm = EditorGUILayout.TextField("Assembly", entry.ManualAsm);
					}
					else {
						entry.ManualClass = EditorGUILayout.TextField("Full type name (Namespace.Class)", entry.ManualClass);
					}
				}
				else {
					uNodeGUIUtility.EditType(entry.serializedType, new GUIContent("Replace with"), type => {
						entry.serializedType = type;
					}, new FilterAttribute() { UnityReference = false});
				}
			}
			EditorGUILayout.Space(4);
		}

		private void ResetScan() {
			_missingEntries.Clear();
			_missingTypeReferences.Clear();
			_missingTypes.Clear();
		}

		private void ScanProject() {
			ResetScan();
			_statusMessage = "Scanning...";

			var files = Directory.GetFiles(Application.dataPath, "*.*", SearchOption.AllDirectories)
				.Where(f => ScannedExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
				.ToList();

			var found = new Dictionary<TypeKey, MissingEntry>();
			int skippedBinary = 0;

			foreach(var absPath in files) {
				string text;
				try { text = File.ReadAllText(absPath); }
				catch { continue; }

				if(!text.StartsWith("%YAML")) {
					skippedBinary++;
					continue;
				}

				string assetPath = "Assets" + absPath.Substring(Application.dataPath.Length).Replace('\\', '/');
				bool anyMatchInFile = false;

				// --- Format 1: Unity SerializeReference ---
				foreach(Match m in SerializeReferenceRegex.Matches(text)) {
					string cls = m.Groups["class"].Value.Trim();
					string ns = m.Groups["ns"].Value.Trim();
					string asm = m.Groups["asm"].Value.Trim();

					if(string.IsNullOrEmpty(cls)) continue; // null reference, not a broken type

					var key = new TypeKey { Format = TypeRefFormat.SerializeReferenceYaml, Class = cls, Ns = ns, Asm = asm };
					if(IsTypeResolvable(key)) continue;

					RegisterMissing(found, key, assetPath);
					anyMatchInFile = true;
				}

				// --- Format 2: uNode serializedType ---
				foreach(Match m in UNodeSerializedTypeRegex.Matches(text)) {
					string fullType = m.Groups["type"].Value.Trim();
					if(string.IsNullOrEmpty(fullType) || fullType == "null") continue;

					var key = new TypeKey { Format = TypeRefFormat.UNodeSerializedType, Class = fullType, Ns = "", Asm = "" };
					if(IsTypeResolvable(key)) continue;

					RegisterMissing(found, key, assetPath);
					anyMatchInFile = true;
				}

				_ = anyMatchInFile; // just for readability, not otherwise used
			}

			{//Find missing type in graphs
				var graphAssets = GraphEditorUtility.FindAllGraphIncludingNestedGraphs().ToArray();
				string assetPath = null;
				bool CheckMissingType(SerializedType serializedType) {
					if(serializedType != null && serializedType.isMissing) {
						if(!_missingTypes.TryGetValue(serializedType.typeName, out var data)) {
							data = new();
							_missingTypes[serializedType.typeName] = data;
						}
						data.Files.Add(assetPath);
						data.OccurrenceCount++;
						return true;
					}
					return false;
				}

				foreach(var asset in graphAssets) {
					//Debug.Log("Searcing on graph: " + asset, asset);
					assetPath = AssetDatabase.GetAssetPath(asset);

					bool hasMissing = false;
					EditorReflectionUtility.AnalizeSerializedObject(asset, val => {
						if(val is SerializedType serializedType) {
							return hasMissing |= CheckMissingType(serializedType);
						}
						else if(val is MemberData member) {
							if(member.IsTargetingReflection || member.IsTargetingType) {
								hasMissing |= CheckMissingType(member.StartSerializedType);
								if(member.serializedInstance != null) {
									hasMissing |= CheckMissingType(member.serializedInstance.serializedType);
								}
								return true;
							}
						}
						return false;
					});
					if(hasMissing) {
						_missingTypeReferences.Add(asset);
					}
				}
			}

			_missingEntries = found.Values.OrderByDescending(e => e.OccurrenceCount).ToList();
			_hasScanned = true;
			_statusMessage = $"Scan complete. Checked {files.Count} file(s), skipped {skippedBinary} non-text (binary) file(s).";
		}

		private static void RegisterMissing(Dictionary<TypeKey, MissingEntry> found, TypeKey key, string assetPath) {
			if(!found.TryGetValue(key, out var entry)) {
				entry = new MissingEntry { Key = key };
				found[key] = entry;
			}
			entry.Files.Add(assetPath);
			entry.OccurrenceCount++;
		}

		private static bool IsTypeResolvable(TypeKey key) {
			string fullName = key.ResolveFullName;

			if(key.Format == TypeRefFormat.SerializeReferenceYaml && !string.IsNullOrEmpty(key.Asm)) {
				try {
					var t = Type.GetType($"{fullName}, {key.Asm}");
					if(t != null) return true;
				}
				catch { /* malformed assembly-qualified name, fall through */ }
			}

			// Assembly-agnostic fallback (also the only path for uNode's format, which has no asm info).
			foreach(var asm in EditorReflectionUtility.GetAssemblies()) {
				if(key.Format == TypeRefFormat.SerializeReferenceYaml && !string.IsNullOrEmpty(key.Asm) && asm.GetName().Name != key.Asm) {
					continue;
				}
				try {
					var t = asm.GetType(fullName, throwOnError: false);
					if(t != null) return true;
				}
				catch { /* ignore */ }
			}
			return false;
		}

		private void ApplyFixes() {
			var replacements = new List<(TypeKey oldKey, TypeKey newKey, HashSet<string> files)>();
			var replacements2 = new List<(string missingType, MissingType data)>();

			foreach(var entry in _missingEntries) {
				TypeKey newKey;
				if(entry.UseManual) {
					if(string.IsNullOrEmpty(entry.ManualClass)) continue;

					if(entry.Key.Format == TypeRefFormat.SerializeReferenceYaml) {
						newKey = new TypeKey {
							Format = TypeRefFormat.SerializeReferenceYaml,
							Class = entry.ManualClass,
							Ns = entry.ManualNs,
							Asm = entry.ManualAsm
						};
					}
					else {
						newKey = new TypeKey {
							Format = TypeRefFormat.UNodeSerializedType,
							Class = entry.ManualClass, // full dotted name
							Ns = "",
							Asm = ""
						};
					}
				}
				else {
					if(entry.serializedType.isAssigned == false) continue;
					var t = entry.serializedType.type;

					newKey = entry.Key.Format == TypeRefFormat.SerializeReferenceYaml
						? new TypeKey { Format = TypeRefFormat.SerializeReferenceYaml, Class = t.Name, Ns = t.Namespace ?? "", Asm = t.Assembly.GetName().Name }
						: new TypeKey { Format = TypeRefFormat.UNodeSerializedType, Class = TypeSerializer.Serialize(t), Ns = "", Asm = "" };
				}
				replacements.Add((entry.Key, newKey, entry.Files));
			}

			foreach(var pair in _missingTypes) {
				if(pair.Value.UseManual && !string.IsNullOrEmpty(pair.Value.ManualClass) || pair.Value.serializedType.isAssigned) {
					replacements2.Add((pair.Key, pair.Value));
				}
			}

			if(replacements.Count == 0 && replacements2.Count == 0) {
				_statusMessage = "Nothing to apply - select a replacement type for at least one entry.";
				return;
			}

			var replacementCounts = replacements.Count + replacements2.Count;
			var listFiles = replacements.SelectMany(r => r.files).Concat(replacements2.SelectMany(r => r.data.Files)).Distinct().ToArray();
			int fileCount = listFiles.Length;
			if(!EditorUtility.DisplayDialog(
					"Apply type fixes?",
					$"This will rewrite {replacementCounts} type reference(s) across {fileCount} file(s) on disk.\n\n" +
					"Make sure your project is committed to version control first.",
					"Apply", "Cancel")) {
				return;
			}

			var allFiles = replacements.SelectMany(r => r.files).Distinct().ToList();
			string assetsRoot = Application.dataPath.Substring(0, Application.dataPath.Length - "Assets".Length);
			int filesChanged = 0;
			int totalReplacements = 0;

			AssetDatabase.StartAssetEditing();
			try {
				foreach(var relPath in allFiles) {
					string absPath = Path.Combine(assetsRoot, relPath);

					string text;
					try { text = File.ReadAllText(absPath); }
					catch { continue; }

					string original = text;

					foreach(var (oldKey, newKey, files) in replacements) {
						if(!files.Contains(relPath)) continue;
						string oldLiteral = oldKey.YamlLiteral;
						string newLiteral = newKey.YamlLiteral;
						int count = Regex.Matches(text, Regex.Escape(oldLiteral)).Count;
						if(count == 0) continue;
						text = text.Replace(oldLiteral, newLiteral);
						totalReplacements += count;
					}

					if(text != original) {
						File.WriteAllText(absPath, text);
						filesChanged++;
					}
				}
			}
			finally {
				AssetDatabase.StopAssetEditing();
				AssetDatabase.Refresh();
			}

			{ //For graphs
				var graphAssets = _missingTypeReferences;
				string assetPath = null;
				bool CheckAndReplaceMissingType(SerializedType serializedType) {
					if(serializedType != null && serializedType.isMissing) {
						if(_missingTypes.TryGetValue(serializedType.typeName, out var data)) {
							if(data.UseManual) {
								var t = data.ManualClass.ToType(false);
								if(t != null) {
									serializedType.type = t;
								}
							}
							else {
								serializedType.CopyFrom(data.serializedType);
							}
							totalReplacements++;
							return true;
						}
					}
					return false;
				}

				foreach(var asset in graphAssets) {
					assetPath = AssetDatabase.GetAssetPath(asset);

					bool hasMissing = false;
					EditorReflectionUtility.AnalizeSerializedObject(asset, val => {
						if(val is SerializedType serializedType) {
							return hasMissing |= CheckAndReplaceMissingType(serializedType);
						}
						else if(val is MemberData member) {
							if(member.IsTargetingReflection || member.IsTargetingType) {
								hasMissing |= CheckAndReplaceMissingType(member.StartSerializedType);
								if(member.serializedInstance != null) {
									hasMissing |= CheckAndReplaceMissingType(member.serializedInstance.serializedType);
								}
								return true;
							}
						}
						return false;
					});
					if(hasMissing) {
						filesChanged++;
						EditorUtility.SetDirty(asset);
					}
				}
			}

			_statusMessage = $"Applied {totalReplacements} replacement(s) across {filesChanged} file(s). Re-scan to verify.";
			_hasScanned = false;
			_missingEntries.Clear();
		}
	}
}
#pragma warning disable CS0618
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Xml;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
using UnityEditor.IMGUI.Controls;

namespace MaxyGames.UNode.Editors {
	public interface IRelevanceItem {
		public float Score { get; set; }
	}

	public partial class ItemSelector {
		#region Search
		abstract class TreeSearcher {
			public virtual float IsMatchSearch(TreeViewItem tree, string[] splittedStrings, SearchKind searchKind, SearchFilter searchFilter) {
				if(tree is MemberTreeView memberTree && !(memberTree.member is Type)) {
					return IsMatchSearch(memberTree.member, memberTree.displayName, splittedStrings, searchKind, searchFilter);
				}
				bool flag = true;
				switch(searchFilter) {
					case SearchFilter.Function: {
						if(tree is SelectorCustomTreeView item) {
							flag = item.item.IsValidSearchFilter(SearchFilter.Function);
						}
						break;
					}
					case SearchFilter.Property: {
						if(tree is SelectorCustomTreeView item) {
							flag = item.item.IsValidSearchFilter(SearchFilter.Property);
						}
						break;
					}
					case SearchFilter.Type: {
						if(tree is MemberTreeView) {
							var item = tree as MemberTreeView;
							flag = item.member is Type;
						}
						else if(tree is SelectorCustomTreeView item) {
							flag = item.item.IsValidSearchFilter(SearchFilter.Type);
						}
						break;
					}
					case SearchFilter.Variable: {
						if(tree is SelectorCustomTreeView item) {
							flag = item.item.IsValidSearchFilter(SearchFilter.Variable);
						}
						break;
					}
				}
				if(!flag) {
					return -1;
				}
				var str = tree.displayName;
				float score = -1;
				for(int i = 0; i < splittedStrings.Length; i++) {
					var s = ScoreSearch(str, splittedStrings[i], searchKind);
					if(s < 0) {
						return s;
					}
					score = MathF.Max(score, s) + (i * 0.1f);
				}
				return score;
			}

			public virtual float IsMatchSearch(MemberInfo member, string typeName, string[] splittedStrings, SearchKind searchKind, SearchFilter searchFilter) {
				bool flag = true;
				switch(searchFilter) {
					case SearchFilter.Function:
						flag = member is MethodInfo;
						break;
					case SearchFilter.Property:
						flag = member is PropertyInfo;
						break;
					case SearchFilter.Type:
						flag = member is Type;
						break;
					case SearchFilter.Variable:
						flag = member is FieldInfo;
						break;
				}
				if(!flag) {
					return -1;
				}
				var str = member.Name;
				if(member is Type) {
					//if(splittedStrings.Length > 1) {
					//	return IsMatchSearch(str, splittedStrings[0], searchKind);
					//}
				}
				else {
					str = member.Name;
					if(splittedStrings.Length > 1) {
						var score = ScoreSearch(typeName, splittedStrings[0], searchKind);
						if(score < 0) {
							for(int i = 0; i < splittedStrings.Length; i++) {
								if(i == 0 && member is ConstructorInfo) {
									str = member.DeclaringType.Name;
									var sc = ScoreSearch("new", splittedStrings[i], SearchKind.Startwith);
									if(sc >= 0) {
										score = MathF.Max(score, sc);
										continue;
									}
								}
								var s = ScoreSearch(str, splittedStrings[i], searchKind);
								if(s < 0) {
									return s;
								}
								score = MathF.Max(score, s) + (i * 0.1f);
							}
							return score;
						}
						for(int i = 1; i < splittedStrings.Length; i++) {
							var s = ScoreSearch(str, splittedStrings[i], searchKind);
							if(s < 0) {
								return s;
							}
							score = MathF.Max(score, s) + (i * 0.1f);
						}
						return score;
					}
				}
				{
					float score = -1;
					for(int i = 0; i < splittedStrings.Length; i++) {
						var s = ScoreSearch(str, splittedStrings[i], searchKind);
						if(s < 0) {
							return s;
						}
						score = MathF.Max(score, s) + (i * 0.1f);
					}
					return score;
				}
			}

			public virtual float ScoreSearch(string str, string searchString, SearchKind searchKind) {
				if(string.IsNullOrEmpty(searchString))
					return 0;
				switch(searchKind) {
					case SearchKind.Relevant:
						return Relevance(searchString, str);
					case SearchKind.Contains: {
						if(str.IndexOf(searchString, StringComparison.OrdinalIgnoreCase) >= 0) {
							static float Score(string target, string query) {
								if(string.IsNullOrEmpty(target) || string.IsNullOrEmpty(query))
									return 0f;

								target = target.ToLowerInvariant();
								query = query.ToLowerInvariant();

								int tLen = target.Length;
								int qLen = query.Length;

								int match = 0;
								int ti = 0;

								// subsequence match
								for(int qi = 0; qi < qLen; qi++) {
									char qc = query[qi];
									while(ti < tLen) {
										if(target[ti++] == qc) {
											match++;
											break;
										}
									}
								}

								// Base ratio
								float ratio = (float)match / Math.Max(tLen, qLen);

								// Prefix bonus (important for member search)
								if(target.StartsWith(query))
									ratio += 0.3f;

								// Contains bonus
								if(target.Contains(query))
									ratio += 0.2f;

								// Clamp
								return Math.Min(ratio, 1f);
							}
							return Score(str, searchString);
						}
						break;
					}
					case SearchKind.Endwith: {
						if(str.EndsWith(searchString, StringComparison.OrdinalIgnoreCase)) {
							if(searchString.Length > str.Length) return 0;
							return (float)searchString.Length / (float)str.Length;
						}
						break;
					}
					case SearchKind.Startwith: {
						if(str.StartsWith(searchString, StringComparison.OrdinalIgnoreCase)) {
							if(searchString.Length > str.Length) return 0;
							return (float)searchString.Length / (float)str.Length;
						}
						break;
					}
					case SearchKind.Equal:
						if(str.Equals(searchString, StringComparison.OrdinalIgnoreCase)) {
							return 1;
						}
						break;
				}
				return -1;
			}

			public static bool IsMatch(string str, string searchString, SearchKind searchKind) {
				if(string.IsNullOrEmpty(searchString))
					return true;
				switch(searchKind) {
					case SearchKind.Contains:
						return str.IndexOf(searchString, StringComparison.OrdinalIgnoreCase) >= 0;
					case SearchKind.Endwith:
						return str.EndsWith(searchString, StringComparison.OrdinalIgnoreCase);
					case SearchKind.Startwith:
						return str.StartsWith(searchString, StringComparison.OrdinalIgnoreCase);
					case SearchKind.Equal:
						return str.Equals(searchString, StringComparison.OrdinalIgnoreCase);
				}
				return false;
			}


			private static bool Compare(char a, char b) {
				return char.ToLowerInvariant(a) == char.ToLowerInvariant(b);
			}

			public static bool IsWordDelimiter(char c) {
				return char.IsWhiteSpace(c) || char.IsSymbol(c) || char.IsPunctuation(c);
			}

			public static bool IsDelimiterAllowed(char c) {
				switch(c) {
					case '-':
					case '+':
					case '*':
					case '/':
					case '%':
					case '^':
					case '!':
					case '?':
					case '=':
						return true;
				}
				return false;
			}

			public static bool IsWordBeginning(char? previous, char current, char? next) {
				var isFirst = previous == null;
				var isLast = next == null;

				var isLetter = char.IsLetter(current);
				var wasLetter = previous != null && char.IsLetter(previous.Value);

				var isNumber = char.IsNumber(current);
				var wasNumber = previous != null && char.IsNumber(previous.Value);

				var isUpper = char.IsUpper(current);
				var wasUpper = previous != null && char.IsUpper(previous.Value);

				var isDelimiter = IsWordDelimiter(current);
				var wasDelimiter = previous != null && IsWordDelimiter(previous.Value);

				var willBeLower = next != null && char.IsLower(next.Value);

				return
					(!isDelimiter && isFirst) ||
					(!isDelimiter && wasDelimiter) ||
					(isLetter && wasLetter && isUpper && !wasUpper) || // camelCase => camel_Case
					(isLetter && wasLetter && isUpper && wasUpper && !isLast && willBeLower) || // => ABBRWord => ABBR_Word
					(isNumber && wasLetter) || // Vector3 => Vector_3
					(isLetter && wasNumber && isUpper && willBeLower); // Word1Word => Word_1_Word, Word1word => Word_1word
			}


			public static bool IsWordBeginning(string s, int index) {
				var previous = index > 0 ? s[index - 1] : (char?)null;
				var current = s[index];
				var next = index < s.Length - 1 ? s[index + 1] : (char?)null;

				return IsWordBeginning(previous, current, next);
			}


			private static bool Match(string query, string haystack, bool strictOnly, float strictWeight, float looseWeight, out float score, bool[] indices) {
				score = 0;
				var isStreaking = false;
				var haystackIndex = 0;
				var matchedAny = false;

				for(var queryIndex = 0; queryIndex < query.Length; queryIndex++) {
					var queryCharacter = query[queryIndex];

					var matched = false;

					if(IsWordDelimiter(queryCharacter)) {
						if(IsDelimiterAllowed(queryCharacter)) {

							for(; haystackIndex < haystack.Length; haystackIndex++) {
								var haystackCharacter = haystack[haystackIndex];

								var matchesLoose = Compare(queryCharacter, haystackCharacter);
								var matchesStrict = matchesLoose && (isStreaking || IsWordBeginning(haystack, haystackIndex));

								var matches = strictOnly ? matchesStrict : matchesLoose;

								if(matches) {
									score += matchesStrict ? strictWeight : looseWeight;
									matched = true;

									if(indices != null) {
										indices[haystackIndex] = true;
									}

									isStreaking = true;
									haystackIndex++;
									break;
								}
								else {
									isStreaking = false;
								}
							}

							if(matched) {
								matchedAny = true;
							}
							else {
								return false;
							}
						}
						continue;
					}


					for(; haystackIndex < haystack.Length; haystackIndex++) {
						var haystackCharacter = haystack[haystackIndex];

						if(IsWordDelimiter(haystackCharacter)) {
							isStreaking = false;
							continue;
						}

						var matchesLoose = Compare(queryCharacter, haystackCharacter);
						var matchesStrict = matchesLoose && (isStreaking || IsWordBeginning(haystack, haystackIndex));

						var matches = strictOnly ? matchesStrict : matchesLoose;

						if(matches) {
							score += matchesStrict ? strictWeight : looseWeight;
							matched = true;

							if(indices != null) {
								indices[haystackIndex] = true;
							}

							isStreaking = true;
							haystackIndex++;
							break;
						}
						else {
							isStreaking = false;
						}
					}

					if(matched) {
						matchedAny = true;
					}
					else {
						return false;
					}
				}

				if(!matchedAny) {
					return false;
				}

				score /= query.Length;
				return true;
			}

			private static float? Score(string query, string haystack, bool strictOnly, float strictWeight, float looseWeight) {
				if(Match(query, haystack, strictOnly, strictWeight, looseWeight, out var score, null)) {
					return score;
				}
				else {
					return null;
				}
			}

			private static bool[] Indices(string query, string haystack, bool strictOnly) {
				bool[] indices = new bool[haystack.Length];

				if(Match(query, haystack, strictOnly, 0, 0, out var score, indices)) {
					return indices;
				}
				else {
					return null;
				}
			}

			public static float Score(string query, string haystack, SearchKind searchKind) {
				switch(searchKind) {
					case SearchKind.Relevant:
						return Relevance(query, haystack);
					case SearchKind.Contains: {

						break;
					}
				}
				return -1;
			}

			public static float Relevance(string query, string haystack) {
				// Configurable score weights
				var strictWeight = 1;
				var looseWeight = 0.25f;
				var shortnessWeight = 1;

				// Calculate the strict score first, then the loose score if the former fails
				var score = Score(query, haystack, true, strictWeight, looseWeight) ?? Score(query, haystack, false, strictWeight, looseWeight);

				if(score == null) {
					// If loose matching fails as well, return a failure
					return -1;
				}

				// Calculate the shortness factor
				var shortness = (float)query.Length / haystack.Length;

				// Sum and normalize scores
				var scoreWeight = Mathf.Max(strictWeight, looseWeight);
				var maxWeight = scoreWeight + shortnessWeight;
				var totalWeight = (score.Value * scoreWeight) + (shortness * shortnessWeight);
				return totalWeight / maxWeight;
			}

			public static List<(int start, int end)> HighlightQueryPositions(string haystack, string query) {
				if(string.IsNullOrEmpty(query)) {
					return new List<(int, int)>();
				}

				bool[] matchingIndices = Indices(query, haystack, true) ?? Indices(query, haystack, false);
				if(matchingIndices == null) {
					return new List<(int, int)>();
				}

				var positions = new List<(int start, int end)>();
				bool isHighlighted = false;
				int matchStart = -1;

				for(var i = 0; i < haystack.Length; i++) {
					if(matchingIndices[i]) {
						if(!isHighlighted) {
							// Start a new match
							matchStart = i;
							isHighlighted = true;
						}
					}
					else {
						if(isHighlighted) {
							// End the current match
							positions.Add((matchStart, i));
							isHighlighted = false;
						}
					}
				}
				return positions;
			}

			public bool Hightlight(TreeViewItem tree, string searchString, SearchKind searchKind, ref TreeHightlight hightlight) {
				var splittedStrings = searchString.Split(new[] { '.', ' ' });
				if(splittedStrings.Length > 0 && splittedStrings[0].EndsWith("[]", StringComparison.Ordinal)) {
					searchString = searchString.Remove(splittedStrings[0].Length - 2, 2);
				}
				var param = new SearchParam(searchString, searchKind, SearchFilter.All);
				return Hightlight(tree, param, ref hightlight);
			}

			protected virtual bool Hightlight(TreeViewItem tree, SearchParam searchParam, ref TreeHightlight hightlight) {
				var str = tree.displayName;
				if(tree is MemberTreeView memberTree) {
					var member = memberTree.member;
					if(member is Type) {
						if(searchParam.splittedStrings.Length > 1) {
							return Hightlight(str, searchParam.splittedStrings[0], searchParam.searchKind, ref hightlight);
						}
					}
				}
				for(int i = 0; i < searchParam.splittedStrings.Length; i++) {
					Hightlight(str, searchParam.splittedStrings[i], searchParam.searchKind, ref hightlight);
				}
				return hightlight.hightlight.Count > 0;
			}

			protected bool Hightlight(string str, string searchString, SearchKind searchKind, ref TreeHightlight hightlight) {
				int first;
				int last;
				switch(searchKind) {
					case SearchKind.Relevant:
						var positions = HighlightQueryPositions(str, searchString);
						if(positions.Count == 0) return false;
						foreach(var (start, end) in positions) {
							hightlight.hightlight.Add(new KeyValuePair<int, int>(start, end));
						}
						return true;
					case SearchKind.Contains:
						first = str.IndexOf(searchString, StringComparison.OrdinalIgnoreCase);
						last = searchString.Length;
						if(first >= 0) {
							hightlight.hightlight.Add(new KeyValuePair<int, int>(first, last));
							return true;
						}
						return false;
					case SearchKind.Endwith:
						first = str.LastIndexOf(searchString, StringComparison.OrdinalIgnoreCase);
						last = searchString.Length;
						if(first >= 0) {
							hightlight.hightlight.Add(new KeyValuePair<int, int>(first, last));
							return true;
						}
						return false;
					case SearchKind.Startwith:
						if(IsMatch(str, searchString, searchKind)) {
							first = 0;
							last = searchString.Length;
							hightlight.hightlight.Add(new KeyValuePair<int, int>(first, last));
							return true;
						}
						break;
					case SearchKind.Equal:
						if(IsMatch(str, searchString, searchKind)) {
							first = 0;
							last = searchString.Length;
							hightlight.hightlight.Add(new KeyValuePair<int, int>(first, last));
							return true;
						}
						break;
				}
				return false;
			}
		}

		class TreeHightlight {
			public List<KeyValuePair<int, int>> hightlight = new List<KeyValuePair<int, int>>();
		}

		class DefaultTreeSearcher : TreeSearcher { }

		class CapitalTreeSearcher : TreeSearcher {
			List<string>[] splittedCapital;

			public CapitalTreeSearcher(string searchString) {
				var splittedStrings = searchString.Split(new[] { '.', ' ' });
				if(splittedStrings.Length > 0) {
					splittedCapital = new List<string>[splittedStrings.Length];
					for(int i = 0; i < splittedCapital.Length; i++) {
						splittedCapital[i] = GetSplittedUpperCase(splittedStrings[i]);
					}
				}
			}

			List<string> GetSplittedUpperCase(string str) {
				List<string> strs = new List<string>();
				string value = string.Empty;
				for(int i = 0; i < str.Length; i++) {
					if(char.IsUpper(str[i])) {
						if(!string.IsNullOrEmpty(value)) {
							strs.Add(value);
							value = string.Empty;
						}
					}
					value += str[i];
				}
				if(!string.IsNullOrEmpty(value)) {
					strs.Add(value);
				}
				return strs;
			}

			public override float IsMatchSearch(MemberInfo member, string typeName, string[] splittedStrings, SearchKind searchKind, SearchFilter searchFilter) {
				bool flag = true;
				switch(searchFilter) {
					case SearchFilter.Function:
						flag = member is MethodInfo;
						break;
					case SearchFilter.Property:
						flag = member is PropertyInfo;
						break;
					case SearchFilter.Type:
						flag = member is Type;
						break;
					case SearchFilter.Variable:
						flag = member is FieldInfo;
						break;
				}
				if(!flag) {
					return -1;
				}
				var str = member.Name;
				if(member is Type) {
				}
				else {
					str = member.Name;
					if(splittedStrings.Length > 1) {
						List<string> split;
						if(member is ConstructorInfo) {
							split = GetSplittedUpperCase(member.DeclaringType.Name);
						}
						else {
							split = GetSplittedUpperCase(str);
						}
						if(!IsMatchSearchCapital(GetSplittedUpperCase(typeName), splittedCapital[0])) {
							for(int i = 0; i < splittedCapital.Length; i++) {
								if(i == 0 && member is ConstructorInfo) {
									if(IsMatch("new", splittedStrings[i], SearchKind.Startwith)) {
										continue;
									}
								}
								if(!IsMatchSearchCapital(split, splittedCapital[i])) {
									return -1;
								}
							}
							return 1;
						}
						for(int i = 1; i < splittedCapital.Length; i++) {
							if(!IsMatchSearchCapital(split, splittedCapital[i])) {
								return -1;
							}
						}
						return 1;
					}
				}
				return IsMatchSearchCapital(str) ? 1 : 0;
			}

			protected override bool Hightlight(TreeViewItem tree, SearchParam searchParam, ref TreeHightlight hightlight) {
				var str = tree.displayName;
				HightlightCapital(str, ref hightlight);
				return hightlight.hightlight.Count > 0;
			}

			private bool HightlightCapital(string str, ref TreeHightlight hightlight) {
				var split = GetSplittedUpperCase(str);
				bool flag = false;
				for(int i = 0; i < splittedCapital.Length; i++) {
					if(HightlightCapital(split, splittedCapital[i], ref hightlight)) {
						flag = true;
					}
				}
				return flag;
			}

			private bool HightlightCapital(List<string> split, List<string> split2, ref TreeHightlight hightlight) {
				if(split.Count < split2.Count) {
					return false;
				}
				bool flag = false;
				int index = 0;
				for(int x = 0; x < split2.Count; x++) {
					if(Hightlight(split[x], split2[x], SearchKind.Startwith, ref hightlight)) {
						flag = true;
						var pair = hightlight.hightlight[hightlight.hightlight.Count - 1];
						pair = new KeyValuePair<int, int>(pair.Key + index, pair.Value);
						hightlight.hightlight[hightlight.hightlight.Count - 1] = pair;
					}
					index += split[x].Length;
				}
				return flag;
			}

			private bool IsMatchSearchCapital(string str) {
				var split = GetSplittedUpperCase(str);
				for(int i = 0; i < splittedCapital.Length; i++) {
					if(!IsMatchSearchCapital(split, splittedCapital[i])) {
						return false;
					}
				}
				return true;
			}

			private bool IsMatchSearchCapital(List<string> split, List<string> split2) {
				if(split.Count < split2.Count) {
					return false;
				}
				for(int x = 0; x < split2.Count; x++) {
					if(IsMatch(split[x], split2[x], SearchKind.Startwith)) {
						return false;
					}
				}
				return true;
			}

			public override float IsMatchSearch(TreeViewItem tree, string[] splittedStrings, SearchKind searchKind, SearchFilter searchFilter) {
				if(tree is MemberTreeView memberTree && !(memberTree.member is Type)) {
					return IsMatchSearch(memberTree.member, memberTree.displayName, splittedStrings, searchKind, searchFilter);
				}
				bool flag = true;
				switch(searchFilter) {
					case SearchFilter.Function: {
						if(tree is SelectorCustomTreeView item) {
							flag = item.item.IsValidSearchFilter(SearchFilter.Function);
						}
						break;
					}
					case SearchFilter.Property: {
						if(tree is SelectorCustomTreeView item) {
							flag = item.item.IsValidSearchFilter(SearchFilter.Property);
						}
						break;
					}
					case SearchFilter.Type: {
						if(tree is MemberTreeView) {
							var item = tree as MemberTreeView;
							flag = item.member is Type;
						}
						else if(tree is SelectorCustomTreeView item) {
							flag = item.item.IsValidSearchFilter(SearchFilter.Type);
						}
						break;
					}
					case SearchFilter.Variable: {
						if(tree is SelectorCustomTreeView item) {
							flag = item.item.IsValidSearchFilter(SearchFilter.Variable);
						}
						break;
					}
				}
				if(!flag) {
					return -1;
				}
				var str = tree.displayName;
				{
					var split = GetSplittedUpperCase(str);
					for(int i = 0; i < splittedCapital.Length; i++) {
						if(split.Count < splittedCapital[i].Count) {
							return -1;
						}
						for(int x = 0; x < splittedCapital[i].Count; x++) {
							if(!IsMatch(split[x], splittedCapital[i][x], SearchKind.Startwith)) {
								return -1;
							}
						}
					}
				}
				return 1;
			}

			public override float ScoreSearch(string str, string searchString, SearchKind searchKind) {
				if(string.IsNullOrEmpty(searchString))
					return 0;
				if(str.StartsWith(searchString, StringComparison.OrdinalIgnoreCase)) {
					return (float)searchString.Length / str.Length;
				}
				return -1;
			}
		}

		readonly struct SearchParam {
			public readonly string searchString;
			public readonly string[] splittedStrings;
			public readonly bool isUsingDot;

			public readonly SearchKind searchKind;
			public readonly SearchFilter searchFilter;

			public SearchParam(string searchString, SearchKind searchKind, SearchFilter searchFilter) {
				this.searchString = searchString;
				this.searchKind = searchKind;
				this.searchFilter = searchFilter;
				splittedStrings = searchString.Split(new[] { '.', ' ' });
				isUsingDot = searchString.Contains('.');
			}
		}
		#endregion

		#region Sort
		//class TreeSorting {
		//	public virtual void Execute(List<TreeViewItem> treeViews) {

		//	}

		//	public void DoSortRecursive(List<TreeViewItem> treeViews) {
		//		DoSort(treeViews);
		//		for(int i=0;i<treeViews.Count;i++) {
		//			if(treeViews[i].hasChildren) {
		//				DoSortRecursive(treeViews[i].children);
		//			}
		//		}
		//	}

		//	protected virtual void DoSort(List<TreeViewItem> treeViews) {

		//	}
		//}
		#endregion

		#region Filter
		class TreeFilter {
			public virtual List<TreeViewItem> DoFilter(List<TreeViewItem> treeViews) {
				return treeViews;
			}
		}

		class TreeArrayFilter : TreeFilter {
			public override List<TreeViewItem> DoFilter(List<TreeViewItem> treeViews) {
				for(int i = 0; i < treeViews.Count; i++) {
					if((treeViews[i] = DoFilter(treeViews[i])) == null) {
						treeViews.RemoveAt(i);
						i--;
					}
				}
				return treeViews;
			}

			private TreeViewItem DoFilter(TreeViewItem tree) {
				if(tree is MemberTreeView) {
					var item = tree as TypeTreeView;
					if(item != null) {
						if(!item.type.IsArray && !(item.type is RuntimeType)) {
							try {
								if(item.type != typeof(void) && item.type != typeof(TypedReference) && !item.type.IsGenericTypeDefinition) {
									tree = new TypeTreeView(ReflectionUtils.MakeArrayType(item.type)) {
										filter = item.filter,
										nextValidation = item.nextValidation,
										selectValidation = item.selectValidation
									};
								}
								else {
									return null;
								}
							}
							catch {
								return null;
							}
						}
						else {
							return null;
						}
					}
					else {
						return tree;
					}
				}
				else if(tree is SelectorCategoryTreeView) {
					var item = tree as SelectorCategoryTreeView;
					item.expanded = true;
					if(tree.hasChildren) {
						var childs = tree.children;
						for(int i = 0; i < childs.Count; i++) {
							if(childs[i] is MemberTreeView && !(childs[i] is TypeTreeView)) {
								childs.RemoveAt(i);
								i--;
							}
						}
					}
					if(!tree.hasChildren)
						return null;
				}
				else {
					return null;
				}
				if(tree.hasChildren) {
					var childs = tree.children;
					for(int i = 0; i < childs.Count; i++) {
						if((childs[i] = DoFilter(childs[i])) == null) {
							childs.RemoveAt(i);
							i--;
						}
					}
					if(childs.Count == 0 && !(tree is MemberTreeView))
						return null;
				}
				return tree;
			}
		}
		#endregion

		class TreeSearchManager {
			public bool deepSearch = true;

			Thread searchThread;

			//Prevent freeze/unsync data.
			object lockSearch = new object();
			public List<SearchProgress> progresses;
			public Manager manager;

			public bool Hightlight(TreeViewItem tree, string searchString, SearchKind searchKind, SearchFilter searchFilter, ref TreeHightlight hightlight) {
				return GetSearcher(searchString, searchKind, searchFilter).Hightlight(tree, searchString, searchKind, ref hightlight);
			}

			public void SearchInBackground(List<TreeViewItem> treeViews, string searchString, SearchKind searchKind, SearchFilter searchFilter, Action<List<TreeViewItem>> onFinished) {
				Terminate();
				if(searchThread == null && !string.IsNullOrEmpty(searchString)) {
					searchThread = new Thread(new ParameterizedThreadStart(DoSearchMember));
					searchThread.Name = "SearchThread";
					searchThread.Priority = System.Threading.ThreadPriority.Highest;
					//searchThread.IsBackground = true;
					searchThread.Start(new object[] { treeViews, searchString, searchKind, searchFilter, onFinished });
				}
			}

			public List<TreeViewItem> Search(List<TreeViewItem> treeViews, string searchString, SearchKind searchKind, SearchFilter searchFilter) {
				var result = new List<TreeViewItem>();
				try {
					GetTreeFilter(ref searchString).DoFilter(treeViews);
					var searcher = GetSearcher(searchString, searchKind, searchFilter);
					var progresses = new List<SearchProgress>();
					for(int i = 0; i < treeViews.Count; i++) {
						if(IsValidSearch(treeViews[i], new SearchParam(searchString, searchKind, searchFilter), progresses, searcher)) {
							result.Add(treeViews[i]);
						}
					}
				}
				catch(Exception ex) {
					if(ex.GetType() == typeof(Exception)) {
						throw;
					}
				}
				return result;
			}

			TreeSearcher GetSearcher(string searchString, SearchKind searchKind, SearchFilter searchFilter) {
				TreeSearcher searcher = null;
				for(int i = 0; i < searchString.Length; i++) {
					if(char.IsUpper(searchString[i])) {
						searcher = new CapitalTreeSearcher(searchString);
						break;
					}
				}
				return searcher ?? new DefaultTreeSearcher();
			}

			TreeFilter GetTreeFilter(ref string searchString) {
				var splittedStrings = searchString.Split(new[] { '.', ' ' });
				if(splittedStrings.Length > 0 && splittedStrings[0].EndsWith("[]", StringComparison.Ordinal)) {
					searchString = searchString.Remove(splittedStrings[0].Length - 2, 2);
					return new TreeArrayFilter();
				}
				return new TreeFilter();
			}

			void DoSearchMember(object obj) {
				try {
					var progresses = new List<SearchProgress>();
					lock(lockSearch) {
						this.progresses = progresses;
					}
					var objs = obj as object[];
					var treeViews = objs[0] as List<TreeViewItem>;
					var searchString = objs[1] as string;
					var searchKind = (SearchKind)objs[2];
					var searchFilter = (SearchFilter)objs[3];
					var onFinished = objs[4] as Action<List<TreeViewItem>>;
					var progress = new SearchProgress();
					var result = new List<TreeViewItem>();
					progresses.Add(progress);
					GetTreeFilter(ref searchString).DoFilter(treeViews);
					var searcher = GetSearcher(searchString, searchKind, searchFilter);
					for(int i = 0; i < treeViews.Count; i++) {
						progress.progress = (float)(i + 1) / (float)treeViews.Count;
						progress.info = "Searching: " + i + "-" + treeViews.Count;
						if(IsValidSearch(treeViews[i], new SearchParam(searchString, searchKind, searchFilter), progresses, searcher)) {
							result.Add(treeViews[i]);
						}
					}
					onFinished?.Invoke(result);
				}
				catch(InvalidOperationException ex) {
					Debug.LogException(ex);
				}
				catch(ThreadAbortException) {

				}
			}

			bool IsValidSearch(TreeViewItem tree, SearchParam searchParam, List<SearchProgress> progresses, TreeSearcher searcher, int depth = 1) {
				if(tree is TypeTreeView && deepSearch) {
					var item = tree as TypeTreeView;
					if(!item.type.IsEnum && searchParam.searchString.Length >= MinWordForDeepTypeSearch) {
						if(searchParam.splittedStrings.Length < 2 || !string.IsNullOrEmpty(searchParam.splittedStrings[1])) {
							float score = searcher.ScoreSearch(item.displayName, searchParam.splittedStrings[0], searchParam.searchKind);
							if(score >= 0 || !searchParam.isUsingDot) {
								item.Search((member) => {
									return searcher.IsMatchSearch(member, item.displayName, searchParam.splittedStrings, searchParam.searchKind, searchParam.searchFilter);
								});
								if(score >= 0.5f) {
									score -= 0.2f;
								}
								item.Score = score;
								if(tree.hasChildren && tree.children.Count > 0) {
									return true;
								}
								return score >= 0;
							}
						}
					}
				}
				else if(tree is SelectorCategoryTreeView) {
					var item = tree as SelectorCategoryTreeView;
					item.expanded = true;
					if(item.hideOnSearch) {
						return false;
					}
				}
				else if(tree is SelectorSearchTreeView) {
					var item = tree as SelectorSearchTreeView;
					if(manager.IsExpanded(item.id)) {
						item.children = item.treeViews((currProgress) => {
							if(progresses != null) {
								SearchProgress progress = null;
								if(depth < progresses.Count) {
									progress = progresses[depth];
								}
								else {
									while(depth + 1 >= progresses.Count) {
										progress = new SearchProgress();
										progresses.Add(progress);
									}
								}
								if(progress != null) {
									progress.progress = currProgress.progress;
									progress.info = currProgress.info;
								}
							}
						});
						var filter = manager.filter;
						foreach(var child in item.children) {
							AssignFilter(child, filter);
						}
					}
				}
				if(tree.hasChildren) {
					var childs = tree.children;
					SearchProgress progress = null;
					if(progresses != null) {
						if(depth < progresses.Count) {
							progress = progresses[depth];
						}
						else {
							while(depth + 1 >= progresses.Count) {
								progress = new SearchProgress();
								progresses.Add(progress);
							}
						}
					}
					int length = childs.Count;
					int count = 0;
					for(int i = 0; i < childs.Count; i++) {
						if(progress != null) {
							progress.progress = (float)++count / (float)length;
							progress.info = "Search on:" + tree.displayName + " - " + length;
						}
						if(!IsValidSearch(childs[i], searchParam, progresses, searcher, depth + 1)) {
							childs.RemoveAt(i);
							i--;
						}
					}
				}
				if(tree is SelectorSearchTreeView || tree.hasChildren && tree.children.Count > 0) {
					return true;
				}
				var sc = searcher.IsMatchSearch(tree, searchParam.splittedStrings, searchParam.searchKind, searchParam.searchFilter);
				if(tree is IRelevanceItem relevance) {
					if(tree is TypeTreeView) {
						if(sc >= 0.5f) {
							sc -= 0.2f;
						}
					}
					relevance.Score = sc;
				}
				return sc >= 0;
			}

			void AssignFilter(TreeViewItem tree, FilterAttribute filter) {
				if(tree is TypeTreeView typeTree) {
					typeTree.filter = filter;
				}
				if(tree.hasChildren) {
					if(tree is SelectorCategoryTreeView categ && categ.expanded == false) {
						foreach(var child in categ.childTrees) {
							AssignFilter(child, filter);
						}
						return;
					}
					foreach(var child in tree.children) {
						AssignFilter(child, filter);
					}
				}
			}

			public void Terminate() {
				if(searchThread != null) {
					searchThread.Abort();
					searchThread = null;
				}
			}
		}
	}
}
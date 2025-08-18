using System;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;

namespace MaxyGames.UNode {
	/// <summary>
	/// Provides nested class for implementing icon.
	/// </summary>
	public static class TypeIcons {
		/// <summary>
		/// Custom Icon, path is inside Resources folder.
		/// </summary>
		[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface, Inherited = false)]
		public sealed class IconPathAttribute : Attribute, ICustomIcon {
			public string path;
			private Texture texture;
			private bool hasInitialize;

			public IconPathAttribute(string path) {
				this.path = path;
			}

			public Texture GetIcon() {
				if(hasInitialize == false) {
					hasInitialize = true;
					texture = Resources.Load<Texture>(path);
				}
				return texture;
			}
		}

		/// <summary>
		/// Custom Icon, guid is the icon asset guid.
		/// </summary>
		[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface, Inherited = false)]
		public sealed class IconGuidAttribute : Attribute, ICustomIcon {
			public string guid;
			private Texture texture;
			private bool hasInitialize;

			public IconGuidAttribute(string guid) {
				this.guid = guid;
			}

			public Texture GetIcon() {
#if UNITY_EDITOR
				if(hasInitialize == false) {
					hasInitialize = true;
					var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
					if(string.IsNullOrEmpty(path) == false) {
						var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(path);
						texture = asset;
					}
				}
#endif
				return texture;
			}
		}

		class IconType : RuntimeType, ICustomIcon {
			public readonly Texture icon;

			public IconType(Texture icon) {
				this.icon = icon;
			}

			public Texture GetIcon() {
				return icon;
			}

			#region Overriden
			public override Type BaseType { get; }
			public override string Name { get; }

			public override FieldInfo GetField(string name, BindingFlags bindingAttr) {
				throw new NotImplementedException();
			}

			public override FieldInfo[] GetFields(BindingFlags bindingAttr) {
				throw new NotImplementedException();
			}

			public override MethodInfo[] GetMethods(BindingFlags bindingAttr) {
				throw new NotImplementedException();
			}

			public override PropertyInfo[] GetProperties(BindingFlags bindingAttr) {
				throw new NotImplementedException();
			}

			protected override TypeAttributes GetAttributeFlagsImpl() {
				throw new NotImplementedException();
			}
			#endregion
		}

		static readonly Dictionary<Texture, Type> typeMap = new Dictionary<Texture, Type>();

		/// <summary>
		/// Create a new icon type from the texture.
		/// This will cache the type for the texture, so it can be reused in runtime but not serializable.
		/// </summary>
		/// <param name="texture"></param>
		/// <returns></returns>
		public static Type FromTexture(Texture texture) {
			if(texture == null)
				return RuntimeType.Default;
			if(!typeMap.TryGetValue(texture, out var result)) {
				result = new IconType(texture);
				typeMap[texture] = result;
			}
			return result;
		}

		[IconPath("uNODE_Logo")]
		public class UNodeIcon { }

		[IconPath("Icons/IconPackage")]
		public class RuntimeTypeIcon { }

		[IconPath("Icons/bookmark")]
		public class BookmarkIcon { }

		[IconPath("Icons/IconFormula")]
		public class FormulaIcon { }

		[IconPath("Icons/database")]
		public class DatabaseIcon { }

		[IconPath("Icons/IconFolder")]
		public class FolderIcon { }

		[IconPath("Icons/IconBug")]
		public class BugIcon {}

		[IconPath("VSIcons/Graph_32x")]
		public class GraphIcon { }

		[IconPath("VSIcons/EnumItem_32x")]
		public class EnumItemIcon { }

		[IconPath("VSIcons/Class_32x")]
		public class ClassIcon { }

		[IconPath("VSIcons/Keyword_32x")]
		public class KeywordIcon { }

		[IconPath("VSIcons/Constant_32x")]
		public class ConstrainIcon { }

		[IconPath("VSIcons/Delegate_32x")]
		public class DelegateIcon { }

		[IconPath("VSIcons/Enumerator_32x")]
		public class EnumIcon { }

		[IconPath("VSIcons/Extension_32x")]
		public class ExtensionIcon { }

		[IconPath("VSIcons/Field_32x")]
		public class FieldIcon { }

		[IconPath("VSIcons/Interface_32x")]
		public class InterfaceIcon { }

		[IconPath("VSIcons/LocalVariable_32x")]
		public class LocalVariableIcon { }

		[IconPath("VSIcons/Method_32x")]
		public class MethodIcon { }

		[IconPath("VSIcons/ExtensionMethod_32x")]
		public class ExtensionMethodIcon { }

		[IconPath("VSIcons/Void_32x")]
		public class VoidIcon { }

		[IconPath("VSIcons/Namespace_32x")]
		public class NamespaceIcon { }

		[IconPath("VSIcons/Property_32x")]
		public class PropertyIcon { }

		[IconPath("VSIcons/Structure_32x")]
		public class StructureIcon { }

		[IconPath("Icons/IconDice")]
		public class RandomIcon { }

		public class FlowIcon { }
		public class ValueIcon { }
		public class BranchIcon { }
		public class ClockIcon { }
		public class RepeatIcon { }
		public class RepeatOnceIcon { }
		public class SwitchIcon { }
		public class RotationIcon { }

		[IconPath("Icons/calculator")]
		public class CalculatorIcon { }

		[IconPath("Icons/IconScriptCode")]
		public class ScriptCodeIcon { }

		public class MouseIcon { }
		public class EventIcon { }

		[IconPath("Icons/IconInput")]
		public class InputIcon { }

		[IconPath("Icons/IconOutput")]
		public class OutputIcon { }

		[IconPath("Icons/IconString")]
		public class StringIcon { }

		[IconPath("Icons/IconInteger")]
		public class IntegerIcon { }

		[IconPath("Icons/IconFloat")]
		public class FloatIcon { }

		[IconPath("Icons/IconVector2")]
		public class Vector2Icon { }

		[IconPath("Icons/IconVector3")]
		public class Vector3Icon { }

		[IconPath("Icons/IconVector4")]
		public class Vector4Icon { }

		[IconPath("Icons/key")]
		public class KeyTypeIcon { }

		[IconPath("Icons/text_list_bullets")]
		public class ListTypeIcon { }

		[IconPath("Icons/note")]
		public class NoteIcon { }

		[IconPath("Icons/no_requirements")]
		public class NullTypeIcon { }

		[IconPath("Icons/IconAction")]
		public class ActionIcon { }

		[IconPath("Icons/data_validation")]
		public class ValidationIcon { }

		[IconPath("Icons/IconAnd")]
		public class BitwiseAndIcon { }

		[IconPath("Icons/IconAnd2")]
		public class AndIcon { }

		[IconPath("Icons/IconOr")]
		public class BitwiseOrIcon { }

		[IconPath("Icons/IconOr2")]
		public class OrIcon { }

		[IconPath("Icons/IconNot")]
		public class NotIcon { }

		[IconPath("Icons/compare")]
		public class CompareIcon { }

		[IconPath("Icons/module")]
		public class StateIcon { }

		[IconPath("Icons/IconRefresh")]
		public class RefreshIcon { }

		[IconPath("Icons/IconFlowJoin")]
		public class JoinIcon { }

		[IconPath("Icons/IconGreaterThan")]
		public class GreaterThan { }

		[IconPath("Icons/IconGreaterThanOrEqual")]
		public class GreaterThanOrEqual { }

		[IconPath("Icons/IconLessThan")]
		public class LessThan { }

		[IconPath("Icons/IconLessThanOrEqual")]
		public class LessThanOrEqual { }

		[IconPath("Icons/IconEqual")]
		public class Equal { }

		[IconPath("Icons/IconNotEqual")]
		public class NotEqual { }

		[IconPath("Icons/IconChange")]
		public class SetValueIcon { }

		[IconPath("Icons/IconAdd")]
		public class SetAddIcon { }

		[IconPath("Icons/IconAdd2")]
		public class AddIcon { }

		[IconPath("Icons/IconDivide")]
		public class SetDivideIcon { }

		[IconPath("Icons/IconDivide2")]
		public class DivideIcon { }

		[IconPath("Icons/IconModulo")]
		public class SetModuloIcon { }

		[IconPath("Icons/IconModulo2")]
		public class ModuloIcon { }

		[IconPath("Icons/IconMultiply")]
		public class SetMultiplyIcon { }

		[IconPath("Icons/IconMultiply2")]
		public class MultiplyIcon { }

		[IconPath("Icons/IconPower")]
		public class PowerIcon { }

		[IconPath("Icons/IconPower2")]
		public class PowerIcon2 { }

		[IconPath("Icons/IconSubtract")]
		public class SetSubtractIcon { }

		[IconPath("Icons/IconSubtract2")]
		public class SubtractIcon { }

		[IconPath("Icons/IconMissing")]
		public class MissingIcon { }
	}
}
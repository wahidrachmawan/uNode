using MaxyGames.OdinSerializer;
using System;
using System.Collections;
using System.Collections.Generic;

[assembly: RegisterFormatter(typeof(MaxyGames.UNode.MemberData.MemberDataFormatter))]
[assembly: RegisterFormatter(typeof(MaxyGames.UNode.SerializedType.SerializedTypeFormatter))]
[assembly: RegisterFormatter(typeof(MaxyGames.UNode.NodeSerializedDataFormatter))]

namespace MaxyGames.UNode {
	internal abstract class ValueFormatter<TValue> : MinimalBaseFormatter<TValue> {
		public T ReadValue<T>(IDataReader reader) {
			try {
				var serializer = Serializer.Get<T>();
				var entryValue = serializer.ReadValue(reader);
				return entryValue;
			}
			catch(Exception ex) {
				reader.Context.Config.DebugContext.LogException(ex);
			}
			return default;
		}

		public void ReadValue<T>(ref T instance, IDataReader reader) {
			try {
				var serializer = Serializer.Get<T>();
				var entryValue = serializer.ReadValue(reader);
				instance = entryValue;
			}
			catch(Exception ex) {
				reader.Context.Config.DebugContext.LogException(ex);
			}
		}

		public void WriteValue<T>(string name, T value, IDataWriter writer) {
			var serializer = Serializer.Get<T>();

			try {
				serializer.WriteValue(name, value, writer);
			}
			catch(Exception ex) {
				writer.Context.Config.DebugContext.LogException(ex);
			}
		}
	}

	internal class NodeSerializedDataFormatter : ValueFormatter<NodeSerializedData> {
		protected override void Read(ref NodeSerializedData value, IDataReader reader) {
			EntryType entryType;

			while((entryType = reader.PeekEntry(out var name)) != EntryType.EndOfNode && entryType != EntryType.EndOfArray && entryType != EntryType.EndOfStream) {
				if(string.IsNullOrEmpty(name)) {
					reader.Context.Config.DebugContext.LogError("Entry of type \"" + entryType + "\" in node \"" + reader.CurrentNodeName + "\" is missing a name.");
					reader.SkipEntry();
					continue;
				}

				switch(name) {
					case nameof(value.data):
						ReadValue(ref value.data, reader);
						break;
					case nameof(value.references):
						ReadValue(ref value.references, reader);
						break;
					case nameof(value.serializedType):
						ReadValue(ref value.serializedType, reader);
						break;
				}
			}
		}

		protected override void Write(ref NodeSerializedData value, IDataWriter writer) {
			WriteValue(nameof(value.data), value.data, writer);
			WriteValue(nameof(value.references), value.references, writer);
			WriteValue(nameof(value.serializedType), value.serializedType, writer);
		}
	}

	public partial class SerializedType {
		internal class SerializedTypeFormatter : ValueFormatter<SerializedType> {
			protected override void Read(ref SerializedType value, IDataReader reader) {
				EntryType entryType;

				while((entryType = reader.PeekEntry(out var name)) != EntryType.EndOfNode && entryType != EntryType.EndOfArray && entryType != EntryType.EndOfStream) {
					if(string.IsNullOrEmpty(name)) {
						reader.Context.Config.DebugContext.LogError("Entry of type \"" + entryType + "\" in node \"" + reader.CurrentNodeName + "\" is missing a name.");
						reader.SkipEntry();
						continue;
					}

					switch(name) {
						case nameof(kind):
							ReadValue(ref value.kind, reader);
							break;
						case nameof(serializedString):
							ReadValue(ref value.serializedString, reader);
							break;
						case nameof(serializedBytes):
							ReadValue(ref value.serializedBytes, reader);
							break;
						case nameof(references):
							ReadValue(ref value.references, reader);
							break;
					}
				}
			}

			protected override void Write(ref SerializedType value, IDataWriter writer) {
				WriteValue(nameof(value.kind), value.kind, writer);

				switch(value.kind) {
					case SerializedTypeKind.Native:
						WriteValue(nameof(value.serializedString), value.serializedString, writer);
						break;
					case SerializedTypeKind.Runtime:
					case SerializedTypeKind.GenericParameter:
						WriteValue(nameof(value.serializedBytes), value.serializedBytes, writer);
						WriteValue(nameof(value.references), value.references, writer);
						break;
				}
			}
		}
	}

	public partial class MemberData {
		internal class MemberDataFormatter : ValueFormatter<MemberData> {
			protected override void Read(ref MemberData member, IDataReader reader) {
				EntryType entryType;

				while((entryType = reader.PeekEntry(out var name)) != EntryType.EndOfNode && entryType != EntryType.EndOfArray && entryType != EntryType.EndOfStream) {
					if(string.IsNullOrEmpty(name)) {
						reader.Context.Config.DebugContext.LogError("Entry of type \"" + entryType + "\" in node \"" + reader.CurrentNodeName + "\" is missing a name.");
						reader.SkipEntry();
						continue;
					}

					switch(name) {
						case nameof(_targetType):
							member._targetType = ReadValue<TargetType>(reader);
							switch(member.targetType) {
								case TargetType.Type:
								case TargetType.uNodeType:
								case TargetType.Constructor:
									member.isStatic = true;
									break;
							}
							continue;
						case nameof(startSerializedType):
							ReadValue(ref member.startSerializedType, reader);
							break;
						case nameof(targetSerializedType):
							ReadValue(ref member.targetSerializedType, reader);
							break;
						case nameof(_items):
							ReadValue(ref member._items, reader);
							break;
						case nameof(_instance):
							ReadValue(ref member._instance, reader);
							break;
						case nameof(_isStatic):
							ReadValue(ref member._isStatic, reader);
							break;
						default:

							break;
					}
				}
			}

			protected override void Write(ref MemberData member, IDataWriter writer) {
				WriteValue(nameof(member._targetType), member._targetType, writer);

				switch(member._targetType) {
					case TargetType.None:
					case TargetType.Null:
					case TargetType.uNodeGenericParameter:
					case TargetType.RuntimeMember:
					case TargetType.uNodeIndexer:

						break;
					case TargetType.Values:
					case TargetType.Self:
						WriteValue(nameof(member.startSerializedType), member.startSerializedType, writer);
						WriteValue(nameof(member._instance), member._instance, writer);
						break;
					case TargetType.Type:
					case TargetType.uNodeType:
						WriteValue(nameof(member.startSerializedType), member.startSerializedType, writer);
						WriteValue(nameof(member._items), member._items, writer);
						break;
					case TargetType.Constructor:
					case TargetType.uNodeConstructor:
					case TargetType.uNodeFunction:
					case TargetType.uNodeLocalVariable:
					case TargetType.uNodeParameter:
					case TargetType.uNodeProperty:
					case TargetType.uNodeVariable:
						WriteValue(nameof(member.startSerializedType), member.startSerializedType, writer);
						WriteValue(nameof(member.targetSerializedType), member.targetSerializedType, writer);
						WriteValue(nameof(member._instance), member._instance, writer);
						WriteValue(nameof(member._items), member._items, writer);
						break;
					case TargetType.Event:
					case TargetType.Field:
					case TargetType.Method:
					case TargetType.Property:
						WriteValue(nameof(member.startSerializedType), member.startSerializedType, writer);
						WriteValue(nameof(member.targetSerializedType), member.targetSerializedType, writer);
						WriteValue(nameof(member._instance), member._instance, writer);
						WriteValue(nameof(member._isStatic), member._isStatic, writer);
						WriteValue(nameof(member._items), member._items, writer);
						break;
				}
			}
		}
	}
}
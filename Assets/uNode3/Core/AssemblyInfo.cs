using MaxyGames.UNode;
using System.Runtime.CompilerServices;
using UnityEngine;

[assembly: InternalsVisibleTo("uNode3.Core.Editor")]
[assembly: InternalsVisibleTo("uNode3.Editor")]
[assembly: InternalsVisibleTo("uNode3.Compiler")]
[assembly: InternalsVisibleTo("uNode3.Pro.Editor")]

#if UNITY_6000_3_OR_NEWER
[assembly: MakeSerializable(typeof(Variable))]
[assembly: MakeSerializable(typeof(Property))]
[assembly: MakeSerializable(typeof(Function))]
[assembly: MakeSerializable(typeof(Constructor))]
[assembly: MakeSerializable(typeof(NodeObject))]
[assembly: MakeSerializable(typeof(FunctionContainer))]
[assembly: MakeSerializable(typeof(VariableContainer))]
[assembly: MakeSerializable(typeof(PropertyContainer))]
[assembly: MakeSerializable(typeof(ConstructorContainer))]
[assembly: MakeSerializable(typeof(MainGraphContainer))]
[assembly: MakeSerializable(typeof(EventGraphContainer))]
[assembly: MakeSerializable(typeof(StateGraphContainer))]
[assembly: MakeSerializable(typeof(TransitionContainer))]
[assembly: MakeSerializable(typeof(BlockContainer))]
#endif
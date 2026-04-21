using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;

namespace MaxyGames.UNode {
	[Serializable]
	public class GeneratedScriptData {
		/// <summary>
		/// The file name for the generated c# script
		/// </summary>
		public string fileName;
		public TypeData selfTypeData = new TypeData();
		public List<TypeData> typeDatas = new List<TypeData>();

		/// <summary>
		/// The type name for the generated c# script, this is used for the class name of the generated script and also used for the identifier of the graph.
		/// </summary>
		public string typeName => selfTypeData?.typeName;

		/// <summary>
		/// Indicates whether debugging is enabled for generated c# script. this is useful for debugging the graph in editor but it will reduce the performance of the generated script.
		/// </summary>
		public bool debug = false;
		/// <summary>
		/// If true, the debugging will be enabled for the value node in the generated c# script.
		/// </summary>
		public bool debugValueNode = true;

		/// <summary>
		/// If true, the graph will be compiled to C# to run using native c# performance on build or in editor using ( Generate C# Scripts ) menu.
		/// </summary>
		public bool compileToScript = true;

		[Serializable]
		public class ObjectData {
			public string name;
			public UnityEngine.Object value;
		}
		/// <summary>
		/// The list of unity objects that used in the graph, this is used for the reference of the unity objects in the generated c# script.
		/// </summary>
		public List<ObjectData> unityObjects = new List<ObjectData>();

		[Serializable]
		public class TypeData {
			public string typeName;
			public UnityEngine.Object reference;
		}
	}
}
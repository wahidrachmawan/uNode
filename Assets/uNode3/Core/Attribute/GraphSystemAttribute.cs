using System;

namespace MaxyGames.UNode {
	[System.AttributeUsage(AttributeTargets.Class)]
	public class GraphElementAttribute : Attribute {

	}

	[System.AttributeUsage(AttributeTargets.Class)]
	public class GraphSystemAttribute : Attribute {
		/// <summary>
		/// Support modifier modification
		/// </summary>
		public bool supportModifier = true;
		/// <summary>
		/// Support attribute modification
		/// </summary>
		public bool supportAttribute = true;
		/// <summary>
		/// Support generic modification, default is false
		/// </summary>
		public bool supportGeneric = false;

		/// <summary>
		/// If true, when compile the graph the generated script will be placed in project. If false, when compile the generated script will be placed somewhere and are handled by uNode. 
		/// </summary>
		public bool isScriptGraph = false;

		/// <summary>
		/// Allow the graph to be compiled to script by using uNode Editor
		/// </summary>
		public bool allowCompileToScript = true;
		/// <summary>
		/// Allow the graph to be compiled by Full Script Compilation, default is false
		/// </summary>
		public bool allowAutoCompile = false;
		/// <summary>
		/// Allow the uNode editor to preview the generated script
		/// </summary>
		public bool allowPreviewScript = true;
		/// <summary>
		/// The generation mode of graph.
		/// -Default is using global settings
		/// -Performance: is forcing to generate pure script, this will have native performance since it have strongly type reference
		/// But it may give errors when other graph is not compiled into script
		/// -Compatibility: is to ensure the script compatible with all graph even when other graph is not compiled into script
		/// </summary>
		public GenerationKind generationKind = GenerationKind.Default;

		/// <summary>
		/// The inherith type when generating script.
		/// If null, will use GetInherithType from the graph itself.
		/// </summary>
		public Type inherithFrom;

		/// <summary>
		/// The type of the graph calss, this will filled automaticly
		/// </summary>
		public Type type;

        public GraphSystemAttribute() { }

		private static GraphSystemAttribute _default;
		public static GraphSystemAttribute Default {
			get {
				if(_default == null) {
					_default = new GraphSystemAttribute();
				}
				return _default;
			}
		}
	}
}
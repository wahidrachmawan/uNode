using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Object = UnityEngine.Object;

namespace MaxyGames.UNode {
	public abstract class GraphAnalyzer {
		public virtual int order => 0;

		public virtual bool IsValidAnalyzerForGraph(Type type) => false;

		public virtual bool IsValidAnalyzerForElement(Type type) => false;

		public virtual bool IsValidAnalyzerForNode(Type type) => false;

		public virtual void CheckGraphErrors(ErrorAnalyzer analyzer, IGraph graph) { }
		public virtual void CheckElementErrors(ErrorAnalyzer analyzer, UGraphElement element) { }
		public virtual void CheckNodeErrors(ErrorAnalyzer analyzer, Node node) { }
	}
}
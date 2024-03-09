using UnityEngine;
using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

namespace MaxyGames.UNode.Nodes {
	[NodeMenu("Data", "Expression", icon = typeof(TypeIcons.FormulaIcon))]
	public class ExpressionNode : ValueNode {
		public class InputData {
			public string name;
			[Filter(DisplayGeneratedRuntimeType = false)]
			public SerializedType type = new SerializedType(null);

			public ValueInput port { get; set; }
		}
		public string expression = "2 + 2";

		[Filter(DisplayGeneratedRuntimeType = false)]
		public SerializedType outputType = typeof(float);

		public List<InputData> inputs = new List<InputData>();

		public bool displayError = true;


		public bool optionUseStatic = true;

		public byte[] rawAssembly;

		public const string ExpressionType = "Expression";
		public const string ExpressionMethod = "M_Expression";

		public bool IsCompiled {
			get => rawAssembly != null && rawAssembly.Length > 0;
		}

		protected override void OnRegister() {
			for(int i = 0; i < inputs.Count; i++) {
				var input = inputs[i];
				input.port = ValueInput(input.name, () => input.type?.type ?? ReturnType()).SetName(input.name);
			}
			base.OnRegister();
		}

		public override string GetTitle() {
			if(string.IsNullOrEmpty(expression))
				return "Expression";
			return expression;
		}

		public override Type ReturnType() => outputType;

		#region Runtime
		private MethodInfo method;
		public override void OnRuntimeInitialize(GraphInstance instance) {
			if(IsCompiled == false) {
				throw new Exception("Formula is not compiled, compile it first for use.");
			}
			var assembly = Assembly.Load(rawAssembly);
			if(assembly == null) {
				throw new Exception("Couldn't load compiled assembly ( maybe it is outdated ), recompile should fix it.");
			}
			var type = assembly.GetType(ExpressionType);
			if(type == null) {
				throw new Exception("Couldn't find matching compiled type ( maybe the compiled assembly is outdated ), recompile should fix it");
			}
			method = type.GetMethod(ExpressionMethod);
			if(method == null) {
				throw new Exception("Couldn't find matching compiled method ( maybe the compiled assembly is outdated ), recompile should fix it");
			}
		}

		public override object GetValue(Flow flow) {
			return method.InvokeOptimized(null, inputs.Select(i => i.port.GetValue(flow)).ToArray());
		}
		#endregion

		#region Codegen
		public override void OnGeneratorInitialize() {
			base.OnGeneratorInitialize();

			CG.RegisterPostGeneration((classData) => {
				var methodName = CG.GenerateName(name, this);
				var method = new CG.MData(methodName, ReturnType(), inputs.Select(i => new CG.MPData(i.name, i.type?.type ?? outputType)).ToArray());
				method.owner = this;
				method.modifier = new FunctionModifier() {
					Public = false,
					Private = true,
					Static = optionUseStatic,
				};
				method.code = $"return {expression};";
				classData.RegisterFunction(method.GenerateCode());
			});
		}

		protected override string GenerateValueCode() {
			return CG.Invoke(string.Empty, CG.GenerateName(name, this), inputs.Select(i => CG.GeneratePort(i.port)).ToArray());
		}
		#endregion

		public override void CheckError(ErrorAnalyzer analizer) {
			base.CheckError(analizer);
			if(displayError && IsCompiled == false && nodeObject.graphContainer is not IScriptGraphType) {
				analizer.RegisterError(this, "Expression is not compiled.\nDisable `Display Error` if node never run on Reflection mode");
			}
		}
	}
}
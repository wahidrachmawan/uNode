using UnityEngine;
using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

namespace MaxyGames.UNode.Nodes {
	[NodeMenu("Data", "Formula", icon = typeof(TypeIcons.FormulaIcon))]
	public class FormulaNode : FlowAndValueNode {
		public enum FormulaKind {
			Simple,
			Complex,
		}

		public class InputData {
			public string id = uNodeUtility.GenerateUID();
			public string name;
			[Filter(DisplayGeneratedRuntimeType = false)]
			public SerializedType type = typeof(float);

			public ValueInput port { get; set; }
		}
		public FormulaKind kind = FormulaKind.Simple;
		public string formula = "a + b";
		[Filter(DisplayGeneratedRuntimeType =false, VoidType = true)]
		public SerializedType outputType = typeof(float);

		public List<InputData> inputs = new List<InputData>() { 
			new InputData() { name = "a" }, 
			new InputData() { name = "b" } 
		};

		public bool displayError = true;


		public bool optionUseStatic = true;

		public byte[] rawAssembly;

		public const string FormulaType = "Formula";
		public const string FormulaMethod = "M_Formula";

		public bool IsCompiled {
			get => rawAssembly != null && rawAssembly.Length > 0;
		}

		protected override void OnRegister() {
			foreach(var input in inputs) {
				input.port = ValueInput(input.id, () => input.type).SetName(input.name);
			}
			if(outputType == typeof(void)) {
				RegisterFlowPort();
			}
			else {
				RegisterValuePort();
			}
		}

		public override string GetTitle() {
			if(kind == FormulaKind.Complex)
				return name;
			if(string.IsNullOrEmpty(formula))
				return "Formula";
			return formula;
		}

		public override Type ReturnType() => outputType.type;

		#region Runtime
		private MethodInfo method;
		public override void OnRuntimeInitialize(GraphInstance instance) {
			Init();
		}

		void Init() {
			if(IsCompiled == false) {
				throw new Exception("Formula is not compiled, compile it first for use.");
			}
			var assembly = Assembly.Load(rawAssembly);
			if(assembly == null) {
				throw new Exception("Couldn't load compiled assembly ( maybe it is outdated ), recompile should fix it.");
			}
			var type = assembly.GetType(FormulaType);
			if(type == null) {
				throw new Exception("Couldn't find matching compiled type ( maybe the compiled assembly is outdated ), recompile should fix it");
			}
			method = type.GetMethod(FormulaMethod);
			if(method == null) {
				throw new Exception("Couldn't find matching compiled method ( maybe the compiled assembly is outdated ), recompile should fix it");
			}
		}

		public override object GetValue(Flow flow) {
			if(method == null)
				Init();
			return method.InvokeOptimized(null, inputs.Select(i => i.port.GetValue(flow)).ToArray());
		}

		protected override void OnExecuted(Flow flow) {
			if(method == null)
				Init();
			method.InvokeOptimized(null, inputs.Select(i => i.port.GetValue(flow)).ToArray());
		}
		#endregion

		#region Codegen
		public override void OnGeneratorInitialize() {
			base.OnGeneratorInitialize();

			CG.RegisterPostGeneration((classData) => {
				var methodName = CG.GenerateName(name, this);
				var method = new CG.MData(methodName, ReturnType(), inputs.Select(i => new CG.MPData(i.name, i.type)).ToArray());
				method.owner = this;
				method.modifier = new FunctionModifier() {
					Public = false,
					Private = true,
					Static = optionUseStatic,
				};
				switch(kind) {
					case FormulaKind.Simple: {
						if(ReturnType() == typeof(void)) {
							method.code = $"{formula};";
						}
						else {
							method.code = $"return {formula};";
						}
						break;
					}
					case FormulaKind.Complex: {
						method.code = formula;
						break;
					}
				}
				classData.RegisterFunction(method.GenerateCode());
			});
		}

		protected override string GenerateValueCode() {
			return CG.Invoke(string.Empty, CG.GenerateName(name, this), inputs.Select(i => CG.GeneratePort(i.port)).ToArray());
		}

		protected override string GenerateFlowCode() {
			return CG.Flow(
				CG.FlowInvoke(string.Empty, CG.GenerateName(name, this), inputs.Select(i => CG.GeneratePort(i.port)).ToArray()),
				CG.FlowFinish(enter, exit)
			);
		}
		#endregion

		public override void CheckError(ErrorAnalyzer analizer) {
			base.CheckError(analizer);
			if(displayError && IsCompiled == false && nodeObject.graphContainer is not IScriptGraphType) {
				analizer.RegisterError(this, "Formula is not compiled.\nDisable `Display Error` if node never run on Reflection mode");
			}
		}
	}
}
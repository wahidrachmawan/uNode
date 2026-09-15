# uNode Visual Scripting System - Complete Reference

uNode is a powerful visual scripting solution for Unity. It lets developers build complex game logic, AI behaviors, and interactions entirely with nodes in the editor; graphs are converted into optimized C# at runtime for near-handwritten performance. This document maps the codebase so any agent can quickly understand what uNode is, how it is structured, and how to use or extend it.

> Source root: `Assets/uNode3` · Install as package `com.maxygames.unode` (git: `https://github.com/wahidrachmawan/uNode.git?path=Assets/uNode3`)

---

## 1. What uNode Is

- **Visual scripting for Unity** : node graphs authored in the editor, saved as assets, referenced by GameObject components.
- **Two execution backends**: optimized **Native C#** (generated code, default in builds) and **Optimized Reflection** (live-editable, used in editor/playmode prototyping).
- **Two compile paths**: **Runtime Graphs** (executable immediately via reflection or compiled C#) and **C# Graphs** (produce a C# script asset for advanced tasks).
- **Graph kinds**: Flow Graphs, State/Event Graphs (flow + state machine + behavior tree combined), Macros (parameterized, reusable graphs), Runtime Graphs, C# Graphs, ECS Graphs (experimental, separate DOTS package).
- **Pro tier** (this repo is Pro): C# Parser (C# script -> graph), Realtime C# Preview with syntax highlighting, Find References, Graph Hierarchy, Graph Search, Graph Trimming for builds, Call Stack window, TypePatcher, XMLDoc generator, bookmarks.
- Requires Unity 2022.3+ and **.NET 4.x** API level (uses Odin Serializer).
- License: Apache-2.0 (free tier); this checkout is the uNode Pro source.

Feature summary (from README): C# Script Generation, Fast Enter Play Mode, Dynamic Nodes for any Unity/user API, Built-in Documentation, Live Editing, Instant Node Search, Graph Organizing (namespace/variable/function grouping, node groups, comments, notes), Graph Inheritance, Graph Debugging (breakpoints, watch connection/variable - Pro adds connection debug logging), Pro-only C# Parser/Preview/Find References/Hierarchy/Search/Trimming.

Repository version per `Assets/uNode3/changelog.md`: **v3.2.11** (latest; dim unused nodes, partial C# graph support).

---

## 2. Repository / Package Structure

```
uNode-Pro/
├── Assets/
│   ├── AI/Script/            # Agent/dev tooling: AI_UNODE.asmref, GraphComparisonEngine.cs,
│   │                         #   GraphComparisonWindow.cs, BreakpointsWindow.csx
│   ├── HotReload/            # HotReload settings asset used in this project
│   ├── Scenes/{A,B,C,Test}/  # Sample scenes + graph assets (A_Graph, B_Graph, etc.)
│   ├── Test/                 # Editor tests, test scenes, Mono debugger tooling
│   ├── uNode.Generated/      # Auto-generated runtime data:
│   │   └── Resources/uNodeDatabase.asset   # serialized graph database
│   └── uNode3/               # <<< THE PACKAGE ===
├── Packages/com.unity.asset-store-tools/   # Asset Store upload tooling (unrelated)
├── Library/uNodeRoslynCompiler/            # Roslyn compiler cache (generated)
├── TempScript/Scripts/        # uNode-generated C# for samples (generated)
└── uNode3Data/                # graph/data scratch
```

### 2.1 uNode3 package layout (the interesting part)

| Folder | Contents |
| --- | --- |
| `Core/` | Runtime + shared engine (asmdef `uNode3.Core`). Graphs, nodes, ports, sequences, reflection, codegen, macros, state machines, runtime helpers, OdinSerializer. |
| `Core.Editor/` | Base editor layer (asmdef `uNode3.Core.Editor`). Graph editor core, UI Toolkit graph view, item selector/searcher, GUI field controls & property drawers, bindings, commands, compiler glue. |
| `Core.Editor/Compiler/` | `uNode3.Compiler` asmdef: Roslyn integration (`RoslynUtility.cs`, `RoslynExtensions.cs`). |
| `Editor/` | Main editor (asmdef `uNode3.Editor`): windows, node views/drawers, drag handlers, node items, analyzers, custom editors, resources (icons/themes/uxml/doc XML). |
| `Editor/CodeCompiler/` | 3 asmdefs: `MaxyGames.CodeCompiler`, `Unity.CodeCompiler.CodeGen` (IL2CPP runner), `MaxyGames.CompilerRunner` (Roslyn compile + run). |
| `Runtime/` | `uNode3.Runtime` asmdef: `Script/Nodes/**` - the built-in node library (Behavior Trees, Collections, Flows, Loop, Transition, Value, Yield, Other). |
| `Integrations/` | `uNode3.Integration.InputSystem` (new Input System event nodes) and `uNode3.Integration.UGUI` (uGUI event nodes). |
| `Pro.Editor/` | `uNode3.Pro.Editor` + `uNode3.CSParser` asmdefs: C# Parser (`CSharpParser.cs` 4468 lines), SyntaxHighlighter, SyntaxAnalizer, CallStack/MonoDebugger, Bookmark, C# Parser windows, XMLDocGenerator, TypePatcher. |
| `Plugins/` | Bundled dependencies: Odin Serializer (+AOT/JIT/EditorOnly) and Roslyn. |
| `Samples~/` | 2D UFO, Custom Nodes, Global Event, Playground, RollABall (hidden from main package import). |

### 2.2 Assembly definitions (dependencies at a glance)

- `uNode3.Core` - standalone runtime engine; precompiled ref `MaxyGames.OdinSerializer.dll`; functional in builds.
- `uNode3.Core.Editor` - editor extensions on core.
- `uNode3.Compiler` - Roslyn utility layer (editor).
- `uNode3.Editor` - main editor assembly (references Core + Core.Editor).
- `MaxyGames.CodeCompiler` / `Unity.CodeCompiler.CodeGen` / `MaxyGames.CompilerRunner` - layered compilation stack.
- `uNode3.Runtime` - built-in script node library.
- `uNode3.Integration.InputSystem` / `uNode3.Integration.UGUI` - optional integrations.
- `uNode3.Pro.Editor` / `uNode3.CSParser` - Pro features.

Namespaces: `MaxyGames.UNode` (engine), `MaxyGames.UNode.Nodes` (built-in nodes), `MaxyGames.UNode.Editor`, `MaxyGames.UNode.Pro.Editor`, `MaxyGames` containing the code generator static class `CG`.

---

## 3. Core Architecture

### 3.1 Graph model

**`Graph : UGraphElement`** (`Core/Graph/Graph.cs`) is the container for all graph data. It lazily creates child containers:

- `VariableContainer` - variables
- `ConstructorContainer` - constructors
- `PropertyContainer` - properties
- `FunctionContainer` - function/method definitions
- `EventGraphContainer` - event sub-graphs (Start, Update, OnCollision, etc.)
- `MainGraphContainer` - main flow

Fields: `graphLayout` (Vertical/Horizontal), `attributes`, non-serialized `owner` (`IGraph`), `version` (hot-reload stamp).

Graph asset kinds derive from `ScriptGraph` / `ClassScript` / `ClassDefinition` / `EnumScript` / `InterfaceScript` / `StateGraphContainer` / `GraphInterface`. `GraphComponent` (`Core/Graph/GraphComponent.cs`) is the MonoBehaviour that hosts a graph at runtime: it holds `m_serializedGraph` (a `SerializedGraph`), `usingNamespaces`, `scriptData` (`GeneratedScriptData`), and `variableOverrides` (per-instance variable override list - used for prefab instances).

Key interfaces (`Core/Classes/Interfaces.cs`): `IGraph`, `ITypeWithScriptData`, `IGraphWithScriptData`, `ITypeGraph`, `IClassGraph`, `IClassDefinition`, `IClassIdentifier`, `IInstancedGraph`, `IGraphWithVariables`, plus `INamespace`/`IUsingNamespace`/`INamespaceSystem`.

### 3.2 Node model

- `NodeObject` (`Core/Nodes/Base/NodeObject.cs`) is the serialized instance of a node inside a graph. It stores `NodeSerializedData` (a `byte[]` payload + `references` list that lazily reloads from `USerializedValueWrapper` entries - this is how graph data serializes compactly via Unity).
- `Node` (abstract, `Core/Nodes/Base/Node.cs`) is the user-facing base class for authoring nodes. Highlights:
  - `protected abstract void OnRegister()` - must create ports (`ValueInput`, `ValueOutput`, `FlowInput`, `FlowOutput` via protected helpers) and register members.
  - `GetValue(Flow)`, `SetValue(Flow, object)` - value node contract; `CanGetValue()`/`CanSetValue()` gates.
  - `OnGeneratorInitialize()` - codegen hook; default implementation registers `GenerateFlowCode()` on the primary flow input and `GenerateValueCode()` on the primary value output. Overriding without calling base disables those two virtuals (fine - just register your own closures via `CG.RegisterPort`).
  - `GetTitle()/GetRichTitle()/GetRichName()`, `GetNodeIcon()`, `Styles`, `OnRuntimeInitialize(GraphInstance)`, `OnInspectorInitialize()`, `CheckError(ErrorAnalyzer)`.
  - Ports held on `NodeObject`: `primaryFlowInput`, `primaryFlowOutput`, `primaryValueInput`, `primaryValueOutput`; `IsFlowNode()`/`IsValueNode()` helpers.

Node hierarchy (from `Node.cs` and `BaseEventNode.cs`):

```
Node (abstract)
├── BaseFlowNode         → enter: FlowInput
│   ├── FlowNode         → + exit: FlowOutput, OnExecuted(Flow), IsCoroutine(), GenerateFlowCode()
│   │   ├── CoroutineNode                     (IsSelfCoroutine() = true)
│   │   ├── FlowAndValueNode                  (+ output: ValueOutput)
│   │   ├── NodeAction : IStackedNode         (block node holding stacked flow nodes)
│   │   └── ...built-in flow nodes (NodeIf, NodeReturn, ...)
│   └── BaseCoroutineNode
├── EntryNode / BaseEntryNode                 (graph/event entry points)
└── BaseEventNode                             (event sources)
    ├── BaseGraphEvent                        (per-graph event method generation: DoGenerateCode, GenerateMethodData)
    └── BaseComponentEvent                    (MonoBehaviour handler registration)
```

Value/property nodes (CallNode equivalents): `MultipurposeNode`, `NodeAction`, `ExpressionNode`, `FormulaNode`, `NodeSetValue`, `NodeReroute`, cache/convert/comparison nodes under `Core/Nodes/Data/` (e.g. `NodeConvert`, `ComparisonNode`, `MultiArithmeticNode`, `CacheNode`), `MultipurposeMember` (`Core/Classes/MultipurposeMember.cs`), `MemberData` (`Core/Graph/MemberData.cs`) - the serialized reference to any C# field/property/method/event, heavily used across nodes.

### 3.3 Ports & connections

`Core/Ports/`: `UPort` (base), `FlowInput`, `FlowOutput`, `ValueInput`, `ValueOutput`, plus `Connection`, `FlowConnection`, `ValueConnection`.

- `ValueInput` features: `filter` (`FilterAttribute`), `DefaultValue` (as `MemberData`), `UseDefaultValue`, `IsOptional`/`OptionalValue`, `IsVariable`, `TargetType`, `isAssigned`, `GetTargetPort()`.
- `FlowInput` accepts an action and optional parameter type; `FlowOutput` can have a name and optional value/output assignment.
- `Flow` (`Core/Ports/Flow.cs`) is the runtime execution context: `flow.Next(port)`, `flow.state` (StateType), `GetLocalData/SetLocalData` keyed on graph elements, plus `GraphRuntimeData` base with `instance`, `graph`, `target`, `eventData`.
- Runtime value caching: `ValueOutputData`, `RuntimeLocalValue`, `GraphInstance.GetOutputData/SetOutputData` (`Core/Graph/GraphInstance.cs`).
- Port kinds: `PortKind { FlowInput, FlowOutput, ValueInput, ValueOutput }`; `PortAccessibility { ReadWrite, ReadOnly, WriteOnly }`.

### 3.4 Reflection layer

`Core/Reflection/`:

- `RuntimeType`, `RuntimeMethod/Constructor/Property/Field/Event/Parameter` - serializable wrappers around `System.Type`/members.
- `FakeReflection/` - editor-time stand-ins for types that may not exist yet (`FakeType`, `FakeMethod`, `FakeField`, `FakeProperty`, `FakeConstructor`, `ArrayFakeType`, `GenericFakeType`, `ReflectionFaker`).
- `Graph/` - runtime graph exposed as types: `RuntimeGraphType`, `RuntimeGraphMethod/Property/Field/Event/Constructor`, `RuntimeGraphInterface`, `RuntimePartialGraphType`, `PartialGraphMembers`, `PartialGraphMerge`, `RuntimeGraphExternalMember` (lets graphs-inherit-graphs).
- `Resolver/` - hardcoded fast paths for Unity APIs: `GetComponentResolver`, `GetComponentsInChildrenResolver`, `FindObjectOfTypeResolver`, `TryGetComponentResolver`, `GenericMethodResolver`, etc.
- `Optimization/` - `ReflectionOptimization` (cached/delegated member access for the reflection backend).
- `NativeMember/` - `RuntimeNativeType/Method/Property/Field/Constructor` (native C# generated members).
- `Core/Utility/ReflectionUtils.cs` (2733 lines) - the workhorse reflection helper; `Core/Utility/MemberDataUtility.cs` - MemberData resolution helpers.

### 3.5 Code generation (`Core/CodeGenerator/`)

Static partial class **`CG`** (`MaxyGames` namespace) drives everything. Files:

- `CodeGenerator.cs` - orchestrator: `CG.Generate(GeneratorSetting)` handles settings, culture, async queue, per-class pass; `CodeGeneratorClasses.cs` - emits class/struct/member skeletons; `CodeGeneratorNode.cs`, `CodeGeneratorValue.cs`, `CodeGeneratorString.cs`, `CodeGeneratorProperty.cs` - per-node/expression generation; `CodeGeneratorInit.cs` - generation init + `CG.Nodes` static; `CodeGeneratorDebugCode.cs` - `CG.debugScript` instrumentation; `CodeGeneratorCaching.cs`; `CodeGeneratorUtility.cs` - `CG.Utility` helpers; `GeneratorExtensions.cs`.

CG API highlights (used by every built-in node):

- `CG.RegisterPort(port, generator)` / `CG.RegisterPort(port, Func<string>)`
- `CG.Value(port)` - C# expression for a value port; `CG.Flow(port)` - recursively emits connected flow code.
- `CG.If(cond, trueBlock[, falseBlock])`, `CG.FlowFinish(enter, ...)`, `CG.New(type, args...)`, `CG.SimplifiedLambda(expr)`, `CG.GetEvent(port)`.
- State-flow vs regular handling: `CG.IsRegularFlow(port)`, `CG.IsStateFlow(port)`, state machine conditions, `CG.SetStateInitialization(enter, ...)`.
- Generation environment: `CG.graph`, `CG.generatorData` (`GData`), `CG.debugScript`, `CG.RegisterPort` closure timing, `CG.IsStackOverflow(node)` guard.
- `GeneratedData`/`GeneratorSetting` - output + settings; `GeneratedScriptData` (`Core/Classes/GeneratedScriptData.cs`) - persisted generated script + Unity object refs on graph assets.

Local-variable/model data: `MessageData`, `FunctionData`, `MethodModifier`, `ClassModifier` etc. live in `Core/CodeGenerator/CodeGeneratorClasses.cs` and `Core/Classes/Classes.cs` (Modifier enums, `AccessModifier`, `Modifier`, `MethodModifier`, `ClassModifier`).

### 3.6 Runtime backend (`Core/Runtime/`)

- `RuntimeGraphUtility.cs` - entry points to execute graphs/targets.
- `EventIterator.cs` / `EventCoroutine.cs` - coroutine-based async flow.
- `RuntimeSMHelper.cs` - state machine runtime helper.
- `ClassModel/`: `ClassAsset` (ScriptableObject classes), `ClassComponent` (MonoBehaviour classes incl. `RuntimeBehaviour`), `ClassObject` (plain object classes, `ClassObjectModel`) - runtime class generation.
- `GraphSingleton`, `RuntimeInstancedGraph` (`Core/Graph/`), `StateMachine`/`State`/`Transition`/`StateGraphContainer` (`Core/StateMachines/`).
- Globals/events runtime: `UEvent`, `UEventListener`, `UGlobalEvent` + typed variants (`Core/Event/`).

### 3.7 Serialization

- Graph assets since v3.1.0 are serialized by **Unity** (not Odin) for speed/size/VCS friendliness: `SerializedGraph`, `NodeSerializedData`, `USerializedValueWrapper`, `UIDref`-like reference fixes via `TypeSerializer`/`SerializerUtility`.
- Odin Serializer remains for complex runtime data (bundled in `Plugins/Odin Serializer/`).
- `Core/Utility/SerializerUtility.cs` (707 lines) - Odin wrapper.

---

## 4. Editor Architecture

### 4.1 Windows & graph canvas

- `uNodeEditor` (`Core.Editor/Core/uNodeEditor.cs`, 1842 lines) - main editor window; `GraphEditor` (772), `GraphEditorData` (705), `GraphCanvasData`, `uNodePreference` (1091; all user preferences), `NodeBrowserManager` (632).
- UI Toolkit graph view lives in `Core.Editor/UIElementGraph/`:
  - `UIElementGraph.cs` (1982), `GraphPanel.cs` (1913) - graph host + panels.
  - `Views/UGraphView.cs` (2555) + `UGraphView.GUI.cs` (1286) + `UGraphView.Function.cs` - graph canvas, selection, zoom, rendering; `UNodeView.cs`, `PortView.cs`, `EdgeView.cs`, `EdgeConnector.cs`, `UEdgeDragHelper.cs`, `MinimapView.cs`, `NodeViews/*` (BaseNodeView, BlockNodeView, RegionNodeView, TransitionView, StateTransitionView...), `EdgeBubble.cs` (connection debug bubbles).
  - `Manipulators/` - SelectionDragger, GraphDragger, ElementResizer, LeftMouseClickable.
  - `Snapper/` - SnapStrategy variants (Grid, Spacing, Ports, Borders).
  - `Controls/` - per-type inline controls (MemberControl, ObjectControl, DefaultControl, IntegerControl, EnumControl, AnimationCurveControl).
- `ItemSelector` (`Core.Editor/ItemSelector/`) - the smart node/member search & create system (Manager 1634 lines, Searcher 1015, Function 831).
- Editor commands broken into classes (`Core.Editor/Command/`): `CompletionEvaluator.cs` (2590), `AutoCompleteWindow.cs`, and Command system in `Editor/Commands/` (NodeCommands, PortCommands, InputPortCommands, SelectionCommands, GraphCommands, SurroundCommands, PortConverter).
- Node views/drawers (`Editor/NodeViews/`, `Editor/NodeDrawers/`) - classic IMGUI views + drawers (MultipurposeNodeDrawer 755 lines).
- GUI layer (`Core.Editor/GUI/`): `uNodeGUI.cs` (1314), `uNodeGUIStyle.cs`, `UInspector.cs`, `PropertyDrawer/*`, `FieldControl/*` (GeneralControl, UnityControl, UNodeControl), `Hierarchy/GraphHierarchyTree.cs` (757) - the graph hierarchy panel.
- `Bind/UBind.cs` (424) - editor data binding between graph model and views.
- Windows: `GraphCreatorWindow` (928), `NodeCreatorWindow` (519), `ActionWindow` (306), `FieldsEditorWindow`, `TypeBuilderWindow` (395), `PreviewSourceWindow` (383), `ErrorCheckWindow`, `WelcomeWindow`, `AboutWindow`, `TooltipWindow`, `GraphInspectorWindow`, `AssetMissingTypeFixer`.
- `Window/GraphCreatorWindow.cs` + `Utility/uNodeEditorInitializer.cs` (1491) - one-time project setup / database hammering; `Utility/GraphConverter.cs` - graph type conversion; `Utility/GenerationUtility.cs` (1424) - whether to compile a graph.

### 4.2 Compiler stack

`Editor/CodeCompiler/`:

- `MaxyGames.CompilerRunner/RoslynCodeCompiler.cs` (508) - drives Roslyn compile of generated scripts, emits assemblies, fixes references.
- `MaxyGames.CodeCompiler/CodeCompiler.cs` (586) + `CodeCompilerILPPRunner.cs` - ILPP/IL2CPP integration for building.
- `Core.Editor/Compiler/RoslynUtility.cs` (1125) - Roslyn syntax/parse helpers; `RoslynExtensions.cs`.
- `ApiCompatLevel`/`CompilationMethod` settings in `uNodePreference`.

### 4.3 Pro editor (`Pro.Editor/`)

- `C# Parser/CSharpParser.cs` (4468) + `SyntaxHandler.cs` (2316) + `ParserGraphManipulator.cs` + `RoundTripTester.cs` + `CSharpParserWindow.cs` - compile C# source into graphs (round-trip verified).
- `SyntaxHighlighter/CSharpSyntaxHighlighter.cs` (707) + `ColorizerSyntaxWalker.cs` - realtime preview highlighting (maps generated code back to graph elements).
- `CallStack/CaptureCallStack.cs` (641) + `MonoDebugger/` - breakpoints & call stack on hit.
- `Script/NodeBrowserWindow.cs` (2366), `GlobalSearch.cs` (301), `GraphHierarchy.cs` (108), `RealtimePreviewSourceWindow.cs`, `uNodeProUtility.cs`.
- `Bookmark/` - bookmark windows/manager/commands; `TypePatcher/TypePatcher.cs`; `XMLDocGenerator/CSharpDocGenerator.cs`; `SyntaxAnalizer/SyntaxAnalizer.cs` (655).

---

## 5. Built-in Node Library (`Runtime/Script/Nodes/`)

- **Behavior Trees**: Sequence, Selector, RandomSequence/RandomSelector, ParallelControl, decorators (Repeater, Inverter, Succeeder, Failer, UntilSuccess, UntilFailure), StopFlowNode, StopAllNode.
- **Flows**: NodeIf is in Core; runtime: NodeSwitch, NodeTry, NodeNullCheck, FlowToggle, FlowOnce, FlowControl, NodeThrow, NodeYieldReturn.
- **Loops**: ForeachLoop, ForeachIndexed, ForNumberLoop, WhileLoop, DoWhileLoop, NodeUsing, NodeLock.
- **Collections**: Get/Set/Add/Remove/Insert/Count/First/Last/Contains ListItem nodes.
- **Value**: GetComponent, Exposed, MakeArray, Select, Condition, Conditional, IncrementDecrement, Negate, Shift, Bitwise, StringBuilder, Coalescing, Default, `Time/PerSecond`.
- **Yield**: NodeTimer, NodeWaitForSecond, NodeWaitUntil, NodeWaitWhile, NodePreprocessor.
- **Transitions** (state graphs): OnTimerElapsed, OnTrigger/Collision Enter/Stay/Exit (+2D), OnApplicationFocus/Pause, OnTransformChanged, ConditionTransition.
- Core event nodes additionally in `Core/Nodes/Event/` (Physics, Mouse, Renderer, Transform, Game Event, CSharpEventListener) and Integrations (InputSystem, UGUI) - all are `BaseEventNode` subclasses triggered by Unity messages or event subscriptions.

---

## 6. How To Use (Concrete)

### 6.1 Creation & iteration

1. **Create a graph**: `Assets > Create > uNode > Graph…` (or `uNode > Open uNode Editor`). Runtime, C# graph, class asset, component, macro, enum, interface, state graph variants exist.
2. **Author**: use the space bar / node search (`ItemSelector`) to add nodes; drag from value/flow ports to connect; use the inspector panel (IMGUI `UInspector` + UI Toolkit sidebars) to edit fields; `Ctrl+click` port for single-click connect (v3.2+); sticky notes/regions/comments for organization; `uNode3Data` and per-graph variable/property/function containers for members.
3. **Watch it run**: enter play mode. Runtime graphs run in reflection mode by default in the editor (live editable). Breakpoints (Pro) pause into the Call Stack window; connection bubbles show data flow (Pro).
4. **Generate C#**: `uNode > Compile` (or preference auto-refresh). The C# preview window shows the exact generated script; `PreviewSourceWindow`/`RealtimePreviewSourceWindow` support selection highlighting.

### 6.2 GraphComponent at runtime

- Add `GraphComponent` to a GameObject, assign a `RuntimeGraph`-style asset. Prefab instances override variables via `variableOverrides` instead of editing the shared graph.
- Instance-level access (from generated/type-safe or `uNodeHelper.RuntimeUtility`):
  - `uNodeHelper.RuntimeUtility.GetVariable(instance, name)`, `SetVariable(instance, name, value[, op])`
  - `GetProperty/SetProperty`, `InvokeFunction(instance, name, args[])`
  - `RuntimeUtility.InitializeVariables(target, graph, variables)`
- Generated C# graph assets expose strongly-typed members; runtime graphs accessed through `GraphInstance`/`Flow` APIs (`RuntimeGraphUtility`).

### 6.3 Graph types & execution modes

- **Runtime Graph**: asset; editor executes via reflection; builds compile to native C# automatically.
- **C# Graph**: produces a `.cs` file (and generated member stubs); use for advanced/API-heavy work; partial support as of v3.2.11.
- **Macro**: reusable sub-graph with parameters (`MacroNode`, `LinkedMacroNode`, `MacroPortNode` under `Core/Macro/`).
- **State/Event Graph**: `StateMachine`, `State`, `StateTransition`, `AnyStateNode`, `StateEntryNode`, `TriggerStateTransition`, `NestedStateNode`, `ScriptState`, `GetStateMachineNode`; update type configurable (Update/Fixed/Late/Manual via `StateGraphContainer`).
- **Class Asset / Component / Object**: runtime-defined classes; `ClassComponent` integrates with MonoBehaviour lifecycle; `GraphComponent` runs them.
- **Graph inheritance**: `RuntimePartialGraphType`, `PartialGraphMembers`, `PartialGraphMerge` let a graph extend another (methods/fields/properties/events).
- **Global events**: `UGlobalEvent` + typed variants (`UGlobalEventBool/Int/Float/String/Action/...`), `UEventListener`, `CSharpEventListener`, `GlobalEventListenerEvent`; trigger via `NodeTriggerGlobalEvent`.

---

## 7. Extending uNode (Agent Guide)

### 7.1 Authoring a custom node (both backends)

```csharp
using MaxyGames.UNode;

[NodeMenu("Custom", "My Node", scope: NodeScope.FlowGraph)]  // category, name, optional scope
public class MyNode : FlowNode {
    public ValueInput input;    // public fields get discovered/bound by the framework

    protected override void OnRegister() {
        input = ValueInput(nameof(input), typeof(int));
        exit  = FlowOutput(nameof(exit));
        base.OnRegister();      // registers `enter` flow and connects it via OnGeneratorInitialize default
    }

    // Reflection/runtime mode -------------------------------------------------
    protected override void OnExecuted(Flow flow) {
        int value = input.GetValue<int>(flow);
        Debug.Log(value * 2);
        flow.Next(exit);                       // continue to next node
        flow.state = StateType.Success;        // optional state for state graphs / BT
    }

    // Native C# mode ----------------------------------------------------------
    public override void OnGeneratorInitialize() {
        base.OnGeneratorInitialize(); // registers enter -> GenerateFlowCode() and any primary output
        // override code by calling CG.RegisterPort(...) AFTER base for custom ports
    }
}
```

Key `CG` calls for codegen in custom nodes: `CG.Value(input)` emits the input expression; `CG.Flow(exit)` emits the connected code; `CG.If(cond, then[, els])`; `CG.FlowFinish(...)`.

When NOT overriding `OnGeneratorInitialize`, `GenerateFlowCode()` is auto-registered for `enter`:

```csharp
protected override string GenerateFlowCode() {
    return // C# statements for this flow node, ending with CG.Flow(exit)-style continuation
}
```

### 7.2 Extending behavior & editor

- **New node scope**: use `NodeScope` constants (`All, GameObject, Coroutine, Macro, StateGraph, FlowGraph, BTGraph, State, Function, ECS*`; combinable via `NodeScope.Scope(a, b)`, exclusion via `"!Scope"`).
- **Custom drawer/control**: add `Core.Editor/GUI/PropertyDrawer/...` or `FieldControl/...` implementations; node views in `Editor/NodeViews/`.
- **Auto-supported members**: any C# type is dynamically representable through `MemberData` + `MultipurposeNode` - no node needed for calling methods/fields/properties/events.
- **Custom attributes**: `FilterAttribute` (port filtering/search), `NodeMenu`, `Description`, `GraphSystemAttribute` (mark a graph system), `DisplayKind`.
- **Integrations**: mirror `Integrations/InputSystem` & `UGUI` (separate asmdef, event nodes, conditional `defineConstraints`).
- **Pro features**: extend `Pro.Editor/Script/`, `SyntaxHighlighter`/`SyntaxAnalizer`, `CallStack`, bookmarks, `TypePatcher`, `C# Parser` handler registration.

---

## 8. Performance, Compatibility & Gotchas

- **Native C# vs Reflection**: native = generated plain C#, near-handwritten speed; reflection = live-editable, slower. Editor defaults to reflection for runtime graphs unless compiled; builds use native.
- **IL2CPP/build**: `link.xml` handling, `Unity.CodeCompiler.CodeGen` ILPP runner, AOT serialization via Odin; uNode trims graph data for WebGL/mobile (Pro).
- **Serialization changes**: v3.1.0 moved to Unity-serialized graphs (smaller, VCS-friendly, but breaking - backup before upgrading).
- **API level**: requires .NET 4.x (Odin dependency). Unity 2022.3+.
- **Editor/playmode**: supports Fast Enter Play Mode; `uNodeDomainReloader`/`uNodeThreadUtility` handle async compile queues; avoid calling `CG.Generate` re-entrantly (it awaits other generations first).
- **Common failure modes for agents**: graph assets referencing deleted scripts (use `AssetMissingTypeFixer` window); regenerated code conflicts after editing generated `.cs` (edit the graph, not the output); macro linked nodes (`LinkedMacroNode`) must match macro signatures.

---

## 9. Key Files Index (quick map)

| Concern | Files |
| --- | --- |
| Graph model | `Core/Graph/Graph.cs`, `Classes.cs`, `GraphInstance.cs`, `GraphComponent.cs`, `GraphElement/{Function,Property,Variable,BaseFunction,Constructor}.cs` |
| Nodes | `Core/Nodes/Base/{Node,NodeObject,BaseEventNode}.cs`, `Core/Nodes/HLNode.cs`, `MultipurposeNode.cs`, `NodeAction.cs`, flow/data nodes in `Core/Nodes/{Flow,Data}/` |
| Ports | `Core/Ports/{UPort,Flow,ValueInput,ValueOutput,FlowInput,FlowOutput,Connection}.cs` |
| Reflection | `Core/Reflection/{RuntimeType,RuntimeMethod,RuntimeProperty,RuntimeField,RuntimeConstructor,RuntimeEvent,RuntimeParameter}.cs`, `FakeReflection/*`, `Resolver/*`, `Graph/*`, `NativeMember/*` |
| Codegen | `Core/CodeGenerator/CodeGenerator*.cs` (the `CG` class) |
| Runtime | `Core/Runtime/{RuntimeGraphUtility,EventIterator,EventCoroutine}.cs`, `Core/StateMachines/*`, `Core/Event/*` |
| Editor core | `Core.Editor/Core/{uNodeEditor,uNodePreference}.cs`, `Core.Editor/UIElementGraph/**` |
| Search | `Core.Editor/ItemSelector/**`, `Editor/Utility/CreateNodeProcessor.cs` |
| Compiler | `Editor/CodeCompiler/**`, `Core.Editor/Compiler/{RoslynUtility,RoslynExtensions}.cs` |
| Built-in nodes | `Runtime/Script/Nodes/**` |
| Pro | `Pro.Editor/**` |
| Docs/changelog | `README.md`, `Assets/uNode3/changelog.md` |

---

## 10. Reference Material

- Official docs: https://docs.maxygames.com/unode3/index.html
- Repo: https://github.com/wahidrachmawan/uNode · Roadmap · Issues · Discord (https://discord.gg/8ufevvN)
- Pro: https://maxygames.com/download/ · DOTS: github.com/wahidrachmawan/uNode3-DOTS

*Document generated from source analysis of this checkout (uNode Pro, v3.2.11). Line counts and file names reflect the repo at the time of writing.*
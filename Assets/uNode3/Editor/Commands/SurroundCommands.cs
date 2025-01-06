using System.Collections;
using System.Collections.Generic;
using MaxyGames.UNode.Nodes;
using UnityEngine;
namespace MaxyGames.UNode.Editors.Commands
{
    public static class SurroundCommands
    {
        public abstract class SurroundCommand
        {
            protected UGraphElement parent;
            public SurroundCommand(UGraphElement parent)
            {
                this.parent = parent;
            }
            public abstract Node SurroundUnit { get; }

            public abstract FlowOutput surroundSource { get; }

            public abstract FlowOutput surroundExit { get; }

            public abstract FlowInput unitEnterPort { get; }

            public abstract string DisplayName { get; }
        }

        public class ForSurround : SurroundCommand
        {
            private ForNumberLoop @for;

            public ForSurround(UGraphElement parent) : base(parent)
            {
                @for = new ForNumberLoop();
            }

            public override Node SurroundUnit
            {
                get
                {
                    return @for;
                }
            }

            public override FlowOutput surroundSource => @for.body;

            public override FlowOutput surroundExit => @for.exit;

            public override FlowInput unitEnterPort => @for.enter;

            public override string DisplayName => "For";
        }

        public class ForEachSurround : SurroundCommand
        {
            private ForeachLoop @for;

            public ForEachSurround(UGraphElement parent) : base(parent)
            {
                @for = new ForeachLoop();
            }

            public override Node SurroundUnit
            {
                get
                {
                    return @for;
                }
            }

            public override FlowOutput surroundSource => @for.body;

            public override FlowOutput surroundExit => @for.exit;

            public override FlowInput unitEnterPort => @for.enter;

            public override string DisplayName => "ForEach";
        }

        public class IfTrueSurround : SurroundCommand
        {
            private NodeIf @if;

            public IfTrueSurround(UGraphElement parent) : base(parent)
            {
                @if = new NodeIf();
            }

            public override Node SurroundUnit
            {
                get
                {
                    return @if;
                }
            }

            public override FlowOutput surroundSource => @if.onTrue;

            public override FlowOutput surroundExit => @if.exit;

            public override FlowInput unitEnterPort => @if.enter;

            public override string DisplayName => "If (true)";
        }

        public class IfFalseSurround : SurroundCommand
        {
            private NodeIf @if;

            public IfFalseSurround(UGraphElement parent) : base(parent)
            {
                @if = new NodeIf();
            }

            public override Node SurroundUnit
            {
                get
                {
                    return @if;
                }
            }

            public override FlowOutput surroundSource => @if.onFalse;

            public override FlowOutput surroundExit => @if.exit;

            public override FlowInput unitEnterPort => @if.enter;

            public override string DisplayName => "If (false)";
        }

        public class DoWhileSurround : SurroundCommand
        {
            private DoWhileLoop doWhile;

            public DoWhileSurround(UGraphElement parent) : base(parent)
            {
                doWhile = new DoWhileLoop();
            }

            public override Node SurroundUnit
            {
                get
                {
                    return doWhile;
                }
            }

            public override FlowOutput surroundSource => doWhile.body;

            public override FlowOutput surroundExit => doWhile.exit;

            public override FlowInput unitEnterPort => doWhile.enter;

            public override string DisplayName => "Do While";
        }

        public class WhileSurround : SurroundCommand
        {
            private WhileLoop @while;

            public WhileSurround(UGraphElement parent) : base(parent)
            {
                @while = new WhileLoop();
            }

            public override Node SurroundUnit
            {
                get
                {
                    return @while;
                }
            }

            public override FlowOutput surroundSource => @while.body;

            public override FlowOutput surroundExit => @while.exit;

            public override FlowInput unitEnterPort => @while.enter;

            public override string DisplayName => "While";
        }

        public class TryFinallySurround : SurroundCommand
        {
            private NodeTry @try;

            public TryFinallySurround(UGraphElement parent) : base(parent)
            {
                @try = new NodeTry();
            }

            public override Node SurroundUnit
            {
                get
                {
                    return @try;
                }
            }

            public override FlowOutput surroundSource => @try.Try;

            public override FlowOutput surroundExit => @try.exit;

            public override FlowInput unitEnterPort => @try.enter;

            public override string DisplayName => "Try-Catch-Finally";
        }
    }
}
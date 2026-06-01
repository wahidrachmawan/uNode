using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;

namespace MaxyGames.UNode.Editors {
	public struct EdgeData {
		public readonly Connection connection;
		public readonly PortView input;
		public readonly PortView output;

		public EdgeData(Connection connection, PortView input, PortView output) {
			this.connection = connection;
			this.input = input;
			this.output = output;
		}
	}

	public abstract class PortData {
		public UNodeView owner;
		public PortView portView;
		public FilterAttribute filter;
		public object userData;

		public abstract UPort portValue { get; }
		public virtual string portID => portValue.id;
		public virtual string name => portValue.name;
		public virtual string title => portValue.title;
		public virtual string tooltip => portType?.PrettyName(true) ?? string.Empty;
		public virtual object defaultValue => null;
		public abstract Type portType { get; }
		public abstract bool isFlow { get; }
		public abstract bool IsInput { get; }
		public bool IsOutput => IsInput == false;
		public virtual bool IsConnected => portValue.hasValidConnections;

		public abstract void ConnectTo(UPort port);

		private FilterAttribute cachedFilter;
		/// <summary>
		/// Get the filter of this port or create new if none.
		/// </summary>
		/// <returns></returns>
		public FilterAttribute GetFilter() {
			if(filter == null) {
				if(cachedFilter == null) {
					Type t = portType;
					if(t != null) {
						if(t.IsByRef) {
							t = t.GetElementType();
							cachedFilter = new FilterAttribute(t) {
								SetMember = true
							};
						}
						else {
							cachedFilter = new FilterAttribute(t);
						}
					} else {
						cachedFilter = new FilterAttribute(typeof(object));
					}
				}
				return cachedFilter;
			}
			return filter;
		}

		public virtual bool IsValidReference(UPort port) => port != null && port == portValue;
	}

	public abstract class PortData<T> : PortData where T : UPort {
		public T port;

		public override UPort portValue => port;

		public PortData(T port) {
			this.port = port;
		}
	}

	public class ValueInputData : PortData<ValueInput> {
		public ValueInputData(ValueInput port) : base(port) {
			filter = port.filter;
		}

		public override Type portType => port.type ?? typeof(object);

		public override bool isFlow => false;
		public override string tooltip {
			get {
				var filter = GetFilter();
				if(filter.Types?.Count > 1) {
					return filter.Tooltip;
				}
				return portType.PrettyName(true);
			}
		}

		public override object defaultValue => port.DefaultValue;

		public override bool IsInput => true;

		public override void ConnectTo(UPort port) {
			if(port is ValueOutput p) {
				ValueConnection.CreateAndConnect(this.port, p);
			} else {
				throw new ArgumentException("Invalid port type", nameof(port));
			}
		}

		public UIControl.MemberControl InstantiateControl(bool autoLayout = false) {
			ControlConfig config = new ControlConfig() {
				owner = owner,
				value = port.DefaultValue,
				type = portType,
				filter = GetFilter(),
				portReference = port,
				onValueChanged = (val) => port.DefaultValue = val as MemberData,
			};
			return new UIControl.MemberControl(config, autoLayout);
		}
	}

	public class ValueOutputData : PortData<ValueOutput> {
		public ValueOutputData(ValueOutput port) : base(port) {
		}

		public override Type portType => port.type ?? typeof(object);
		public override bool isFlow => false;

		public override bool IsInput => false;

		public override void ConnectTo(UPort port) {
			if(port is ValueInput p) {
				ValueConnection.CreateAndConnect(p, this.port);
			} else {
				throw new ArgumentException("Invalid port type", nameof(port));
			}
		}
	}

	public class FlowInputData : PortData<FlowInput> {
		public FlowInputData(FlowInput port) : base(port) {
		}

		public override Type portType => typeof(void);
		public override bool isFlow => true;
		public override string tooltip => "Flow";

		public override bool IsInput => true;

		public override void ConnectTo(UPort port) {
			if(port is FlowOutput p) {
				FlowConnection.CreateAndConnect(this.port, p);
			} else {
				throw new ArgumentException("Invalid port type", nameof(port));
			}
		}
	}

	public class MultiFlowOutputData : PortData<MultiFlowOutput> {
		public MultiFlowOutputData(MultiFlowOutput port) : base(port) { }

		public override Type portType => typeof(void);
		public override bool isFlow => true;

		public override bool IsInput => false;

		public override bool IsValidReference(UPort port) {
			if(port is FlowOutput) {
				return this.port.GetFlows().Any(p => p == port);
			}
			return base.IsValidReference(port);
		}

		public override void ConnectTo(UPort port) {
			if(port is FlowInput) {
				this.port.ConnectTo(port);
			}
			else {
				throw new ArgumentException("Invalid port type", nameof(port));
			}
		}
	}

	public class MultiFlowOutput : FlowPort, IMultiConnectionPort {
		[NonSerialized]
		private Func<IEnumerable<FlowOutput>> getFlows;
		[NonSerialized]
		private Func<FlowInput, Connection> newConnection;

		public MultiFlowOutput(NodeObject node, Func<IEnumerable<FlowOutput>> getFlows, Func<FlowInput, Connection> newConnection) : base(node) {
			this.getFlows = getFlows;
			this.newConnection = newConnection;
		}

		public IEnumerable<FlowOutput> GetFlows() => getFlows();

		public Connection ConnectTo(UPort other) {
			if(other is not FlowInput) throw null;
			var existingPort = getFlows().FirstOrDefault(p => p.GetTargetFlow() == other);
			if(existingPort != null) {
				existingPort.ClearConnections();
			}
			return newConnection(other as FlowInput);
		}

		public override IEnumerable<Connection> Connections {
			get {
				foreach(var port in GetFlows()) {
					foreach(var con in port.Connections) {
						yield return con;
					}
				}
			}
		}
	}

	public class FlowOutputData : PortData<FlowOutput> {
		public FlowOutputData(FlowOutput port) : base(port) { }

		public override Type portType => typeof(void);
		public override bool isFlow => true;
		public override string tooltip => "Flow";

		public override bool IsInput => false;

		public override void ConnectTo(UPort port) {
			if(port is FlowInput p) {
				FlowConnection.CreateAndConnect(p, this.port);
			} else {
				throw new ArgumentException("Invalid port type", nameof(port));
			}
		}
	}
}
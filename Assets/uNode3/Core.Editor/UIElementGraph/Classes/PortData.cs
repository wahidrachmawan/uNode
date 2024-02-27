using System;
using System.Collections.Generic;
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
		public virtual string tooltip => portType?.PrettyName(true) ?? string.Empty;
		public virtual object defaultValue => null;
		public abstract Type portType { get; }
		public abstract bool isFlow { get; }

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
						cachedFilter = new FilterAttribute(t);
					} else {
						cachedFilter = new FilterAttribute(typeof(object));
					}
				}
				return cachedFilter;
			}
			return filter;
		}
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

		public override object defaultValue => port.defaultValue;

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
				value = port.defaultValue,
				type = portType,
				filter = GetFilter(),
				onValueChanged = (val) => port.defaultValue = val as MemberData,
			};
			return new UIControl.MemberControl(config, autoLayout);
		}
	}

	public class ValueOutputData : PortData<ValueOutput> {
		public ValueOutputData(ValueOutput port) : base(port) {
		}

		public override Type portType => port.type ?? typeof(object);
		public override bool isFlow => false;

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

		public override void ConnectTo(UPort port) {
			if(port is FlowOutput p) {
				FlowConnection.CreateAndConnect(this.port, p);
			} else {
				throw new ArgumentException("Invalid port type", nameof(port));
			}
		}
	}

	public class FlowOutputData : PortData<FlowOutput> {
		public FlowOutputData(FlowOutput port) : base(port) { }

		public override Type portType => typeof(void);
		public override bool isFlow => true;
		public override string tooltip => "Flow";

		public override void ConnectTo(UPort port) {
			if(port is FlowInput p) {
				FlowConnection.CreateAndConnect(p, this.port);
			} else {
				throw new ArgumentException("Invalid port type", nameof(port));
			}
		}
	}
}
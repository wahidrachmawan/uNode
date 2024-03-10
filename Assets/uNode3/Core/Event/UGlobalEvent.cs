using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;

namespace MaxyGames.UNode {
	/// <summary>
	/// Base class for all global event.
	/// </summary>
	public abstract class UGlobalEvent : ScriptableObject, IRefreshable {
		private string m_name;
		public virtual string EventName {
			get {
				if(uNodeUtility.IsInMainThread) {
					return m_name = name;
				}
				else {
					return m_name;
				}
			}
		}

		public void TriggerWeak(object[] parameters) {
			(this as IGlobalEvent).Trigger(parameters);
		}

		private void OnValidate() {
			if(this is not IGlobalEvent) {
				Debug.LogError("Event must implement IGlobalEvent");
			}
		}

		public virtual CG.MData GenerateMethodCode(out string[] parameterNames, out string actionCode) {
			var evt = this as IGlobalEvent;
			var count = evt.ParameterCount;
			var parameters = new Type[count];
			for(int i = 0; i < count; i++) {
				parameters[i] = evt.GetParameterType(i);
			}
			var mData = CG.generatorData.AddNewGeneratedMethod(CG.GenerateNewName(EventName), typeof(void), parameters);
			var names = new string[count];
			for(int i = 0; i < count; i++) {
				names[i] = mData.parameters[i].name;
			}
			parameterNames = names;
			actionCode = CG.NewGeneric(typeof(Action), parameters.Select(item => CG.Type(item)), new string[] { mData.name }, null);
			return mData;
		}

		public abstract void ClearListener();

		public virtual void Refresh() {
			if(!Application.isPlaying) {
				ClearListener();
			}
		}
	}

	public interface IGlobalEvent {
		public int ParameterCount { get; }
		public Type GetParameterType(int index);
		public string GetParameterName(int index);
		public void AddListener(Delegate @delegate);
		public void RemoveListener(Delegate @delegate);
		public void Trigger(object[] parameters);
	}

	public interface IGlobalEventNoParameter : IGlobalEvent {
		int IGlobalEvent.ParameterCount => 0;
		Type IGlobalEvent.GetParameterType(int index) => throw new IndexOutOfRangeException();
		string IGlobalEvent.GetParameterName(int index) => throw new IndexOutOfRangeException();

		void IGlobalEvent.AddListener(Delegate @delegate) {
			if(@delegate is Action) {
				AddListener(@delegate as Action);
			}
			else {
				throw new Exception("Delegate type is not valid.\nDelegate type: " + @delegate.GetType());
			}
		}

		void IGlobalEvent.RemoveListener(Delegate @delegate) {
			if(@delegate is Action) {
				RemoveListener(@delegate as Action);
			}
			else {
				throw new Exception("Delegate type is not valid.\nDelegate type: " + @delegate.GetType());
			}
		}

		void IGlobalEvent.Trigger(object[] parameters) {
			if(parameters != null && parameters.Length > 0)
				throw new InvalidOperationException("Invalid given parameters");
			Trigger();
		}

		public void AddListener(Action action);
		public void RemoveListener(Action action);

		public void Trigger();
	}

	public interface IGlobalEvent<T> : IGlobalEvent {
		int IGlobalEvent.ParameterCount => 1;
		Type IGlobalEvent.GetParameterType(int index) => typeof(T);
		string IGlobalEvent.GetParameterName(int index) => "value";

		void IGlobalEvent.AddListener(Delegate @delegate) {
			if(@delegate is Action<T>) {
				AddListener(@delegate as Action<T>);
			}
			else {
				throw new Exception("Delegate type is not valid.\nDelegate type: " + @delegate.GetType());
			}
		}

		void IGlobalEvent.RemoveListener(Delegate @delegate) {
			if(@delegate is Action<T>) {
				RemoveListener(@delegate as Action<T>);
			}
			else {
				throw new Exception("Delegate type is not valid.\nDelegate type: " + @delegate.GetType());
			}
		}

		void IGlobalEvent.Trigger(object[] parameters) {
			if(parameters == null)
				throw new ArgumentNullException(nameof(parameters));
			if(parameters.Length != 1)
				throw new InvalidOperationException("Invalid given parameters");
			Trigger((T)parameters[0]);
		}

		public void AddListener(Action<T> action);
		public void RemoveListener(Action<T> action);

		public void Trigger(T value);
	}

	public interface IGlobalEvent<T1, T2> : IGlobalEvent {
		int IGlobalEvent.ParameterCount => 2;
		Type IGlobalEvent.GetParameterType(int index) => index switch {
			0 => typeof(T1),
			1 => typeof(T2),
			_ => throw new IndexOutOfRangeException(),
		};
		string IGlobalEvent.GetParameterName(int index) => "value" + (index + 1);

		void IGlobalEvent.AddListener(Delegate @delegate) {
			if(@delegate is Action<T1, T2>) {
				AddListener(@delegate as Action<T1, T2>);
			}
			else {
				throw new Exception("Delegate type is not valid.\nDelegate type: " + @delegate.GetType());
			}
		}

		void IGlobalEvent.RemoveListener(Delegate @delegate) {
			if(@delegate is Action<T1, T2>) {
				RemoveListener(@delegate as Action<T1, T2>);
			}
			else {
				throw new Exception("Delegate type is not valid.\nDelegate type: " + @delegate.GetType());
			}
		}

		void IGlobalEvent.Trigger(object[] parameters) {
			if(parameters == null)
				throw new ArgumentNullException(nameof(parameters));
			if(parameters.Length != 2)
				throw new InvalidOperationException("Invalid given parameters");
			Trigger((T1)parameters[0], (T2)parameters[1]);
		}

		public void AddListener(Action<T1, T2> action);
		public void RemoveListener(Action<T1, T2> action);

		public void Trigger(T1 value1, T2 value2);
	}

	public interface IGlobalEvent<T1, T2, T3> : IGlobalEvent {
		int IGlobalEvent.ParameterCount => 2;
		Type IGlobalEvent.GetParameterType(int index) => index switch {
			0 => typeof(T1),
			1 => typeof(T2),
			_ => throw new IndexOutOfRangeException(),
		};
		string IGlobalEvent.GetParameterName(int index) => "value" + (index + 1);

		void IGlobalEvent.AddListener(Delegate @delegate) {
			if(@delegate is Action<T1, T2, T3>) {
				AddListener(@delegate as Action<T1, T2, T3>);
			}
			else {
				throw new Exception("Delegate type is not valid.\nDelegate type: " + @delegate.GetType());
			}
		}

		void IGlobalEvent.RemoveListener(Delegate @delegate) {
			if(@delegate is Action<T1, T2>) {
				RemoveListener(@delegate as Action<T1, T2, T3>);
			}
			else {
				throw new Exception("Delegate type is not valid.\nDelegate type: " + @delegate.GetType());
			}
		}

		void IGlobalEvent.Trigger(object[] parameters) {
			if(parameters == null)
				throw new ArgumentNullException(nameof(parameters));
			if(parameters.Length != 3)
				throw new InvalidOperationException("Invalid given parameters");
			Trigger((T1)parameters[0], (T2)parameters[1], (T3)parameters[2]);
		}

		public void AddListener(Action<T1, T2, T3> action);
		public void RemoveListener(Action<T1, T2, T3> action);

		public void Trigger(T1 value1, T2 value2, T3 value3);
	}
}
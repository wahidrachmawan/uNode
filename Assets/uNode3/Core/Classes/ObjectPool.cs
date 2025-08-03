using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace MaxyGames.UNode {
	/// <summary>
	/// Generic implementation of object pooling pattern with predefined pool size limit. The main
	/// purpose is that limited number of frequently used objects can be kept in the pool for
	/// further recycling.
	///
	/// Notes: 
	/// 1) it is not the goal to keep all returned objects. Pool is not meant for storage. If there
	///    is no space in the pool, extra returned objects will be dropped.
	///
	/// 2) it is implied that if object was obtained from a pool, the caller will return it back in
	///    a relatively short time. Keeping checked out objects for long durations is ok, but 
	///    reduces usefulness of pooling. Just new up your own.
	///
	/// Not returning objects to the pool in not detrimental to the pool's work, but is a bad practice. 
	/// Rationale: 
	///    If there is no intent for reusing the object, do not use pool - just use "new". 
	/// </summary>
	public class ObjectPool<T> where T : class {
		private struct Element {
			internal T Value;
		}

		private T _firstItem;

		private readonly Element[] _items;

		private readonly Func<T> _factory;
		private readonly Action<T> _onAllocate, _onFree;

		public ObjectPool(Func<T> factory) : this(factory, Environment.ProcessorCount * 2) {
		}

		public ObjectPool(Func<T> factory, int size) {
			_factory = factory;
			_items = new Element[size - 1];
		}

		public ObjectPool(Func<T> factory, Action<T> onAllocate, Action<T> onFree) : this(factory, onAllocate, onFree, Environment.ProcessorCount * 2) {
		}

		public ObjectPool(Func<T> factory, Action<T> onAllocate, Action<T> onFree, int size) {
			_factory = factory;
			_onAllocate = onAllocate;
			_onFree = onFree;
			_items = new Element[size - 1];
		}

		public ObjectPool(Func<ObjectPool<T>, T> factory, int size) {
			Func<ObjectPool<T>, T> factory2 = factory;
			ObjectPool<T> arg = this;
			_factory = () => factory2(arg);
			_items = new Element[size - 1];
		}

		private T CreateInstance() {
			return _factory();
		}

		/// <summary>
		/// Produces an instance.
		/// </summary>
		/// <remarks>
		/// Search strategy is a simple linear probing which is chosen for it cache-friendliness.
		/// Note that Free will try to store recycled objects close to the start thus statistically 
		/// reducing how far we will typically search.
		/// </remarks>
		public T Allocate() {
			T inst = _firstItem;
			if(inst == null || inst != Interlocked.CompareExchange(ref _firstItem, null, inst)) {
				inst = AllocateSlow();
			}
			_onAllocate?.Invoke(inst);
			return inst;
		}

		private T AllocateSlow() {
			Element[] items = _items;
			for(int i = 0; i < items.Length; i++) {
				T inst = items[i].Value;
				if(inst != null && inst == Interlocked.CompareExchange(ref items[i].Value, null, inst)) {
					return inst;
				}
			}
			return CreateInstance();
		}

		/// <summary>
		/// Returns objects to the pool.
		/// </summary>
		/// <remarks>
		/// Search strategy is a simple linear probing which is chosen for it cache-friendliness.
		/// Note that Free will try to store recycled objects close to the start thus statistically 
		/// reducing how far we will typically search in Allocate.
		/// </remarks>
		public void Free(T obj) {
			_onFree?.Invoke(obj);
			if(_firstItem == null) {
				_firstItem = obj;
			}
			else {
				FreeSlow(obj);
			}
		}

		private void FreeSlow(T obj) {
			Element[] items = _items;
			for(int i = 0; i < items.Length; i++) {
				if(items[i].Value == null) {
					items[i].Value = obj;
					break;
				}
			}
		}
	}

	public static class StaticListPool<T> {
		public readonly static ObjectPool<List<T>> pool = new ObjectPool<List<T>>(Construct, null, OnFree);

		public static List<T> Allocate() {
			return pool.Allocate();
		}

		public static void Free(List<T> obj) {
			pool.Free(obj);
		}

		static List<T> Construct() {
			return new List<T>();
		}

		static void OnFree(List<T> value) {
			value.Clear();
		}
	}

	public static class StaticHashPool<T> {
		public readonly static ObjectPool<HashSet<T>> pool = new ObjectPool<HashSet<T>>(Construct, null, OnFree);

		public static HashSet<T> Allocate() {
			return pool.Allocate();
		}

		public static void Free(HashSet<T> obj) {
			pool.Free(obj);
		}

		static HashSet<T> Construct() {
			return new HashSet<T>();
		}

		static void OnFree(HashSet<T> value) {
			value.Clear();
		}
	}
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Kernel.ServiceLocator
{
	public class Locator
	{
		private static Locator _instance;

		private Dictionary<Type, Type> _singletons = new Dictionary<Type, Type>();
		private Dictionary<Type, Tuple<object, bool>> _singletonInstances = new Dictionary<Type, Tuple<object, bool>>();


		public static IEnumerable<KeyValuePair<Type, Type>> Singletons
		{
			get { return _instance._singletons; }
		}

		public static IEnumerable<KeyValuePair<Type, Tuple<object, bool>>> SingletonInstances
		{
			get { return _instance._singletonInstances; }
		}


		static Locator()
		{
			_instance = new Locator();
		}

		protected Locator()
		{
		}

		public static void Reset()
		{
			var disposables = new List<Type>();
			foreach (var kvp in _instance._singletonInstances)
			{
				var service = kvp.Value.Item1 as IService;
				if (service != null) service.Reset();

				var disposableService = kvp.Value.Item1 as IDisposableService;
				if (disposableService is IDisposableService)
				{
					disposableService.Destroy();
					disposables.Add(kvp.Key);
				}
			}

			foreach (var service in disposables)
			{
				_instance._singletonInstances.Remove(service);
				_instance._singletons.Remove(service);
			}
		}

		public static void Destroy()
		{
			foreach (var kvp in _instance._singletonInstances)
			{
				IService service = kvp.Value as IService;
				if (service != null) service.Destroy();
			}

			_instance._singletons.Clear();
			_instance._singletonInstances.Clear();
		}

		public static bool IsSingletonRegistered<TConcrete>()
		{
			return IsSingletonRegistered(typeof(TConcrete));
		}

		public static bool IsSingletonRegistered(Type concreteType)
		{
			return _instance._singletons.ContainsKey(concreteType);
		}

		public static void RegisterSingletons(params Assembly[] assemblies)
		{
			foreach (Assembly assembly in assemblies)
			{
				foreach (Type type in assembly.GetTypes())
				{
					var attrs = type.GetCustomAttributes(typeof(RegisterSingletonAttribute), false);
					foreach (var attr in attrs)
					{
						var abstractType = (attr as RegisterSingletonAttribute).AbstractType;
						RegisterSingleton(abstractType, type);
					}
				}
			}
		}

		public static void InjectStatic(params Assembly[] assemblies)
		{
			var fields = FindStaticFields(assemblies);
			InjectStaticFields(fields);
		}

		public static void InjectStaticFields(IEnumerable<FieldInfo> fields)
		{
			foreach (var field in fields)
			{
				if (_instance._singletons.ContainsKey(field.FieldType))
				{
					var val = Locator.Resolve(field.FieldType, false);
					field.SetValue(null, val);
				}
			}
		}

		public static IEnumerable<FieldInfo> FindStaticFields(params Assembly[] assemblies)
		{
			var result = new List<FieldInfo>();

			foreach (var assembly in assemblies)
			{
				foreach (var type in assembly.GetTypes())
				{
					// if (type.GetCustomAttributes(typeof(StaticInjectAttribute), true).Length > 0)
					// {
					var fields = type
						.GetFields(BindingFlags.Static | BindingFlags.NonPublic)
						.Where(x => x.IsDefined(typeof(StaticInjectAttribute), false));

					result.AddRange(fields);
					// }
				}
			}

			return result;
		}

		public static void ExecuteServices()
		{
			foreach (var kvp in _instance._singletons)
			{
				Locator.Resolve(kvp.Key);
			}
		}

		public static void RegisterSingleton<TConcrete>()
		{
			RegisterSingleton(typeof(TConcrete));
		}

		public static void RegisterSingleton(Type concreteType)
		{
			_instance._singletons[concreteType] = concreteType;
		}

		public static void RegisterSingleton<TAbstract, TConcrete>() where TConcrete : TAbstract
		{
			RegisterSingleton(typeof(TAbstract), typeof(TConcrete));
		}

		public static void RegisterSingleton(Type abstractType, Type concreteType)
		{
			_instance._singletons[abstractType] = concreteType;
		}

		public static T Resolve<T>() where T : class
		{
			return Resolve(typeof(T), true) as T;
		}

		public static object Resolve(Type t)
		{
			return Resolve(t, true);
		}

		private static object Resolve(Type t, bool awake)
		{
			Type concreteType = null;

			if (_instance._singletons.TryGetValue(t, out concreteType))
			{
				Tuple<object, bool> result = null;
				if (!_instance._singletonInstances.TryGetValue(t, out result))
				{
					var instance = Activator.CreateInstance(concreteType);
					result = new Tuple<object, bool>(instance, false);
					_instance._singletonInstances[t] = result;
				}

				if (awake)
				{
					IService service = result.Item1 as IService;
					if (service != null && !result.Item2)
					{
						result = new Tuple<object, bool>(result.Item1, true);
						_instance._singletonInstances[t] = result;
						service.Awake();
					}
				}

				return result.Item1;
			}

			return null;
		}
	}
}

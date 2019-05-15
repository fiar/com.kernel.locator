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
		private Dictionary<Type, Type> _transients = new Dictionary<Type, Type>();
		private Dictionary<Type, object> _singletonInstances = new Dictionary<Type, object>();


		public static IEnumerable<KeyValuePair<Type, Type>> Singletons
		{
			get { return _instance._singletons; }
		}

		public static IEnumerable<KeyValuePair<Type, Type>> Transients
		{
			get { return _instance._transients; }
		}

		public static IEnumerable<KeyValuePair<Type, object>> SingletonInstances
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
			var contextServices = new List<Type>();
			foreach (KeyValuePair<Type, object> kvp in _instance._singletonInstances)
			{
				var service = kvp.Value as IService;
				if (service != null) service.Reset();

				var contextService = kvp.Value as IContextService;
				if (contextService is IContextService)
				{
					contextService.Destroy();
					contextServices.Add(kvp.Key);
				}
			}

			foreach (var service in contextServices)
			{
				_instance._singletonInstances.Remove(service);
			}

			_instance._transients.Clear();
		}

		public static void Destroy()
		{
			foreach (KeyValuePair<Type, object> kvp in _instance._singletonInstances)
			{
				IService service = kvp.Value as IService;
				if (service != null) service.Destroy();
			}

			_instance._singletons.Clear();
			_instance._singletonInstances.Clear();
			_instance._transients.Clear();
		}

		public static bool IsSingletonRegistered<TConcrete>()
		{
			return IsSingletonRegistered(typeof(TConcrete));
		}

		public static bool IsSingletonRegistered(Type concreteType)
		{
			return _instance._singletons.ContainsKey(concreteType);
		}


		public static bool IsTransientRegistered<TConcrete>()
		{
			return IsTransientRegistered(typeof(TConcrete));
		}

		public static bool IsTransientRegistered(Type concreteType)
		{
			return _instance._transients.ContainsKey(concreteType);
		}

		public static void RegisterSingletons()
		{
			foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
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

		//public static void Inject(object instance)
		//{
		//	Inject(instance, false);
		//}

		//public static void Inject(object instance, bool onlyExisting)
		//{
		//	if (instance == null) return;

		//	var fields = instance.GetType()
		//		.GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
		//		.Where(x => x.IsDefined(typeof(InjectAttribute), false));

		//	foreach (var field in fields)
		//	{
		//		if (_instance._singletons.ContainsKey(field.FieldType))
		//		{
		//			var val = Locator.Resolve(field.FieldType, onlyExisting);
		//			field.SetValue(instance, val);
		//		}

		//		if (_instance._transients.ContainsKey(field.FieldType))
		//		{
		//			var val = Locator.Resolve(field.FieldType, onlyExisting);
		//			field.SetValue(instance, val);
		//		}
		//	}
		//}

		public static void InjectStatic(params Assembly[] assemblies)
		{
			var fields = FindStaticFields(assemblies);
			InjectStaticFields(fields);
		}

		public static void InjectStaticFields(IEnumerable<FieldInfo> fields)
		{
			var instances = new Dictionary<Type, object>();
			foreach (var field in fields)
			{
				if (_instance._singletons.ContainsKey(field.FieldType))
				{
					var val = Locator.Resolve(field.FieldType, false, false);
					field.SetValue(null, val);
					if (!instances.ContainsKey(val.GetType()))
					{
						instances[val.GetType()] = val;
					}
				}

				if (_instance._transients.ContainsKey(field.FieldType))
				{
					var val = Locator.Resolve(field.FieldType, false, false);
					field.SetValue(null, val);
					if (!instances.ContainsKey(val.GetType()))
					{
						instances[val.GetType()] = val;
					}
				}
			}

			foreach (var kvp in instances)
			{
				IService service = kvp.Value as IService;
				if (service != null) service.Awake();
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

		public static void RegisterSingleton<TConcrete>(TConcrete instance)
		{
			RegisterSingleton(typeof(TConcrete), instance);
		}

		public static void RegisterSingleton(Type concreteType, object instance)
		{
			_instance._singletons[concreteType] = concreteType;
			_instance._singletonInstances[concreteType] = instance;
		}

		public static void RegisterTransient<TAbstract, TConcrete>()
		{
			RegisterTransient(typeof(TAbstract), typeof(TConcrete));
		}

		public static void RegisterTransient(Type abstractType, Type concreteType)
		{
			_instance._transients[abstractType] = concreteType;
		}

		public static T Resolve<T>() where T : class
		{
			return Resolve<T>(false);
		}

		public static T Resolve<T>(bool onlyExisting) where T : class
		{
			return Resolve(typeof(T), onlyExisting) as T;
		}

		public static object Resolve(Type t)
		{
			return Resolve(t, false);
		}

		public static object Resolve(Type t, bool onlyExisting)
		{
			return Resolve(t, onlyExisting, true);
		}

		private static object Resolve(Type t, bool onlyExisting, bool callAwake)
		{
			object result = null;
			Type concreteType = null;
			if (_instance._singletons.TryGetValue(t, out concreteType))
			{
				object r = null;
				if (!_instance._singletonInstances.TryGetValue(t, out r) && !onlyExisting)
				{
					if (concreteType.IsSubclassOf(typeof(MonoBehaviour)))
					{
						GameObject singletonGameObject = new GameObject();
						r = singletonGameObject.AddComponent(concreteType);
						singletonGameObject.name = t.ToString() + " (singleton)";
					}
					else
					{
						r = Activator.CreateInstance(concreteType);
					}

					_instance._singletonInstances[t] = r;

					if (callAwake)
					{
						IService service = r as IService;
						if (service != null) service.Awake();
					}
				}
				result = r;
			}
			else if (_instance._transients.TryGetValue(t, out concreteType))
			{
				object r = null;
				if (concreteType.IsSubclassOf(typeof(MonoBehaviour)))
				{
					GameObject singletonGameObject = new GameObject();
					r = singletonGameObject.AddComponent(concreteType);
					singletonGameObject.name = t.ToString() + " (transient)";
				}
				else
				{
					r = Activator.CreateInstance(concreteType);
				}
				result = r;
			}
			return result;
		}
	}
}

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

		public static void Inject(object instance)
		{
			Inject(instance, false);
		}

		public static void Inject(object instance, bool onlyExisting)
		{
			if (instance == null) return;

			var fields = instance.GetType()
				.GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
				.Where(x => x.IsDefined(typeof(InjectAttribute), false));

			foreach (var field in fields)
			{
				if (_instance._singletons.ContainsKey(field.FieldType))
				{
					var val = Locator.Resolve(field.FieldType, onlyExisting);
					field.SetValue(instance, val);
				}

				if (_instance._transients.ContainsKey(field.FieldType))
				{
					var val = Locator.Resolve(field.FieldType, onlyExisting);
					field.SetValue(instance, val);
				}
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

					IService service = r as IService;
					if (service != null) service.Awake();
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

using System;
using UnityEngine;

namespace Fundamental
{

	public class AutoSingletonAttribute : Attribute
	{
		public bool bAutoCreate;

		public AutoSingletonAttribute(bool bCreate)
		{
			this.bAutoCreate = bCreate;
		}
	}

	[AutoSingleton(true)]
	public class MonoSingleton<T> : MonoBehaviour where T : Component
	{
		private static T _instance;

		private static bool _destroyed;

		public static T Instance
		{
			get
			{
				return MonoSingleton<T>.GetInstance();
			}
		}

		public static T GetInstance()
		{
			if (MonoSingleton<T>._instance == null && !MonoSingleton<T>._destroyed)
			{
				Type typeFromHandle = typeof(T);
				MonoSingleton<T>._instance = (T)((object)GameObject.FindObjectOfType(typeFromHandle));
				if (MonoSingleton<T>._instance == null)
				{
					object[] customAttributes = typeFromHandle.GetCustomAttributes(typeof(AutoSingletonAttribute), true);
					if (customAttributes.Length > 0 && !((AutoSingletonAttribute)customAttributes[0]).bAutoCreate)
					{
						return (T)((object)null);
					}
					GameObject gameObject = new GameObject(typeof(T).Name);
					MonoSingleton<T>._instance = gameObject.AddComponent<T>();
					GameObject bootObj = GameObject.Find("BootObj");
					if (bootObj != null)
					{
						gameObject.transform.SetParent(bootObj.transform);
					}
				}
			}
			return MonoSingleton<T>._instance;
		}

		public static void DestroyInstance()
		{
			if (MonoSingleton<T>._instance != null)
			{
				Destroy(MonoSingleton<T>._instance.gameObject);
			}
			MonoSingleton<T>._destroyed = true;
			MonoSingleton<T>._instance = (T)((object)null);
		}

		public static void ClearDestroy()
		{
			MonoSingleton<T>.DestroyInstance();
			MonoSingleton<T>._destroyed = false;
		}

		public static bool IsNull()
		{
			return MonoSingleton<T>._instance == null;
		}

		protected virtual void Awake()
		{
			if (MonoSingleton<T>._instance != null && MonoSingleton<T>._instance.gameObject != base.gameObject)
			{
				if (Application.isPlaying)
				{
					Destroy(gameObject);
				}
				else
				{
					DestroyImmediate(gameObject);
				}
				return;
			}
			else if (MonoSingleton<T>._instance == null)
			{
				MonoSingleton<T>._instance = base.GetComponent<T>();
			}
			DontDestroyOnLoad(gameObject);
			this.Init();
		}

		protected virtual void OnDestroy()
		{
			if (MonoSingleton<T>._instance != null && MonoSingleton<T>._instance.gameObject == gameObject)
			{
				MonoSingleton<T>._instance = (T)((object)null);
			}
		}

		public static bool HasInstance()
		{
			return MonoSingleton<T>._instance != null;
		}

		protected virtual void Init()
		{

		}
	}
}
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Fundamental
{
	public interface IPooledMonoBehaviour
	{
		void OnCreate();

		void OnGet();

		void OnRecycle();
	}

	public class CPooledGameObjectScript : MonoBehaviour
	{
		public string PrefabKey { get; set; }

		public bool IsInit { get; set; }

		public Vector3 DefaultScale { get; set; }

		private IPooledMonoBehaviour[] _cachedIPooledMonos;

		private bool IsInUse { get; set; }

		public void Initialize(string prefabKey)
		{
			_cachedIPooledMonos = GetComponentsInChildren<IPooledMonoBehaviour>(true);

			PrefabKey = prefabKey;
			DefaultScale = gameObject.transform.localScale;
			IsInit = true;
			IsInUse = false;
			gameObject.name = PrefabKey + "_" + GetInstanceID();
		}

		public void AddCachedMono(MonoBehaviour mono, bool defaultEnabled)
		{
			if (mono == null)
			{
				return;
			}
			if (mono is IPooledMonoBehaviour)
			{
				IPooledMonoBehaviour[] array = new IPooledMonoBehaviour[this._cachedIPooledMonos.Length + 1];
				for (int i = 0; i < this._cachedIPooledMonos.Length; i++)
				{
					array[i] = this._cachedIPooledMonos[i];
				}
				array[this._cachedIPooledMonos.Length] = (mono as IPooledMonoBehaviour);
				this._cachedIPooledMonos = array;
			}
		}

		public void OnCreate()
		{
			if (_cachedIPooledMonos != null)
			{
				for (int i = 0; i < _cachedIPooledMonos.Length; i++)
				{
					if (_cachedIPooledMonos[i] != null)
					{
						_cachedIPooledMonos[i].OnCreate();
					}
				}
			}
		}

		public void OnGet()
		{
			if (!gameObject.activeSelf)
			{
				gameObject.SetActive(true);
			}
			if (_cachedIPooledMonos != null)
			{
				for (int i = 0; i < _cachedIPooledMonos.Length; i++)
				{
					if (_cachedIPooledMonos[i] != null)
					{
						_cachedIPooledMonos[i].OnGet();
					}
				}
			}
			IsInUse = true;
		}

		public void OnRecycle()
		{
			if (_cachedIPooledMonos != null)
			{
				for (int i = 0; i < _cachedIPooledMonos.Length; i++)
				{
					if (_cachedIPooledMonos[i] != null)
					{
						_cachedIPooledMonos[i].OnRecycle();
					}
				}
			}
			gameObject.SetActive(false);
			IsInUse = false;
		}

		public void OnPrepare()
		{
			gameObject.SetActive(false);
		}
	}

	public sealed class CGameObjectPool : Singleton<CGameObjectPool>
	{
		private class stDelayRecycle
		{
			public GameObject recycleObj;

			public int recycleTime;

			public CGameObjectPool.OnDelayRecycleDelegate callback;
		}

		public delegate void OnDelayRecycleDelegate(GameObject recycleObj);

		private Dictionary<string, Queue<CPooledGameObjectScript>> _pooledGameObjectMap = new Dictionary<string, Queue<CPooledGameObjectScript>>();

		private LinkedList<CGameObjectPool.stDelayRecycle> m_delayRecycle = new LinkedList<CGameObjectPool.stDelayRecycle>();

		private GameObject m_poolRoot;
		private const string RootStr = "CGameObjectPool";

		private bool m_clearPooledObjects;

		private int m_clearPooledObjectsExecuteFrame;

		private static int s_frameCounter;

		public override void Init()
		{
			OnSceneLoaded(default(Scene), LoadSceneMode.Single);
			SceneManager.sceneLoaded += OnSceneLoaded;
		}

		private void OnSceneLoaded(Scene arg0, LoadSceneMode arg1)
		{
			m_poolRoot = GameObject.Find(RootStr);
			if (m_poolRoot == null)
				m_poolRoot = new GameObject(RootStr);
			m_delayRecycle.Clear();
			_pooledGameObjectMap.Clear();
		}

		public override void UnInit()
		{

		}

		public void Update()
		{
			CGameObjectPool.s_frameCounter++;
			this.UpdateDelayRecycle();
			if (this.m_clearPooledObjects && this.m_clearPooledObjectsExecuteFrame == CGameObjectPool.s_frameCounter)
			{
				this.ExecuteClearPooledObjects();
				this.m_clearPooledObjects = false;
			}
		}

		public void ClearPooledObjects()
		{
			this.m_clearPooledObjects = true;
			this.m_clearPooledObjectsExecuteFrame = CGameObjectPool.s_frameCounter + 1;
		}

		public void ExecuteClearPooledObjects()
		{
			for (LinkedListNode<CGameObjectPool.stDelayRecycle> linkedListNode = this.m_delayRecycle.First; linkedListNode != null; linkedListNode = linkedListNode.Next)
			{
				if (null != linkedListNode.Value.recycleObj)
				{
					this.RecycleGameObject(linkedListNode.Value.recycleObj);
				}
			}
			this.m_delayRecycle.Clear();
			Dictionary<string, Queue<CPooledGameObjectScript>>.Enumerator enumerator = this._pooledGameObjectMap.GetEnumerator();
			while (enumerator.MoveNext())
			{
				KeyValuePair<string, Queue<CPooledGameObjectScript>> current = enumerator.Current;
				Queue<CPooledGameObjectScript> value = current.Value;
				while (value.Count > 0)
				{
					CPooledGameObjectScript cPooledGameObjectScript = value.Dequeue();
					if (cPooledGameObjectScript != null && cPooledGameObjectScript.gameObject != null)
					{
						GameObject.Destroy(cPooledGameObjectScript.gameObject);
					}
				}
			}
			this._pooledGameObjectMap.Clear();
		}

		private void UpdateDelayRecycle()
		{
			LinkedListNode<CGameObjectPool.stDelayRecycle> linkedListNode = this.m_delayRecycle.First;
			int num = (int)(Time.time * 1000f);
			while (linkedListNode != null)
			{
				LinkedListNode<CGameObjectPool.stDelayRecycle> thisNode = linkedListNode;
				linkedListNode = linkedListNode.Next;
				if (null == thisNode.Value.recycleObj)
				{
					this.m_delayRecycle.Remove(thisNode);
				}
				else
				{
					if (thisNode.Value.recycleTime > num)
					{
						break;
					}
					if (thisNode.Value.callback != null)
					{
						thisNode.Value.callback(thisNode.Value.recycleObj);
					}
					this.RecycleGameObject(thisNode.Value.recycleObj);
					this.m_delayRecycle.Remove(thisNode);
				}
			}
		}

		public GameObject GetGameObject(string prefabFullPath, Vector3 pos, Quaternion rot)
		{
			bool flag = false;
			return this.GetGameObject(prefabFullPath, pos, rot, true, out flag);
		}

		public GameObject GetGameObject(string prefabFullPath, Vector3 pos, Quaternion rot, out bool isInit)
		{
			return this.GetGameObject(prefabFullPath, pos, rot, true, out isInit);
		}

		public GameObject GetGameObject(string prefabFullPath, Vector3 pos)
		{
			bool flag = false;
			return this.GetGameObject(prefabFullPath, pos, Quaternion.identity, false, out flag);
		}

		public GameObject GetGameObject(string prefabFullPath, Vector3 pos, out bool isInit)
		{
			return this.GetGameObject(prefabFullPath, pos, Quaternion.identity, false, out isInit);
		}

		public GameObject GetGameObject(string prefabFullPath)
		{
			bool flag = false;
			return this.GetGameObject(prefabFullPath, Vector3.zero, Quaternion.identity, false, out flag);
		}

		public GameObject GetGameObject(string prefabFullPath, out bool isInit)
		{
			return this.GetGameObject(prefabFullPath, Vector3.zero, Quaternion.identity, false, out isInit);
		}

		private GameObject GetGameObject(string path, Vector3 pos, Quaternion rot, bool useRotation, out bool isInit)
		{
			Queue<CPooledGameObjectScript> queue = null;

			if (!this._pooledGameObjectMap.TryGetValue(path, out queue))
			{
				queue = new Queue<CPooledGameObjectScript>();
				this._pooledGameObjectMap.Add(path, queue);
			}

			CPooledGameObjectScript cPooledGameObjectScript = null;
			while (queue.Count > 0)
			{
				cPooledGameObjectScript = queue.Dequeue();
				if (cPooledGameObjectScript != null && cPooledGameObjectScript.gameObject != null)
				{
					cPooledGameObjectScript.gameObject.transform.SetParent(null, true);
					cPooledGameObjectScript.gameObject.transform.position = pos;
					cPooledGameObjectScript.gameObject.transform.rotation = rot;
					cPooledGameObjectScript.gameObject.transform.localScale = cPooledGameObjectScript.DefaultScale;
					break;
				}
				cPooledGameObjectScript = null;
			}
			if (cPooledGameObjectScript == null)
			{
				cPooledGameObjectScript = this.CreateGameObject(path, pos, rot, useRotation);
#if UNITY_EDITOR
				//SuperDebug.Log(SuperDebug.ASSETBUNDLE, "这个资源没缓存呀：" + text);
#endif
			}
			if (cPooledGameObjectScript == null)
			{
				isInit = false;
				return null;
			}
			isInit = cPooledGameObjectScript.IsInit;
			cPooledGameObjectScript.OnGet();
			return cPooledGameObjectScript.gameObject;
		}

		public void RecycleGameObjectDelay(GameObject pooledGameObject, int delayMillSeconds, CGameObjectPool.OnDelayRecycleDelegate callback = null)
		{
			CGameObjectPool.stDelayRecycle stDelayRecycle = new CGameObjectPool.stDelayRecycle();
			stDelayRecycle.recycleObj = pooledGameObject;
			stDelayRecycle.recycleTime = (int)(Time.time * 1000f) + delayMillSeconds;
			stDelayRecycle.callback = callback;
			if (this.m_delayRecycle.Count == 0)
			{
				this.m_delayRecycle.AddLast(stDelayRecycle);
				return;
			}
			for (LinkedListNode<CGameObjectPool.stDelayRecycle> linkedListNode = this.m_delayRecycle.Last; linkedListNode != null; linkedListNode = linkedListNode.Previous)
			{
				if (linkedListNode.Value.recycleTime < stDelayRecycle.recycleTime)
				{
					this.m_delayRecycle.AddAfter(linkedListNode, stDelayRecycle);
					return;
				}
			}
			this.m_delayRecycle.AddFirst(stDelayRecycle);
		}

		public void RecycleGameObject(GameObject pooledGameObject)
		{
			this._RecycleGameObject(pooledGameObject, false);
		}

		public void RecyclePreparedGameObject(GameObject pooledGameObject)
		{
			this._RecycleGameObject(pooledGameObject, true);
		}

		private void _RecycleGameObject(GameObject pooledGameObject, bool setIsInit)
		{
			if (pooledGameObject == null)
			{
				return;
			}
			if (m_poolRoot == null)
			{
				return;
			}
			CPooledGameObjectScript component = pooledGameObject.GetComponent<CPooledGameObjectScript>();
			if (component != null)
			{
				Queue<CPooledGameObjectScript> queue = null;
				if (this._pooledGameObjectMap.TryGetValue(component.PrefabKey, out queue))
				{
					queue.Enqueue(component);
					component.OnRecycle();
					component.transform.SetParent(this.m_poolRoot.transform, true);
					component.IsInit = setIsInit;
					return;
				}
			}
			GameObject.Destroy(pooledGameObject);
		}

		/*
		public void PrepareGameObject(string prefabFullPath, enResourceType resourceType, int amount, Action OnFinish)
		{
			string prefabFullPathInRes = CFileManager.EraseExtension(prefabFullPath);
			Queue<CPooledGameObjectScript> queue = null;
			if (!this._pooledGameObjectMap.TryGetValue(prefabFullPathInRes.JavaHashCodeIgnoreCase(), out queue))
			{
				queue = new Queue<CPooledGameObjectScript>();
				this._pooledGameObjectMap.Add(prefabFullPathInRes.JavaHashCodeIgnoreCase(), queue);
			}
			if (queue.Count >= amount)
			{
				OnFinish();
				return;
			}
			amount -= queue.Count;
			_mono.StartCoroutine(Precache(amount, prefabFullPath, resourceType, prefabFullPathInRes, queue, OnFinish));
			return;
		}*/
		/*
		public System.Collections.IEnumerator Precache(int amount, string prefabFullPath, CustomAsyncOperation customAsyncOperation)
		{
			if (customAsyncOperation != null)
				customAsyncOperation.AddMission();

			Queue<CPooledGameObjectScript> queue = null;

			if (!this._pooledGameObjectMap.TryGetValue(prefabFullPath, out queue))
			{
				queue = new Queue<CPooledGameObjectScript>();
				this._pooledGameObjectMap.Add(prefabFullPath, queue);
			}

			var co = ResourceManager.Instance.GetResourceAsync(prefabFullPath, typeof(GameObject), resourceType);
			while (co.MoveNext())
			{
				yield return co.Current;
			}

			for (int i = 0; i < amount; i++)
			{
				CPooledGameObjectScript cPooledGameObjectScript = this.CreateGameObject(prefabFullPath, Vector3.zero, Quaternion.identity);

				SuperDebug.Assert(cPooledGameObjectScript != null, "Failed Create Game object from \"{0}\"", new object[]
				{
				prefabFullPath
				});

				if (cPooledGameObjectScript != null)
				{
					queue.Enqueue(cPooledGameObjectScript);
					cPooledGameObjectScript.gameObject.transform.SetParent(this.m_poolRoot.transform, true);
					cPooledGameObjectScript.OnPrepare();
				}
			}
			if (customAsyncOperation != null)
				customAsyncOperation.FinishMission();
		}*/

		private CPooledGameObjectScript CreateGameObject(string prefabFullPath, Vector3 pos, Quaternion rot, bool useRotation)
		{
			GameObject prefabObj = Resources.Load<GameObject>(prefabFullPath);
			if (prefabObj == null)
			{
				SuperDebug.LogError("prefabObj Can Not Be Null " + prefabFullPath);
				return null;
			}
			GameObject instObj;
			if (useRotation)
			{
				instObj = (GameObject.Instantiate(prefabObj, pos, rot) as GameObject);
			}
			else
			{
				instObj = (GameObject.Instantiate(prefabObj) as GameObject);
				instObj.transform.position = pos;
			}
			SuperDebug.Assert(instObj != null);
			CPooledGameObjectScript cPooledGameObjectScript = instObj.GetComponent<CPooledGameObjectScript>();
			if (cPooledGameObjectScript == null)
			{
				cPooledGameObjectScript = instObj.AddComponent<CPooledGameObjectScript>();
			}
			cPooledGameObjectScript.Initialize(prefabFullPath);
			cPooledGameObjectScript.OnCreate();
			return cPooledGameObjectScript;
		}
	}
}
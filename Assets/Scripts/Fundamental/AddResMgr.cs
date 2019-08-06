using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Fundamental
{
	public class AddRes : IEnumerator
	{
		public UnityEngine.Object Obj;

		public event Action<UnityEngine.Object> Completed;

		public bool LoadDone;

		public object Current => Obj;

		public bool MoveNext()
		{
			return !LoadDone;
		}

		public void Reset()
		{

		}

		internal void OnLoad<T>(AsyncOperationHandle<T> load)
		{
			Obj = load.Result as UnityEngine.Object;
			LoadDone = true;
			Completed?.Invoke(Obj);
		}
	}

	public class AddResMgr : Singleton<AddResMgr>
	{
		public Dictionary<string, AddRes> _Cache;

		public override void Init()
		{
			base.Init();
			_Cache = new Dictionary<string, AddRes>();
		}

		public void Release()
		{
			foreach (var asset in _Cache)
			{
				Addressables.Release(asset.Value.Obj);
			}
			_Cache.Clear();
		}

		public IEnumerator CoLoadRes<T>(string addPath) where T : UnityEngine.Object
		{
			if (!_Cache.ContainsKey(addPath))
			{
				AddRes addres = new AddRes();
				_Cache[addPath] = addres;

				var load = Addressables.LoadAssetAsync<T>(addPath);
				yield return load;

				addres.OnLoad(load);
			}
			else
			{
				AddRes addres = _Cache[addPath];
				yield return addres;
			}
		}

		public T DirectGetRes<T>(string addPath) where T : UnityEngine.Object
		{
			if (!_Cache.ContainsKey(addPath))
			{
				Debug.LogError(addPath + " not exist");
			}
			return _Cache[addPath] as T;
		}

		public AddRes GetRes<T>(string addPath) where T : UnityEngine.Object
		{
			if (!_Cache.ContainsKey(addPath))
			{
				AddRes addres = new AddRes();
				_Cache[addPath] = addres;

				var load = Addressables.LoadAssetAsync<T>(addPath);
				load.Completed += addres.OnLoad<T>;

				return addres;
			}
			else
			{
				AddRes addres = _Cache[addPath];

				return addres;
			}
		}
	}
}

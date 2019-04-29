using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Test
{
	public class TestCoroutine : MonoBehaviour
	{
		TextAsset asset;
		// Start is called before the first frame update
		void Start()
		{
			//var co1 = new TestIEnumerator();
			//StartCoroutine(co1);
			//StartCoroutine(co2());
			//StartCoroutine(co3());
			StartCoroutine(co4());
		}

		IEnumerator co4()
		{
			var load = Addressables.LoadAsset<TextAsset>("TestText");
			yield return load;
			Debug.Log(load.Result.text);
			//load.Release();
			Addressables.Release<TextAsset>(load.Result);
		}

		IEnumerator co2()
		{
			var co3 = new TestIEnumerator();
			yield return co3;
			Debug.Log(Time.realtimeSinceStartup);
		}

		IEnumerator co3()
		{
			yield return co2();
			Debug.Log(Time.realtimeSinceStartup);
		}

		// Update is called once per frame
		void Update()
		{

		}
	}

	public class TestIEnumerator : IEnumerator
	{
		public object Current
		{
			get
			{
				return _Time;
			}
		}

		public bool MoveNext()
		{
			Debug.Log("TestIEnumerator Check MoveNext");
			return Time.realtimeSinceStartup - _Time < 5;
		}

		public void Reset()
		{
			_Time = Time.realtimeSinceStartup;
		}

		float _Time;

		public TestIEnumerator()
		{
			_Time = Time.realtimeSinceStartup;
		}
	}
}
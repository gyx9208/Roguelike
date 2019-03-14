using Fundamental;
using UnityEngine;

namespace Test
{
	public class TestDebug:MonoBehaviour
	{
		public void Awake()
		{
			SuperDebug.LoadConfig();
		}

		public void Start()
		{
			SuperDebug.Log(DebugPrefix.Network, "hello world");
		}
	}
}

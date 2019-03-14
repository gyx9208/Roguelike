using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Test
{
	public class TestLua : MonoBehaviour
	{
		// Start is called before the first frame update
		void Start()
		{
			XLua.LuaEnv luaenv = new XLua.LuaEnv();
			luaenv.DoString("CS.UnityEngine.Debug.Log('hello world')");
			luaenv.Dispose();
		}

		// Update is called once per frame
		void Update()
		{

		}
	}
}
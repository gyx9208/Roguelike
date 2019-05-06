using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;
using XLua;

public class LoadHotfix : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
		StartCoroutine(Load());
    }

    // Update is called once per frame
    void Update()
    {
        
    }

	IEnumerator Load()
	{
		var hotfix = Addressables.LoadAsset<TextAsset>("hotfix");
		yield return hotfix;
		LuaEnv luaenv = new LuaEnv();

		luaenv.DoString(hotfix.Result.text);
		yield return null;
		SceneManager.LoadScene("Particle");
	}
}

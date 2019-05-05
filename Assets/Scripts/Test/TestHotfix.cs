using UnityEngine;
using XLua;

[Hotfix]
public class TestHotfix : MonoBehaviour
{
	// Start is called before the first frame update
	public ParticleSystem ThisParticle;

	
	public void Start()
	{
		var main = ThisParticle.main;
		main.startColor = new Color(0, 1, 0);
	}

	private void Awake()
	{

	}

	// Update is called once per frame
	void Update()
	{

	}
}

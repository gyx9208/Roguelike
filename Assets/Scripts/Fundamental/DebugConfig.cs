using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Custom/Debug Config")]
public class DebugConfig : ScriptableObject
{
	public bool Enable;
	public bool[] Switch;
}

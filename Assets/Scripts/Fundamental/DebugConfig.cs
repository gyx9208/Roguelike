using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Fundamental
{
	[CreateAssetMenu(menuName = "Custom/Debug Config")]
	public class DebugConfig : ScriptableObject
	{
		public bool Enable;
		public bool[] Switch;
	}
}
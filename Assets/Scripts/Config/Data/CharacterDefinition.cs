using UnityEngine;

namespace Config.Data
{
	[FullInspector.fiInspectorOnly]
	[CreateAssetMenu(menuName = "Custom/Design/Character")]
	public class CharacterDefinition : ScriptableObject
	{
		public int Level;
		public int BaseHp;
		public int BaseAtk;
	}
}

using UnityEngine;

namespace Config.Data
{
	[FullInspector.fiInspectorOnly]
	[CreateAssetMenu(menuName ="Custom/Design/Ability")]
	public class AbilityDefinition : ScriptableObject
	{
		public int Id;
		public string Name;
		public SkillDefinition[] Skills;
	}

}
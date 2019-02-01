using System;
using UnityEngine;
using System.Collections.Generic;

namespace Config.TableTools
{
	/// <summary>
	/// Table tool needs the only one config file.
	/// </summary>
	[CreateAssetMenu(menuName = "Custom/Table Tool/Table Tool Datas")]
	public class TableToolDatas : ScriptableObject
	{
		public List<TableToolsData> TableDatas = new List<TableToolsData>();

		[Serializable]
		public class TableToolsData
		{
			public string ClassName;
			public string TablePath;
			public string ClassPath;

			public List<string> ConstructorTypes;

			[NonSerialized]
			public TextAsset Table;
			[NonSerialized]
			public TextAsset Cs;
		}
	}
}
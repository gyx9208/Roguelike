/*
 * Author: yuxiang.geng@mihoyo.com
 * Date: 2018-10-11
*/
using FullInspector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Net
{
	[fiInspectorOnly]
	[CreateAssetMenu(menuName = "HKMM/Server Info")]
	public class ServerInfo : ScriptableObject
	{
		public int Index;
		public OneServerInfo[] Servers;
	}

	[Serializable]
	public class OneServerInfo
	{
		public string Name;
		public string Ip;
		public ushort Port;
	}
}

using System.Collections.Generic;
using System;
using Fundamental;
using Net;

/*  
 *  Generate By Script
 *  
 */

namespace Net
{
	public class CommandMap : Singleton<CommandMap>
	{
		private Dictionary<ushort, Type> _cmdIDMap;
		private Dictionary<Type, ushort> _typeMap;

		public override void Init()
		{
			base.Init();

			_cmdIDMap = MakeCmdIDMap();
			_typeMap = GetReverseMap(_cmdIDMap);
		}

		private Dictionary<Type, ushort> GetReverseMap(Dictionary<ushort, Type> orgMap)
		{
			Dictionary<Type, ushort> reverseMap = new Dictionary<Type, ushort>();
			foreach (KeyValuePair<ushort, Type> kvp in orgMap)
			{
				reverseMap.Add(kvp.Value, kvp.Key);
			}
			return reverseMap;
		}

		private Dictionary<ushort, Type> MakeCmdIDMap()
		{
			Dictionary<ushort, Type> cmdMap = new Dictionary<ushort, Type>();
			
			cmdMap.Add((ushort)CommonRsp.CmdId.CmdId, typeof(CommonRsp));
			cmdMap.Add((ushort)HeartBeat.CmdId.CmdId, typeof(HeartBeat));


			return cmdMap;
		}

		public ushort GetCmdIDByType(Type type)
		{
			ushort resCmdID;
			if (!_typeMap.TryGetValue(type, out resCmdID))
			{
				SuperDebug.LogWarning(DebugPrefix.Network, "undefined type=" + type);
			}

			return resCmdID;
		}

		public Type GetTypeByCmdID(ushort cmdID)
		{
			Type resType;
			if (!_cmdIDMap.TryGetValue(cmdID, out resType))
			{
				SuperDebug.LogWarning(DebugPrefix.Network, "undefined cmdID=" + cmdID);
			}

			return resType;
		}
	}
}
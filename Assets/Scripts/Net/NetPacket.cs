using UnityEngine;
using System;
using System.IO;
using System.Text;
using Fundamental;

/*
 *  2015/01/30
 *  xiao.liu@mihoyo.com
 *      定义网络消息包结构，并实现相关序列化、反序列化、网络字节序和主机字节序的转换
 *      
 *  2018.10.9 by yuxiang.geng@mihoyo.com
 *  包结构修改
 */

namespace Net
{
	/*
	 *  用来定义packet里的一些常量
	 */
	class NetDefine
	{
		public const int PACKET_HEAD_LEN = 4;
		public const int PACKET_SUB_HEAD_LEN = 2;

		public const int BODY_HEAD_LEN = 8;
		public const int CMD_BYTES = 2;
		public const int SESSION_LEN_BYTES = 2;
		public const int CONTENT_LEN_BYTES = 4;


		public const int PACKET_MAX_BYTES = 65500;
	}

	/*
	*  定义消息包状态，反序列化时的返回值
	*/
	public enum PacketStatus
	{
		PACKET_CORRECT = 1,         // 消息包完整、正常
		PACKET_NOT_COMPLETE = 2,    // 消息包不完整
		PACKET_NOT_CORRECT = 3,  // 消息包格式错误
	}

	/*
	 *  定义数据包结构，除了body外，全部按照网络字节序存储各个字段
	 */
	public class NetPacket
	{
		/*
		 *  数据包变量结构
		 */

		// 数据包，存储一个序列化后的PROTOBUF实例
		private MemoryStream _bodyMem;
		private MemoryStream _sessionMem;

		private Net.PacketSession _session;
		private System.Object _content;

		bool _hasHead;
		int _thisLength;
		ushort _cmdId;

		/*
		 *  构造函数  
		 */
		public NetPacket()
		{
			_bodyMem = new MemoryStream();
			_sessionMem = new MemoryStream();
			_hasHead = false;
		}

		/*
		 *  一系列GET方法  
		 */
		public ushort GetCmdId()
		{
			return _cmdId;
		}

		public void ReadData()
		{
			try
			{
				_bodyMem.Position = 0;
				byte[] numberBytes = new byte[4];

				_bodyMem.Read(numberBytes, 0, NetDefine.CMD_BYTES);
				_cmdId = NetUtils.BigEndianToUshort(numberBytes);

				_bodyMem.Read(numberBytes, 0, NetDefine.SESSION_LEN_BYTES);
				ushort sessionLen = NetUtils.BigEndianToUshort(numberBytes);
				_bodyMem.Read(numberBytes, 0, NetDefine.CONTENT_LEN_BYTES);
				uint contentLen = NetUtils.BigEndianToUint(numberBytes);

				byte[] sessionBytes = new byte[sessionLen];
				_bodyMem.Read(sessionBytes, 0, sessionLen);

				_sessionMem.Write(sessionBytes, 0, sessionLen);
				_sessionMem.Position = 0;

				_session = ProtoBuf.Serializer.Deserialize<PacketSession>(_sessionMem);

				Type t = CommandMap.Instance.GetTypeByCmdID(_cmdId);

				_content = ProtoBuf.Serializer.Deserialize(t, _bodyMem);
			}
			catch (System.Exception e)
			{
				SuperDebug.LogError(SuperDebug.NETWORK, "Deserialize meet exception " + e);
				OutputBytes("error buf:", _bodyMem.GetBuffer(), _bodyMem.Length);
			}
		}

		public PacketSession GetSession()
		{
			return _session;
		}

		public System.Object getData()
		{
			return _content;
		}

		public bool setData<T>(T data, PacketSession session)
		{
			try
			{
				_content = data;
				_session = session;
				_cmdId = (ushort)session.CmdId;

				_bodyMem.Position = 0;
				_bodyMem.SetLength(0);

				_bodyMem.Write(NetUtils.UshortToBigEndian(_cmdId), 0, NetDefine.CMD_BYTES);
				_bodyMem.Seek(NetDefine.BODY_HEAD_LEN, SeekOrigin.Begin);

				ProtoBuf.Serializer.Serialize(_bodyMem, session);
				long sessionPos = _bodyMem.Position;

				ProtoBuf.Serializer.Serialize(_bodyMem, data);
				long contentPos = _bodyMem.Position;

				_bodyMem.Seek(NetDefine.CMD_BYTES, SeekOrigin.Begin);

				_bodyMem.Write(NetUtils.UshortToBigEndian((ushort)(sessionPos - NetDefine.BODY_HEAD_LEN)),0,NetDefine.SESSION_LEN_BYTES);
				_bodyMem.Write(NetUtils.UintToBigEndian((uint)(contentPos - sessionPos)), 0, NetDefine.CONTENT_LEN_BYTES);

				//OutputBytes("true data:", body_.GetBuffer(), body_.Length);
			}
			catch (System.Exception e)
			{
				SuperDebug.LogWarning(SuperDebug.NETWORK, "Serialize " + typeof(T).ToString() + " meet exception " + e);
				return false;
			}

			return true;
		}

		/*
		 *  序列化数据
		 */
		public void serialize(ref MemoryStream ms)
		{
			// 清空ms
			ms.SetLength(0);
			ms.Position = 0;

			int length = (int)_bodyMem.Length;
			_bodyMem.Position = 0;

			while (length > 0)
			{
				int index = (length-1) / NetDefine.PACKET_MAX_BYTES;
				int thisLen = Math.Min(length, NetDefine.PACKET_MAX_BYTES);
				length = length - thisLen;
				ms.Write(NetUtils.UshortToBigEndian((ushort)(thisLen + NetDefine.PACKET_SUB_HEAD_LEN)), 0, 2);
				ms.WriteByte((byte)_session.SessionId);
				ms.WriteByte((byte)index);

				byte[] thisdata = new byte[thisLen];
				_bodyMem.Read(thisdata, 0, thisLen);
				ms.Write(thisdata, 0, thisLen);
			}
			//OutputBytes("Send: ", ms.GetBuffer(), ms.Length);
		}


		byte _thisIndex;
		/*
		 * 反序列化 
		 * used = 这次反序列化用掉的数据长度
		 */
		public PacketStatus deserialize(ref byte[] buf, ref int used, int total)
		{
			if (null == buf)
			{
				return PacketStatus.PACKET_NOT_CORRECT;
			}

			while (used < total)
			{
				if (_hasHead)
				{
					int writeCount = Mathf.Min(total - used, _thisLength);
					_bodyMem.Write(buf, used, writeCount);
					_thisLength -= writeCount;
					used += writeCount;

					if(_thisLength ==0)
					{
						if (_thisIndex == 0)
						{
							//OutputBytes("receive (after deserialize) :", body_.GetBuffer(), body_.Length);

							return PacketStatus.PACKET_CORRECT;
						}
						else
						{
							_hasHead = false;
						}
					}
				}

				if(!_hasHead)
				{
					if(total - used < NetDefine.PACKET_HEAD_LEN)
					{
						return PacketStatus.PACKET_NOT_COMPLETE;
					}
					else
					{
						_hasHead = true;
						_thisLength = NetUtils.BigEndianToUshort(buf, used) - NetDefine.PACKET_SUB_HEAD_LEN;
						_thisIndex = buf[used+3];
						used += NetDefine.PACKET_HEAD_LEN;
					}
				}
			}
			return PacketStatus.PACKET_NOT_COMPLETE;
		}

		public static void OutputBytes(string v1, byte[] v2, long length)
		{
			StringBuilder sb = new StringBuilder();
			sb.Append(v1);
			sb.Append("len: ").AppendLine(length.ToString());
			for(int i = 0; i < length; i++)
			{
				sb.AppendFormat("{0:X00} ", v2[i]).Append(' ');
			}
			Debug.Log(sb.ToString());
		}
	}
}
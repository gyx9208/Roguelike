using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

/*
 *  2015/01/30
 *  xiao.liu@mihoyo.com
 *      实现一些常用的网络方法
 */

namespace Net
{
	/*
	 * 实现一些常用的网络方法
	 */
	public class NetUtils
	{
		/*
		 * 针对无符号整型的主机字节序、网络字节序转换方法
		 */
		public static ushort HostToNetworkOrder(ushort num)
		{
			byte[] buf = BitConverter.GetBytes(num);
			buf[0] = (byte)((num >> 8) & 0xFF);
			buf[1] = (byte)(num & 0xFF);
			return BitConverter.ToUInt16(buf, 0);
		}
		public static uint HostToNetworkOrder(uint num)
		{
			byte[] buf = BitConverter.GetBytes(num);
			buf[0] = (byte)((num >> 24) & 0xFF);
			buf[1] = (byte)((num >> 16) & 0xFF);
			buf[2] = (byte)((num >> 8) & 0xFF);
			buf[3] = (byte)(num & 0xFF);
			return BitConverter.ToUInt32(buf, 0);
		}
		public static ulong HostToNetworkOrder(ulong num)
		{
			byte[] buf = BitConverter.GetBytes(num);
			buf[0] = (byte)((num >> 56) & 0xFF);
			buf[1] = (byte)((num >> 48) & 0xFF);
			buf[2] = (byte)((num >> 40) & 0xFF);
			buf[3] = (byte)((num >> 32) & 0xFF);
			buf[4] = (byte)((num >> 24) & 0xFF);
			buf[5] = (byte)((num >> 16) & 0xFF);
			buf[6] = (byte)((num >> 8) & 0xFF);
			buf[7] = (byte)(num & 0xFF);
			return BitConverter.ToUInt64(buf, 0);
		}

		public static ushort NetworkToHostOrder(ushort num)
		{
			byte[] buf = BitConverter.GetBytes(num);
			ushort res = 0;
			res = buf[0];
			res = (ushort)((res << 8) | buf[1]);
			return res;
		}

		public static uint NetworkToHostOrder(uint num)
		{
			byte[] buf = BitConverter.GetBytes(num);
			uint res = 0;
			res = buf[0];
			res = (uint)((res << 8) | buf[1]);
			res = (uint)((res << 8) | buf[2]);
			res = (uint)((res << 8) | buf[3]);
			return res;
		}

		public static ulong NetworkToHostOrder(ulong num)
		{
			byte[] buf = BitConverter.GetBytes(num);
			ulong res = 0;
			res = buf[0];
			res = (ulong)((res << 8) | buf[1]);
			res = (ulong)((res << 8) | buf[2]);
			res = (ulong)((res << 8) | buf[3]);
			res = (ulong)((res << 8) | buf[4]);
			res = (ulong)((res << 8) | buf[5]);
			res = (ulong)((res << 8) | buf[6]);
			res = (ulong)((res << 8) | buf[7]);
			return res;
		}

		public static ushort BigEndianToUshort(byte[] buf, int index = 0)
		{
			ushort res = 0;
			res = buf[index];
			res = (ushort)((res << 8) | buf[index+1]);
			return res;
		}

		public static byte[] UshortToBigEndian(ushort number)
		{
			byte[] ret = new byte[2];
			ret[1] = (byte)(number & 0xFF);
			ret[0] = (byte)(number>>8 & 0xFF);
			return ret;
		}

		public static uint BigEndianToUint(byte[] buf)
		{
			uint res = 0;
			res = buf[0];
			res = (res << 8) | buf[1];
			res = (res << 8) | buf[2];
			res = (res << 8) | buf[3];
			return res;
		}

		public static byte[] UintToBigEndian(uint number)
		{
			byte[] ret = new byte[4];
			ret[3] = (byte)(number & 0xFF);
			ret[2] = (byte)(number >> 8 & 0xFF);
			ret[1] = (byte)(number >> 16 & 0xFF);
			ret[0] = (byte)(number >> 24 & 0xFF);
			return ret;
		}
	}


}

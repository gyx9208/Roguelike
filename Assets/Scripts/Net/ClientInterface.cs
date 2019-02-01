using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

/*
 *  2015/02/01
 *  xiao.liu@mihoyo.com
 *  
 *      定义MiClient需要实现的公共方法
 */

namespace Net
{
	/*
	 *  定义class MiClient提供给外部使用的接口
	 *  除了connect，其它方法都是线程安全的
	 */
	public interface ClientInterface
	{
		/*
		 *  功能: 建立到指定服务器的连接
		 *  
		 *  输入:
		 *      string host 服务器IP、或者域名(传入域名时，轮询尝试连接每个DNS解析到的IP)
		 *      ushort port 服务器端口
		 *      timeout_ms  socket超时时间(默认2000毫秒, 连接、发送数据时都会用到)
		 *      
		 *  输出:
		 *      成功返回true
		 */
		bool connect(string host, ushort port, int timeout_ms = 2000);

		/*
		 *  功能:主动断开连接(主动正常断开连接，不会调用设置的回调函数)
		 */
		void disconnect();

		/*
		 * 功能:判断连接状态
		 * 
		 * 输出:
		 *      连接正常时返回true，否则返回false
		 */
		bool isConnected();

		/*
		 *  功能: 同步阻塞的方式发送一个packet到服务器，最大阻塞时间等于connect()时传入的timeout
		 *  
		 *  输入:
		 *      NetPacketV1 packet  待发送的数据包
		 *      
		 *  输出:
		 *      成功返回true
		 */
		bool send(NetPacket packet);

		/*
		 *  功能: 从已收到的消息队列中，pop一个packet并返回
		 *        如果消息队列非空，即使连接已经断开，该方法仍然可以正常使用
		 *  
		 *  输入:
		 *      int timeout_ms  超时时间，当队列为空时，如果timeout=0则立刻返回null，否则一直阻塞到新消息到达或者超时
		 *      
		 *  输出: 
		 *      成功时返回一个实例，否则返回null
		 */
		NetPacket recv(int timeout_ms = 0);

		/*
		 * 功能: 设置连接意外断开时(网络故障、服务器主动关闭等)的回调函数, 由MiClient线程调用
		 * 
		 * 输入:
		 *      Callback callback   等于null时表示取消回调
		 *      
		 */
		void setDisconnectCallback(Action callback);

		/*
		 *  功能: 设置发往服务器的心跳包
		 *  
		 *  输入:
		 *      int time_ms    发送心跳包时间间隔
		 *      NetPacketV1 pakcet  心跳包内容
		 *      
		 *  输出:
		 *      成功时返回true
		 */
		bool setKeepalive(int time_ms, NetPacket packet);
	}
}

using System;
using UnityEngine;

namespace Fundamental
{
	public enum DebugPrefix
	{
		Default,
		Expression,
		Network
	}

	public static class SuperDebug
	{
		static DebugConfig Config;

		public static void LoadConfig()
		{
			Config = Resources.Load<DebugConfig>("DebugConfig");
		}

		public static bool Enable = true;

		public static bool[] Switch =
		{
			true,		// Default		0
			true,		// Expression	1
			true		// Network		2
		};

		public static void Assert(bool InCondition)
		{
			SuperDebug.Assert(InCondition, null, null);
		}

		public static void Assert(bool InCondition, string InFormat)
		{
			SuperDebug.Assert(InCondition, InFormat, null);
		}

		public static void Assert(bool InCondition, string InFormat, params object[] InParameters)
		{
			if (SuperDebug.Enable && !InCondition)
			{
				try
				{
					string text = null;
					if (!string.IsNullOrEmpty(InFormat))
					{
						try
						{
							if (InParameters != null)
							{
								text = string.Format(InFormat, InParameters);
							}
							else
							{
								text = InFormat;
							}
						}
						catch (Exception)
						{
						}
					}
					else
					{
						text = string.Format(" no assert detail, stacktrace is :{0}", Environment.StackTrace);
					}
					if (text != null)
					{
						string str = "Assert failed! " + text;
						Warning(str);
					}
					else
					{
						Warning("Assert failed!");
					}
				}
				catch (Exception)
				{
				}
			}
		}

		public static void AssertThrow(bool InCondition)
		{
			SuperDebug.Assert(InCondition, null, null);
		}

		public static void AssertThrow(bool InCondition, string InFormat)
		{
			SuperDebug.Assert(InCondition, InFormat, null);
		}

		public static void AssertThrow(bool InCondition, string InFormat, params object[] InParameters)
		{
			if (SuperDebug.Enable && !InCondition)
			{
				try
				{
					string text = null;
					if (!string.IsNullOrEmpty(InFormat))
					{
						try
						{
							if (InParameters != null)
							{
								text = string.Format(InFormat, InParameters);
							}
							else
							{
								text = InFormat;
							}
						}
						catch (Exception)
						{
						}
					}
					else
					{
						text = string.Format(" no assert detail, stacktrace is :{0}", Environment.StackTrace);
					}
					if (text != null)
					{
						string str = "Assert failed! " + text;
						throw new System.Exception(str);
					}
					else
					{
						throw new System.Exception("Assert failed!");
					}
				}
				catch (Exception)
				{
				}
			}
		}

		public static void Log(string str)
		{
			Log(DebugPrefix.Default, str);
		}

		public static void Error(string str)
		{
			Error(DebugPrefix.Default, str);
		}

		public static void Warning(string str)
		{
			Warning(DebugPrefix.Default, str);
		}

		public static void DrawLine(DebugPrefix type, Vector3 start, Vector3 end, Color? color = null, float duration = 1.0f, bool depthTest = true)
		{
			if (Check(type))
			{
				Debug.DrawLine(start, end, color ?? Color.blue, duration, depthTest);
			}
		}

		public static void Log(DebugPrefix type, string str)
		{
			if (Check(type))
			{
				str = type + DateTime.Now.ToString(": yyyyMMdd_HHmmss \r\n") + str;
				Debug.Log(str);
			}
		}

		public static void Warning(DebugPrefix type, string str)
		{
			if (Check(type))
			{
				str = type + DateTime.Now.ToString(": yyyyMMdd_HHmmss \r\n") + str;
				Debug.LogWarning(str);
			}
		}

		public static void Error(DebugPrefix type, string str)
		{
			if (Check(type))
			{
				str = type + DateTime.Now.ToString(": yyyyMMdd_HHmmss \r\n") + str;
				Debug.LogError(str);
			}
		}

		static bool Check(DebugPrefix type)
		{
			int index = (int)type;
			if (Config != null)
			{
				if (Config.Enable && Config.Switch[index])
					return true;
			}
			else
			{
				if (Enable && Switch[index])
					return true;
			}
			return false;
		}
	}
}
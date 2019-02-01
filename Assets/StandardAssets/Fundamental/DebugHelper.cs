using System;
using UnityEngine;

namespace Fundamental
{

	public static class SuperDebug
	{
		public static bool enableLog = true;

		public static bool[] DEBUG_SWITCH =
		{
			true,		// Default		0
			true		// Expression	1
	};

		public static string[] LOG_PREFIX =
		{
			"DEFAULT:",
			"EXPRESSION:"
	};

		public const int DEFAULT = 0;
		public const int EXPRESSION = 1;

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
			if (SuperDebug.enableLog && !InCondition)
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
			if (SuperDebug.enableLog && !InCondition)
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

		public static void Log(string str, params object[] InParameters)
		{
			if (SuperDebug.enableLog)
			{
				try
				{
					if (InParameters != null)
					{
						str = string.Format(str, InParameters);
					}
					str = DateTime.Now.ToString("yyyyMMdd_HHmmss \r\n") + str;
					Debug.Log(str);
				}
				catch (Exception)
				{
				}
			}
		}

		public static void Log(string str)
		{
			SuperDebug.Log(str, null);
		}

		public static void LogError(int type, string str)
		{
			if (DEBUG_SWITCH[type])
			{
				Error(str);
			}
		}

		public static void Error(string str)
		{
			Debug.LogError(str);
		}

		public static void LogWarning(int type, string str)
		{
			if (DEBUG_SWITCH[type])
			{
				Warning(str);
			}
		}

		public static void Warning(string str)
		{
			Debug.LogWarning(str);
		}

		public static void DrawLine(int type, Vector3 start, Vector3 end, Color? color = null, float duration = 1.0f, bool depthTest = true)
		{
			if (DEBUG_SWITCH[type])
			{
				Debug.DrawLine(start, end, color ?? Color.blue, duration, depthTest);
			}
		}

		public static void Log(int type, string str)
		{
			if (DEBUG_SWITCH[type])
			{
				Log(str);
			}
		}
	}
}
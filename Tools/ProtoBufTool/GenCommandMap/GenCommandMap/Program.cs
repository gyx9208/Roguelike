using System;
using System.Collections.Generic;
using System.IO;

namespace GenCommandMap
{
	class Program
	{
		static void Main(string[] args)
		{
			List<string> classNameList = new List<string>();
			HashSet<string> nameSpaceSet = new HashSet<string>();

			string strLine;
			bool findNameSpace = false;

			string outDir = Environment.CurrentDirectory;

			for (int i = 0; i < args.Length; i++)
			{
				if (args[i].StartsWith("--csharp_out"))
				{
					var p = args[i].Split('=');
					outDir = Path.GetFullPath(p[1]);
				}
			}
			outDir = outDir + "/CommandMap.cs";

			string[] filePaths = Directory.GetFiles(System.Environment.CurrentDirectory);

			foreach (string path in filePaths)
			{
				if (!path.EndsWith(".proto") || path.Contains("meta")) continue;
				findNameSpace = false;

				try
				{
					FileStream aFile = new FileStream(path, FileMode.Open);
					StreamReader sr = new StreamReader(aFile);
					strLine = sr.ReadLine();

					while (strLine != null)
					{
						System.Text.RegularExpressions.Regex regex = new System.Text.RegularExpressions.Regex("message (.*){");

						if (regex.IsMatch(strLine))
						{
							string classname = regex.Match(strLine).Groups[1].Value.Trim();
							strLine = sr.ReadLine();

							if (strLine.Contains("enum CmdId"))
							{
								classNameList.Add(classname);
							}
						}

						strLine = sr.ReadLine();

						//	nameSpace
						if (!findNameSpace && strLine.Contains("package"))
						{
							string nameSpace = strLine.Substring("package ".Length);
							if (!nameSpaceSet.Contains(nameSpace))
							{
								nameSpaceSet.Add(nameSpace);
							}
							findNameSpace = true;
						}
					}
					sr.Close();
				}
				catch (IOException ex)
				{
					Console.WriteLine("An IOException has been thrown!");
					Console.WriteLine(ex.ToString());
					Console.ReadLine();
					return;
				}

			}

			try
			{
				FileStream aFile = new FileStream(outDir, FileMode.Create);
				StreamWriter sw = new StreamWriter(aFile);

				// Write data to file.
				sw.Write(GetOutputString(classNameList, nameSpaceSet));
				sw.Close();
			}
			catch (IOException ex)
			{
				Console.WriteLine("An IOException has been thrown!");
				Console.WriteLine(ex.ToString());
				Console.ReadLine();
				return;
			}
		}

		private static string GetOutputString(List<string> classNameList, HashSet<string> nameSpaceSet)
		{
			string itemOutput =
@"using System.Collections.Generic;
using System;
using Fundamental;
#usingList
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
			
#paramList

			return cmdMap;
		}

		public ushort GetCmdIDByType(Type type)
		{
			ushort resCmdID;
			if (!_typeMap.TryGetValue(type, out resCmdID))
			{
				SuperDebug.Warning(""undefined type="" + type);
			}

			return resCmdID;
		}

		public Type GetTypeByCmdID(ushort cmdID)
		{
			Type resType;
			if (!_cmdIDMap.TryGetValue(cmdID, out resType))
			{
				SuperDebug.Warning(""undefined cmdID="" + cmdID);
			}

			return resType;
		}
	}
}";

			string usingListStr = "";
			foreach (string nameSpace in nameSpaceSet)
			{
				usingListStr += string.Format("using {0}\r\n", nameSpace);
			}
			itemOutput = itemOutput.Replace("#usingList", usingListStr);

			string paramListStr = "";
			foreach (string className in classNameList)
			{
				paramListStr += string.Format("\t\t\tcmdMap.Add((ushort){0}.CmdId.CmdId, typeof({0}));\r\n", className);
			}

			itemOutput = itemOutput.Replace("#paramList", paramListStr);
			return itemOutput;
		}
	}
}

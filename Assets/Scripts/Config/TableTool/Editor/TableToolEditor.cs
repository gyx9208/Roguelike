using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System;
using System.IO;


namespace Config.TableTools
{
	public class TableToolEditor : EditorWindow
	{
		protected static TableToolDatas _tableDatas;

		protected string _searchFieldText = "";

		private Vector2 _scrollPosition = Vector2.zero;

		[MenuItem("miHoYo/Utils/表格代码生成器 &r")]
		static void Init()
		{
			LoadAsset();

			TableToolEditor window = (TableToolEditor)EditorWindow.GetWindow(typeof(TableToolEditor));
			window.Show();
		}

		void OnInspectorUpdate()
		{
			Repaint();
		}

		/// <summary>  
		/// 显示战斗数据 
		/// </summary>  
		void OnGUI()
		{
			if (_tableDatas == null)
			{
				LoadAsset();
				return;
			}

			try
			{
				GUILayout.BeginHorizontal();
				GUILayout.Label("搜索:", GUILayout.Width(30));
				_searchFieldText = GUILayout.TextField(_searchFieldText);
				string[] searchList = _searchFieldText.Split(' ');

				GUILayout.EndHorizontal();
				EditorGUILayout.Space();

				_scrollPosition = GUILayout.BeginScrollView(_scrollPosition, GUIStyle.none);

				//	每个配置	//
				TableToolDatas.TableToolsData tableData;
				for (int i = 0; i < _tableDatas.TableDatas.Count; i++)
				{
					tableData = _tableDatas.TableDatas[i];

					//	搜索筛选	//
					bool find = true;
					for (int j = 0; j < searchList.Length; j++)
					{
						if (string.IsNullOrEmpty(_searchFieldText)) break;
						if (!tableData.TablePath.ToLower().Contains(searchList[j]))
						{
							find = false;
							break;
						}
					}
					if (!find) continue;

					TextAsset tempTable = null;
					tempTable = EditorGUILayout.ObjectField("表格:", tempTable, typeof(TextAsset), false) as TextAsset;
					if (tempTable != null)
					{
						string txtPath1 = AssetDatabase.GetAssetPath(tempTable);
						txtPath1 = txtPath1.Substring(txtPath1.IndexOf("/Data/") + 1);
						tableData.TablePath = txtPath1.Substring(0, txtPath1.IndexOf('.'));
					}
					EditorGUILayout.LabelField("Table path:", tableData.TablePath);

					TextAsset tempCS = null;
					tempCS = EditorGUILayout.ObjectField("目标CS文件:", tempCS, typeof(TextAsset), false) as TextAsset;
					if (tempCS != null)
					{
						tableData.ClassPath = AssetDatabase.GetAssetPath(tempCS);
						tableData.ClassName = Path.GetFileNameWithoutExtension(tableData.ClassPath);
					}
					EditorGUILayout.LabelField("CS path:", tableData.ClassPath);

					GUILayout.BeginHorizontal();
					#region //	生成按钮	//
					if (GUILayout.Button("生成", GUILayout.Width(140)))
					{
						if (string.IsNullOrEmpty(tableData.TablePath))
						{
							Debug.Log("先把Txt表加上来吧!");
							return;
						}
						if (string.IsNullOrEmpty(tableData.ClassPath))
						{
							Debug.Log("先在想要的目录创建好数据结构的CS文件然后拖上来后才能生成代码!");
							return;
						}
						CreateClassFile(tableData);
						SaveAsset(_tableDatas);
					}
					#endregion
					#region //	删除按钮	//
					if (GUILayout.Button("删除", GUILayout.Width(140)))
					{
						_tableDatas.TableDatas.RemoveAt(i);
						SaveAsset(_tableDatas);
					}
					#endregion
					GUILayout.EndHorizontal();

					EditorGUILayout.Space();
					GUILayout.Label("--------------------------------------------------------------------------------", GUILayout.Width(300));
					EditorGUILayout.Space();
				}
				GUILayout.EndScrollView();
				EditorGUILayout.Space();

				#region //	按钮	//
				if (GUILayout.Button("新增"))
				{
					TableToolDatas.TableToolsData data = new TableToolDatas.TableToolsData();
					_tableDatas.TableDatas.Add(data);
					SaveAsset(_tableDatas);
				}
				if (GUILayout.Button("重新生成所有"))
				{
					for (int i = 0; i < _tableDatas.TableDatas.Count; i++)
					{
						tableData = _tableDatas.TableDatas[i];
						if (string.IsNullOrEmpty(tableData.TablePath))
						{
							Debug.Log("请检查所有Txt表位置!");
							return;
						}
						if (string.IsNullOrEmpty(tableData.ClassPath))
						{
							Debug.Log("先在想要的目录创建好数据结构的CS文件然后拖上来后才能生成代码!");
							return;
						}
						CreateClassFile(tableData);
					}
					SaveAsset(_tableDatas);
				}
				if (GUILayout.Button("检查数据（每次更改数据后必查）"))
				{
					CheckAllData();
				}
				if (GUILayout.Button("保存"))
				{
					SaveAsset(_tableDatas);
				}
				#endregion
				EditorGUILayout.Space();
			}
			catch (System.Exception) { }
		}

		private void CheckAllData()
		{
			for (int i = 0; i < _tableDatas.TableDatas.Count; i++)
			{
				CheckData(i, _tableDatas.TableDatas[i]);
			}
			#region check specific project files
			/*
			var skills = TableTool.LoadTable<SkillData>();
			for(int i = 0; i < skills.Length; i++)
			{
				try
				{
					ExpressionParser.Instance.EvaluateHkmmExpression(skills[i].attackFormula);
				}
				catch
				{
					Debug.Log(string.Format("skill[{0}]的attack formula有问题", skills[i].id));
				}
				var _ActionSet = ResourceManager.Instance.GetResData<ActionSetConfig>(InLevelData.SKILL_LOGIC_PATH + skills[i].skillLogic);
				if (_ActionSet == null && !string.IsNullOrEmpty(skills[i].skillLogic))
					Debug.Log(string.Format("skill[{0}]的action logic找不到", skills[i].id));
			}*/
			#endregion

			Debug.Log("检查完成");
		}

		private void CheckData(int index, TableToolDatas.TableToolsData tableData)
		{
			string path = tableData.ClassPath;
			tableData.Cs = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
			tableData.Table = Resources.Load<TextAsset>(tableData.TablePath);

			if (tableData.Cs == null)
			{
				Debug.Log(string.Format("INDEX[{0}] 表[{1}] 的cs文件不存在", index, tableData.ClassName));
				return;
			}

			if(tableData.Table == null)
			{
				Debug.Log(string.Format("INDEX[{0}] 表[{1}] 的数值文件不存在,或不在Resources目录下", index, tableData.ClassName));
				return;
			}

			var _lines = tableData.Table.text.SplitLine();

			string[] locate = _lines[TableTool.PARAM_LOCATE].SplitStringByTab();
			var _needRead = new List<int>();
			for (int i = 0; i < locate.Length; i++)
			{
				if (locate[i].ToLower().Contains(TableTool.CLIENT_MARK))
				{
					_needRead.Add(i);
				}
			}

			if (tableData.ConstructorTypes == null || tableData.ConstructorTypes.Count != _needRead.Count)
			{
				Debug.Log(string.Format("INDEX[{0}] 表[{1}] 的表结构有所更改，需要重新生成", index, tableData.ClassName));
			}


			for (int i = TableTool.PARAM_COUNT; i < _lines.Length; i++)
			{
				var paramList = new object[_needRead.Count];

				string[] words = _lines[i].SplitStringByTab();
				for (int j = 0; j < _needRead.Count; j++)
				{
					try
					{
						paramList[j] = TableTool.ParseExcelType(words[_needRead[j]], tableData.ConstructorTypes[j]);
					}
					catch
					{
						Debug.Log(string.Format("INDEX[{0}] 表[{1}] 行[{2}]列[{3}] 的数据有问题", index, tableData.ClassName, i, j));
					}
				}
			}
		}

		/// <summary>
		/// 读取本地序列化文件
		/// </summary>
		private static void LoadAsset()
		{
			_tableDatas = AssetDatabase.LoadAssetAtPath<TableToolDatas>(TableTool.ASSET_PATH);
		}

		/// <summary>
		/// 保存配置文件
		/// </summary>
		public static void SaveAsset(TableToolDatas tableDatas)
		{
			//if (tableDatas == null || tableDatas.tableDatas.Count == 0) return;
			//AssetDatabase.DeleteAsset(TableTools.ASSET_PATH);
			//AssetDatabase.CreateAsset(tableDatas, TableTools.ASSET_PATH);

			EditorUtility.SetDirty(_tableDatas);
			AssetDatabase.SaveAssets();
		}

		/// <summary>
		/// 生成数据结构文件
		/// </summary>
		protected void CreateClassFile(TableToolDatas.TableToolsData tableData)
		{
			//	保存txt文件信息	//
			string path = tableData.ClassPath;
			tableData.Cs = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
			tableData.Table = Resources.Load<TextAsset>(tableData.TablePath);

			//	手动添加部分	//
			List<List<string>> addedText = ReadAddedText(tableData.Cs);
			bool hasInit = tableData.Cs.text.Contains("void Init()");

			string content = "";
			List<string> paramNames = null;
			List<string> paramTypes = null;
			List<string> paramDescribes = null;

			List<List<string>> excelData = ReadExcel(tableData);
			LoadExcelFormat(excelData, out paramNames, out paramTypes, out paramDescribes);

			tableData.ConstructorTypes = paramTypes;
			content = CreateClassStr(tableData, paramNames, paramTypes, paramDescribes, addedText, hasInit);

			//创建文件
			StreamWriter sw;
			FileInfo t = new FileInfo(path);
			if (!t.Exists)
			{
				sw = t.CreateText();
			}
			else
			{
				sw = t.CreateText();
				sw = new StreamWriter(sw.BaseStream, new System.Text.UTF8Encoding(false));
			}

			sw.Write(content);
			sw.Close();
			sw.Dispose();
			Debug.Log("Create" + path + "file success!");
		}

		/// <summary>
		/// 读取cs文件中手动添加的代码
		/// </summary>
		protected List<List<string>> ReadAddedText(TextAsset textAsset)
		{
			List<List<string>> result = new List<List<string>>();

			string text0 = textAsset.text;
			int startRegion = 0;
			List<string> region = null;
			string[] line = text0.Split('\n');

			for (int i = 0; i < line.Length; i++)
			{
				//	开始	//
				if (startRegion == 0 && line[i].Contains("#region"))
				{
					string regionBefore = line[i].Substring(0, line[i].IndexOf("#region"));
					if (!regionBefore.Contains("//") && !regionBefore.Contains("/*"))
					{
						startRegion++;
						region = new List<string>();
						result.Add(region);
					}
				}

				if (startRegion > 0)
				{
					string addText = line[i].Replace("\r", "\n");
					if (!addText.Contains("\n"))
					{
						addText += '\n';
					}
					region.Add(addText);
					if (line[i].Contains("#endregion"))
					{
						startRegion--;
					}
				}
			}

			return result;
		}

		public void OnDestroy()
		{
			SaveAsset(_tableDatas);
		}

		#region Excel
		/// <summary>
		/// 读Excel表的前四行
		/// </summary>
		protected List<List<string>> ReadExcel(TableToolDatas.TableToolsData tableData)
		{
			List<List<string>> result = new List<List<string>>();

			string[] lines = tableData.Table.text.SplitLine();

			for (int i = 0; i < 4; i++)
			{
				result.Add(new List<string>());
				string[] words = lines[i].SplitStringByTab();
				foreach (string word in words)
				{
					if (word == "\r") continue;

					result[result.Count - 1].Add(word);
				}
			}
			return result;
		}

		/// <summary>
		/// 处理表格编码
		/// </summary>
		/// <param name="tableData"></param>
		protected string CheckFileEncoding(TableToolDatas.TableToolsData tableData)
		{
			string tablePath = AssetDatabase.GetAssetPath(tableData.Table);
			System.IO.FileStream fs = new System.IO.FileStream(tablePath, System.IO.FileMode.Open, System.IO.FileAccess.Read);
			System.IO.BinaryReader br = new System.IO.BinaryReader(fs);
			Byte[] buffer = br.ReadBytes(2);
			System.Text.Encoding encoding = null;
			if (buffer[0] >= 0xEF)
			{
				if (buffer[0] == 0xEF && buffer[1] == 0xBB)
				{
					encoding = System.Text.Encoding.UTF8;
				}
			}

			if (encoding != System.Text.Encoding.UTF8)
			{
				//	强转	//
				fs = new System.IO.FileStream(tablePath, System.IO.FileMode.Open, System.IO.FileAccess.Read);
				br = new System.IO.BinaryReader(fs);
				buffer = br.ReadBytes((int)fs.Length);
				//buffer = new byte[(int)fs.Length];
				//br.Read(buffer, 0, (int)fs.Length);
				System.Text.Encoding utf8 = System.Text.Encoding.UTF8;
				System.Text.Encoding gb2312 = System.Text.Encoding.GetEncoding("GB2312");
				byte[] asciiBytes = System.Text.Encoding.Convert(gb2312, utf8, buffer);
				char[] asciiChars = new char[utf8.GetCharCount(asciiBytes, 0, asciiBytes.Length)];
				utf8.GetChars(asciiBytes, 0, asciiBytes.Length, asciiChars, 0);
				string result = new string(asciiChars);

				br.Close();
				fs.Close();
				return result;
			}
			return null;
		}

		private string GB2312ToUTF8(string str)
		{
			try
			{
				System.Text.Encoding utf8 = System.Text.Encoding.UTF8;
				System.Text.Encoding gb2312 = System.Text.Encoding.GetEncoding("GB2312");
				byte[] unicodeBytes = gb2312.GetBytes(str);
				byte[] asciiBytes = System.Text.Encoding.Convert(gb2312, utf8, unicodeBytes);
				char[] asciiChars = new char[utf8.GetCharCount(asciiBytes, 0, asciiBytes.Length)];
				utf8.GetChars(asciiBytes, 0, asciiBytes.Length, asciiChars, 0);
				string result = new string(asciiChars);
				return result;
			}
			catch
			{
				return "";
			}
		}

		/// <summary>
		/// 创建数据结构的字符串
		/// </summary>
		protected string CreateClassStr(TableToolDatas.TableToolsData tableData, List<string> paramNames,
			List<string> paramTypes, List<string> paramDescribes, List<List<string>> addedText, bool hasInit)
		{
			System.Text.StringBuilder str = new System.Text.StringBuilder();
			str.Append("using MoleMole.TableTool;\n");
			str.Append("using System.Collections.Generic;\n");
			str.Append("using UnityEngine;\n");
			str.Append("\n");
			str.Append("// 此类的代码由TableToolsEditor插件生成，请勿修改代码！\n");
			str.Append("// 不过#region中的代码将会在生成后保留！\n");
			str.Append("// By XiaoZeFeng Tools---TableTool.cs\n");
			str.Append("\n");
			str.Append("namespace MoleMole\n");
			str.Append("{\n");

			string className = tableData.ClassName;
			str.Append("\tpublic class " + className + " : ITableToolData\n");
			str.Append("\t{\n");

			//	参数	//
			for (int i = 0; i < paramNames.Count; i++)
			{
				str.Append("\t\tpublic " + GetTrueType(paramTypes[i]) + " " + paramNames[i] + ";");

				if (paramDescribes != null && paramDescribes.Count > i && paramDescribes[i] != "")
				{
					str.Append("\t//\t" + paramDescribes[i] + "\t//\n");
				}
				else
				{
					str.Append("\n");
				}
			}
			str.Append("\n");

			//	添加数据	//
			str.Append("\t\tpublic void SetData(params object[] param)\n");
			str.Append("\t\t{\n");

			for (int i = 0; i < paramNames.Count; i++)
			{
				str.Append("\t\t\tthis." + paramNames[i] + " = (" + GetTrueType(paramTypes[i]) + ")param[" + i + "];\n");
			}
			if (hasInit)
			{
				str.Append("\t\t\tInit();\n");
			}
			str.Append("\t\t}\n\n");

			//	手动编辑部分	//
			if (addedText == null || addedText.Count == 0)
			{
				str.Append("\t\t#region //\t手动编辑部分（可在重新生成代码后保留）\t//\n\t\t#endregion\n");
			}
			else
			{
				for (int i = 0; i < addedText.Count; i++)
				{
					for (int j = 0; j < addedText[i].Count; j++)
					{
						str.Append(addedText[i][j]);
					}
				}
			}

			str.Append("\t}\n");
			str.Append("}\n");

			return str.ToString();
		}

		protected string GetTrueType(string type)
		{
			switch (type)
			{
				case TableTool.NUMBER:
					return "int";
				case TableTool.LNUMBER:
					return "List<int>";
				default:
					return type;
			}
		}

		/// <summary>
		/// 收集Excel表的参数名和类型
		/// </summary>
		protected void LoadExcelFormat(List<List<string>> excelData, out List<string> paramNames, out List<string> paramTypes, out List<string> paramDescribes)
		{
			paramNames = new List<string>();
			paramTypes = new List<string>();
			paramDescribes = new List<string>();

			for (int i = 0; i < excelData[0].Count; i++)
			{
				string paramName = excelData[TableTool.PARAM_NAME][i].Trim();
				string paramDescribe = excelData[TableTool.PARAM_DES][i].Trim();
				string paramLocate = excelData[TableTool.PARAM_LOCATE][i].Trim().ToLower();
				string paramType = excelData[TableTool.PARAM_TYPE][i].Trim().ToLower();

				if (paramLocate.Contains(TableTool.INT) || paramLocate.Contains(TableTool.FLOAT) || paramLocate.Contains(TableTool.BOOL) || paramLocate.Contains(TableTool.STRING))
				{
					Debug.LogWarning("表格格式可能存在错误！正确格式：第一行是参数名，第二行是解释，第三行是客户端是否读，第四行是参数类型！");
				}
				if (paramName == "" || paramName == "\r" || paramName == "\n" || paramName == "\t" || !paramLocate.Contains(TableTool.CLIENT_MARK))
					continue;
				if (paramType.Contains("list"))
				{
					paramType = 'L' + paramType.Substring(1);
				}
				paramNames.Add(paramName);
				paramTypes.Add(paramType);
				paramDescribes.Add(paramDescribe);
			}
		}
		#endregion
	}
}
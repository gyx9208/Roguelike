using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Config.TableTools
{
	public class LoadTableList<T> : IEnumerator where T : ITableToolData, new()
	{
		bool IsDone = false;
		public T[] Result;
		public event Action<LoadTableList<T>> Completed;

		public object Current
		{
			get
			{
				return Result;
			}
		}

		public bool MoveNext()
		{
			return !IsDone;
		}

		public void Reset()
		{
		}

		public LoadTableList(string path)
		{
			var load = Addressables.LoadAsset<TextAsset>(path);
			load.Completed += LoadCompleted;
		}

		private void LoadCompleted(IAsyncOperation<TextAsset> obj)
		{
			var text = obj.Result.text;
			Result = TableTool.LoadTable<T>(text);
			IsDone = true;
			Completed?.Invoke(this);
			Addressables.ReleaseAsset(obj.Result);
		}
	}

	public class LoadTableDic<TK, T> : IEnumerator where T : ITableToolData, new()
	{
		bool IsDone = false;
		public Dictionary<TK, T> Result;
		public event Action<LoadTableDic<TK, T>> Completed;

		public object Current
		{
			get
			{
				return Result;
			}
		}

		public bool MoveNext()
		{
			return !IsDone;
		}

		public void Reset()
		{
		}

		public LoadTableDic(string path)
		{
			var load = Addressables.LoadAsset<TextAsset>(path);
			load.Completed += LoadCompleted;
		}

		private void LoadCompleted(IAsyncOperation<TextAsset> obj)
		{
			var text = obj.Result.text;
			Result = TableTool.LoadDictionary<TK, T>(text);
			IsDone = true;
			Completed?.Invoke(this);
			Addressables.ReleaseAsset(obj.Result);
		}
	}

	public interface ITableToolData
	{
		void SetData(params object[] param);
	}

	public static class TableTool
	{
		public const string TAB = "\t";
		public const string LINE_FEED = "\n";
		public const string CARRIAGE_RETURN = "\r";
		public const string WINDOWS_ENTER = "\r\n";

		public static readonly string[] STRING_SPLITS = new string[3] { ";", "；", "|" };
		public static readonly string[] STRING_SUB_SPLITS = new string[1] { ","};
		public static readonly string[] LINE_SPLITS = new string[3] { WINDOWS_ENTER, CARRIAGE_RETURN, LINE_FEED };
		public static readonly string[] TAB_SPLITS = new string[1] { TAB };

		public const string RESOURCES_ASSET_PATH = "Data/TableToolsAsset";
		public const string ASSET_PATH = "Assets/Resources/" + RESOURCES_ASSET_PATH + ".asset";

		public const string CLIENT_MARK = "client";
		public const string
			INT = "int",
			STRING = "string",
			BOOL = "bool",
			FLOAT = "float",
			LINT = "List<int>",
			NUMBER = "number",
			LSTRING = "List<string>",
			LBOOL = "List<bool>",
			LFLOAT = "List<float>",
			LNUMBER = "List<number>";

		public const int PARAM_NAME = 0;
		public const int PARAM_DES = 1;
		public const int PARAM_LOCATE = 2;
		public const int PARAM_TYPE = 3;
		public const int PARAM_COUNT = 4;

		private static TableToolDatas _tableDatas;

		private static string[] _lines;
		private static List<int> _needRead;
		private static TextAsset _textAsset;
		private static TableToolDatas.TableToolsData _tableData;

		delegate void Onload(int index, object[] data);

		private static void LoadData()
		{
			if (_tableDatas == null)
			{
				_tableDatas = Resources.Load<TableToolDatas>(RESOURCES_ASSET_PATH);
			}
		}

		public static T[] LoadTable<T>(string text) where T : ITableToolData, new()
		{
			PrepareForLoad<T>(text);

			T[] list = new T[_lines.Length - PARAM_COUNT];
			Onload onload = (a, b) =>
			{
				list[a] = new T();
				list[a].SetData(b);
			};
			Load(onload);
			ReleaseData();
			return list;
		}

		public static Dictionary<TK, T> LoadDictionary<TK, T>(string text) where T : ITableToolData, new()
		{
			PrepareForLoad<T>(text);
			Dictionary<TK, T> dic = new Dictionary<TK, T>();
			Onload onload = (a, b) =>
			{
				T t = new T();
				t.SetData(b);
				dic[(TK)b[0]] = t;
			};
			Load(onload);
			ReleaseData();
			return dic;
		}

		private static void PrepareForLoad<T>(string text)
		{
			TableTool.LoadData();

			//	找到记录的数据	//
			int i = 0;
			var name = typeof(T).Name;
			for (i = 0; i < _tableDatas.TableDatas.Count; i++)
			{
				if (_tableDatas.TableDatas[i].ClassName.Equals(name))
				{
					_tableData = _tableDatas.TableDatas[i];
					break;
				}
			}

			if (_tableData == null)
			{
				throw new System.Exception("Can't find Excel File for " + name);
			}

			_lines = text.SplitLine();

			//	筛选要读的列	//
			string[] locate = _lines[PARAM_LOCATE].SplitStringByTab();
			_needRead = new List<int>();
			for (i = 0; i < locate.Length; i++)
			{
				if (locate[i].ToLower().Contains(CLIENT_MARK))
				{
					_needRead.Add(i);
				}
			}
		}

		private static void Load(Onload onload)
		{
			int i = 0, j = 0;
			object[] paramList;

			for (i = PARAM_COUNT; i < _lines.Length; i++)
			{
				paramList = new object[_needRead.Count];

				string[] words = _lines[i].SplitStringByTab();
				for (j = 0; j < _needRead.Count; j++)
				{
					paramList[j] = ParseExcelType(words[_needRead[j]], _tableData.ConstructorTypes[j]);
				}

				onload(i - PARAM_COUNT, paramList);
			}
		}

		private static void ReleaseData()
		{
			_lines = null;
			_needRead = null;
			_textAsset = null;
			_tableData = null;
		}

		public static object ParseExcelType(string str, string type)
		{
			if (INT.Equals(type))
			{
				if (string.IsNullOrEmpty(str))
					return 0;
				return int.Parse(str);
			}
			else if (NUMBER.Equals(type))
			{
				if (string.IsNullOrEmpty(str))
					return 0;
				return InLevelData.ParseInLevelInt(str);
			}
			else if (FLOAT.Equals(type))
			{
				if (string.IsNullOrEmpty(str))
					return 0;
				return float.Parse(str);
			}
			else if (STRING.Equals(type))
			{
				return str;
			}
			else if (BOOL.Equals(type))
			{
				return "true".Equals(str.ToLower());
			}
			else if (LINT.Equals(type))
			{
				string[] p = str.SplitStringBySplits();
				List<int> result = new List<int>(p.Length);
				for (int i = 0; i < p.Length; i++)
				{
					if (string.IsNullOrEmpty(str))
						result.Add(0);
					else
						result.Add(int.Parse(p[i]));
				}
				return result;
			}
			else if (LNUMBER.Equals(type))
			{
				string[] p = str.SplitStringBySplits();
				List<int> result = new List<int>(p.Length);
				for (int i = 0; i < p.Length; i++)
				{
					if (string.IsNullOrEmpty(str))
						result.Add(0);
					else
						result.Add(InLevelData.ParseInLevelInt(p[i]));
				}
				return result;
			}
			else if (LFLOAT.Equals(type))
			{
				string[] p = str.SplitStringBySplits();
				List<float> result = new List<float>(p.Length);
				for (int i = 0; i < p.Length; i++)
				{
					if (string.IsNullOrEmpty(str))
						result.Add(0);
					else
						result.Add(float.Parse(p[i]));
				}
				return result;
			}
			else if (LSTRING.Equals(type))
			{
				string[] p = str.SplitStringBySplits();
				List<string> result = new List<string>(p.Length);
				for (int i = 0; i < p.Length; i++)
				{
					result.Add(p[i]);
				}
				return result;
			}
			else if (LBOOL.Equals(type))
			{
				string[] p = str.SplitStringBySplits();
				List<bool> result = new List<bool>(p.Length);
				for (int i = 0; i < p.Length; i++)
				{
					result.Add("true".Equals(str));
				}
				return result;
			}
			return null;
		}

		public static string[] SplitStringBySplits(this string str)
		{
			return str.Split(STRING_SPLITS, System.StringSplitOptions.RemoveEmptyEntries);
		}

		public static string[] SplitStringByTab(this string str)
		{
			return str.Split(TAB_SPLITS, System.StringSplitOptions.None);
		}

		public static string[] SplitLine(this string str)
		{
			return str.Split(LINE_SPLITS, System.StringSplitOptions.RemoveEmptyEntries);
		}
	}
}
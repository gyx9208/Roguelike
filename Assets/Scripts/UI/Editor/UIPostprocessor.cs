using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace UI
{
	public class UIPostprocessor : AssetPostprocessor
	{
		private static string _uiPrefabFile = "Resources/UI";
		private static string _commonUIDataTempPath = "Assets/Scripts/UI/Editor/CommonUIData.byte";
		private static string _ui_max_key = "UI_MAX";
		private static string _ui_none_key = "UI_NONE";

		void OnPreprocessTexture()
		{
			TextureImporter ti = TextureImporter.GetAtPath(assetPath) as TextureImporter;
			ti.mipmapEnabled = false;

			if (ti != null)
			{
				if (assetPath.Contains("Assets/OriginalResRepos/Sprite/"))
				{
					ti.textureType = TextureImporterType.Sprite;
					ti.spriteImportMode = SpriteImportMode.Single;
					ti.spritePixelsPerUnit = 1;
					//ti.textureFormat = TextureImporterFormat.AutomaticTruecolor;
					//ti.maxTextureSize = 1024;
				}
			}


		}

		static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
		{
			foreach (string str in importedAssets)
			{
				OnImportMonoUIAsset(str);
			}

			foreach (string str in deletedAssets)
			{
				OnDeleteMonoUIAsset(str);
			}

			foreach (string str in movedFromAssetPaths)
			{
				OnDeleteMonoUIAsset(str);
			}

			foreach (string str in movedAssets)
			{
				OnMoveMonoUIAsset(str);
			}

			AssetDatabase.Refresh();
		}

		static void OnImportMonoUIAsset(string assetPath)
		{
			if (!assetPath.Contains(_uiPrefabFile) || !assetPath.Contains(".prefab"))
				return;

			Debug.Log("OnImportMonoUIAsset:" + assetPath);
			GameObject uiPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
			if (uiPrefab == null)
				return;

			BaseUIView baseUIView = uiPrefab.GetComponent<BaseUIView>();
			if (baseUIView == null)
				return;

			string uiType = GetUITypeByUIPrefabName(uiPrefab.name);
			string uiPath = assetPath.Replace("Assets/Resources/", "").Replace(".prefab", "");
			List<string> names = new List<string>();
			Dictionary<string, string> dictUIPaths = new Dictionary<string, string>();
			LoadCacheCommonUIData(names, dictUIPaths);

			if (!names.Contains(_ui_none_key))
			{
				names.Add(_ui_none_key);
				names.Add(_ui_max_key);

				dictUIPaths.Add(_ui_none_key, "null");
				dictUIPaths.Add(_ui_max_key, "null");
			}

			if (!names.Contains(uiType))
			{
				int max = names.Count - 1;
				names.Insert(max, uiType);
				dictUIPaths.Add(uiType, uiPath);

				AutoCreateCommonUIDataCS(names, dictUIPaths);
				SaveCacheCommonUIData(names, dictUIPaths);

				PrefabUtility.SetPropertyModifications(uiPrefab, new PropertyModification[0]);
				AssetDatabase.SaveAssets();
			}
		}

		static void OnDeleteMonoUIAsset(string assetPath)
		{
			if (!assetPath.Contains(_uiPrefabFile) || !assetPath.Contains(".prefab"))
				return;

			Debug.Log("OnDeleteMonoUIAsset:" + assetPath);
			string uiType = GetUITypeByUIPrefabName(assetPath.Substring(assetPath.LastIndexOf('/') + 1).Replace(".prefab", ""));
			string uiPath = assetPath.Replace("Assets/Resources/", "").Replace(".prefab", "");
			List<string> names = new List<string>();
			Dictionary<string, string> dictUIPaths = new Dictionary<string, string>();
			LoadCacheCommonUIData(names, dictUIPaths);
			if (names.Contains(uiType) && dictUIPaths[uiType] == uiPath)
			{
				names.Remove(uiType);
				dictUIPaths.Remove(uiType);

				AutoCreateCommonUIDataCS(names, dictUIPaths);
				SaveCacheCommonUIData(names, dictUIPaths);
			}

			string uiassetPath = assetPath.Replace("Resources", "Resources").Replace(".prefab", ".asset");
			AssetDatabase.DeleteAsset(uiassetPath);
		}

		static void OnMoveMonoUIAsset(string assetPath)
		{
			if (!assetPath.Contains(_uiPrefabFile) || !assetPath.Contains(".prefab"))
				return;

			Debug.Log("OnMoveMonoUIAsset:" + assetPath);
			GameObject uiPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
			if (uiPrefab == null)
				return;

			BaseUIView baseUIView = uiPrefab.GetComponent<BaseUIView>();
			if (baseUIView == null)
				return;

			string uiType = GetUITypeByUIPrefabName(uiPrefab.name);
			string uiPath = assetPath.Replace("Assets/Resources/", "").Replace(".prefab", "");
			List<string> names = new List<string>();
			Dictionary<string, string> dictUIPaths = new Dictionary<string, string>();
			LoadCacheCommonUIData(names, dictUIPaths);
			if (dictUIPaths.ContainsKey(uiType) && dictUIPaths[uiType] != uiPath)
			{
				string oldAssetPath = "Assets/Resources/" + dictUIPaths[uiType] + ".asset";
				AssetDatabase.DeleteAsset(oldAssetPath);

				dictUIPaths[uiType] = uiPath;

				AutoCreateCommonUIDataCS(names, dictUIPaths);
				SaveCacheCommonUIData(names, dictUIPaths);
			}
		}

		public static string GetUITypeByUIPrefabName(string name)
		{
			name = "ui_" + name.ToLower().Replace("monoui", "").Replace("view", "").Replace("(clone)", "");
			return name.ToUpper();
		}

		static void AutoCreateCommonUIDataCS(List<string> names, Dictionary<string, string> dictUIPaths)
		{
			FileStream fs = File.Open("Assets/Scripts/UI/CommonUIData.cs", FileMode.OpenOrCreate);
			if (fs == null)
				return;
			fs.SetLength(0);

			StreamWriter sw = new StreamWriter(fs, System.Text.Encoding.UTF8);
			sw.WriteLine("using System.Collections.Generic;");
			sw.WriteLine("// 这个文件是自动生成的, 不要手动修改!!!!! //");
			sw.WriteLine();

			sw.WriteLine("namespace UI");
			sw.WriteLine("{");

			// ui_type
			sw.WriteLine("\tpublic enum UI_TYPE : int");
			sw.WriteLine("\t{");
			for (int i = 0; i < names.Count; i++)
			{
				sw.WriteLine("\t\t" + names[i] + " = " + i + ",");
			}
			sw.WriteLine("\t}");//enum ui_type
			sw.WriteLine();

			//uitype Comparer
			sw.WriteLine("\tpublic class UITypeComparer : IEqualityComparer<UI_TYPE>");
			sw.WriteLine("\t{");
			sw.WriteLine("\t\tpublic bool Equals(UI_TYPE x, UI_TYPE y)\r\n\t\t{\r\n\t\t\tint iX = (int)x;\r\n\t\t\tint iY = (int)y;\r\n\t\t\treturn iX.Equals(iY);\r\n\t\t}");
			sw.WriteLine("\t\tpublic int GetHashCode(UI_TYPE obj)\r\n\t\t{\r\n\t\t\treturn (int)obj;\r\n\t\t}");
			sw.WriteLine("\t}");
			sw.WriteLine();

			sw.WriteLine("\tpublic class CommonUIData");
			sw.WriteLine("\t{");

			// ui path
			sw.WriteLine("\t\tpublic static readonly Dictionary<UI_TYPE, string> DICT_UI_PATHS = new Dictionary<UI_TYPE, string>(new UITypeComparer())");
			sw.WriteLine("\t\t{");
			foreach (var item in dictUIPaths)
			{
				if (item.Key == _ui_max_key || item.Key == _ui_none_key)
					continue;

				sw.WriteLine("\t\t\t{ UI_TYPE." + item.Key + ", \"" + item.Value + "\"},");
			}
			sw.WriteLine("\t\t};");

			sw.WriteLine("\t}");//class CommonUIData

			sw.WriteLine("}");//namespace
			sw.Close();
			fs.Close();
		}

		static void LoadCacheCommonUIData(List<string> names, Dictionary<string, string> dictUIPaths)
		{
			if (!File.Exists(_commonUIDataTempPath))
				return;

			FileStream fs = File.Open(_commonUIDataTempPath, FileMode.Open);
			StreamReader sr = new StreamReader(fs);

			while (!sr.EndOfStream)
			{
				string line = sr.ReadLine();
				string[] items = line.Split(':');
				names.Add(items[0]);
				dictUIPaths[items[0]] = items[1];
			}

			sr.Close();
			fs.Close();
		}

		static void SaveCacheCommonUIData(List<string> names, Dictionary<string, string> dictUIPaths)
		{
			FileStream fs = File.Open(_commonUIDataTempPath, FileMode.OpenOrCreate);
			fs.SetLength(0);
			StreamWriter sw = new StreamWriter(fs, System.Text.Encoding.UTF8);

			foreach (var item in names)
			{
				sw.WriteLine(item + ":" + dictUIPaths[item]);
			}

			sw.Close();
			fs.Close();
		}

	}
}

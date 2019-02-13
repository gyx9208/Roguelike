using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace UI
{
    public class UIPostprocessor : AssetPostprocessor
    {
        private static string _uiPrefabFile = "Resources/UI/UIView";
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

            foreach(string str in deletedAssets)
            {
                OnDeleteMonoUIAsset(str);
            }

            foreach(string str in movedAssets)
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
            Dictionary<string, int> dictUITypes = new Dictionary<string, int>();
            Dictionary<string, string> dictUIPaths = new Dictionary<string, string>();
            LoadCacheCommonUIData(dictUITypes, dictUIPaths);

            if (!dictUITypes.ContainsKey(_ui_none_key))
            {
                dictUITypes.Add(_ui_none_key, 0);
                dictUITypes.Add(_ui_max_key, 1);

                dictUIPaths.Add(_ui_none_key, "null");
                dictUIPaths.Add(_ui_max_key, "null");
            }

            if (!dictUITypes.ContainsKey(uiType))
            {
                int max = dictUITypes[_ui_max_key];
                dictUITypes.Add(uiType, max);
                dictUIPaths.Add(uiType, uiPath);
                dictUITypes[_ui_max_key] = ++max;

                dictUITypes = (from entry in dictUITypes orderby entry.Value ascending select entry)
                    .ToDictionary(pair => pair.Key, pair => pair.Value);

                AutoCreateCommonUIDataCS(dictUITypes, dictUIPaths);
                SaveCacheCommonUIData(dictUITypes, dictUIPaths);

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
            Dictionary<string, int> dictUITypes = new Dictionary<string, int>();
            Dictionary<string, string> dictUIPaths = new Dictionary<string, string>();
            LoadCacheCommonUIData(dictUITypes, dictUIPaths);
            if(dictUITypes.ContainsKey(uiType) && dictUIPaths[uiType] == uiPath)
            {
                dictUITypes.Remove(uiType);
                dictUIPaths.Remove(uiType);

                AutoCreateCommonUIDataCS(dictUITypes, dictUIPaths);
                SaveCacheCommonUIData(dictUITypes, dictUIPaths);
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
            Dictionary<string, int> dictUITypes = new Dictionary<string, int>();
            Dictionary<string, string> dictUIPaths = new Dictionary<string, string>();
            LoadCacheCommonUIData(dictUITypes, dictUIPaths);
            if(dictUIPaths.ContainsKey(uiType) && dictUIPaths[uiType] != uiPath)
            {
				string oldAssetPath = "Assets/Resources/" + dictUIPaths[uiType] + ".asset";
				AssetDatabase.DeleteAsset(oldAssetPath);

				dictUIPaths[uiType] = uiPath;

                AutoCreateCommonUIDataCS(dictUITypes, dictUIPaths);
                SaveCacheCommonUIData(dictUITypes, dictUIPaths);
			}
        }

        public static string GetUITypeByUIPrefabName(string name)
        {
            name = "ui_" + name.ToLower().Replace("monoui", "").Replace("view", "").Replace("(clone)", "");
			return name.ToUpper();
        }

        static void AutoCreateCommonUIDataCS(Dictionary<string, int> dictUITypes, Dictionary<string, string> dictUIPaths)
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
            foreach (var item in dictUITypes)
            {
                sw.WriteLine("\t\t" + item.Key + " = " + item.Value + ",");
            }
            sw.WriteLine("\t}");//enum ui_type
            sw.WriteLine();

			//uitype Comparer
			sw.WriteLine("\tpublic class UITypeComparer : IEqualityComparer<UI_TYPE>");
			sw.WriteLine("\t{");
			sw.WriteLine("\t\tpublic bool Equals(UI_TYPE x, UI_TYPE y)\n\t\t{\n\t\t\tint iX = (int)x;\n\t\t\tint iY = (int)y;\n\t\t\treturn iX.Equals(iY);\n\t\t}");
			sw.WriteLine("\t\tpublic int GetHashCode(UI_TYPE obj)\n\t\t{\n\t\t\treturn (int)obj;\n\t\t}");
			sw.WriteLine("\t}");
			sw.WriteLine();

			sw.WriteLine("\tpublic class CommonUIData");
            sw.WriteLine("\t{");

			// ui path
			sw.WriteLine("\t\tpublic static readonly Dictionary<UI_TYPE, string> DICT_UI_PATHS = new Dictionary<UI_TYPE, string>(new UITypeComparer())");
            sw.WriteLine("\t\t{");
            foreach(var item in dictUIPaths)
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

        static void LoadCacheCommonUIData(Dictionary<string, int> dictUITypes, Dictionary<string, string> dictUIPaths)
        {
            if (!File.Exists(_commonUIDataTempPath))
                return;

            FileStream fs = File.Open(_commonUIDataTempPath, FileMode.Open);
            StreamReader sr = new StreamReader(fs);

            while(!sr.EndOfStream)
            {
                string line = sr.ReadLine();
                string[] items = line.Split(':');
                dictUITypes[items[0]] = int.Parse(items[1]);
                dictUIPaths[items[0]] = items[2];
            }

            sr.Close();
            fs.Close();
        }

        static void SaveCacheCommonUIData(Dictionary<string, int> dictUITypes, Dictionary<string, string> dictUIPaths)
        {
            FileStream fs = File.Open(_commonUIDataTempPath, FileMode.OpenOrCreate);
            fs.SetLength(0);
            StreamWriter sw = new StreamWriter(fs, System.Text.Encoding.UTF8);

            foreach(var item in dictUITypes)
            {
                sw.WriteLine(item.Key + ":" + item.Value + ":" + dictUIPaths[item.Key]);
            }

            sw.Close();
            fs.Close();
        }
		
	}
}

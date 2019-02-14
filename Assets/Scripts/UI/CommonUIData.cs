using System.Collections.Generic;
// 这个文件是自动生成的, 不要手动修改!!!!! //

namespace UI
{
	public enum UI_TYPE : int
	{
		UI_NONE = 0,
		UI_MAIN = 1,
		UI_MAX = 2,
	}

	public class UITypeComparer : IEqualityComparer<UI_TYPE>
	{
		public bool Equals(UI_TYPE x, UI_TYPE y)
		{
			int iX = (int)x;
			int iY = (int)y;
			return iX.Equals(iY);
		}
		public int GetHashCode(UI_TYPE obj)
		{
			return (int)obj;
		}
	}

	public class CommonUIData
	{
		public static readonly Dictionary<UI_TYPE, string> DICT_UI_PATHS = new Dictionary<UI_TYPE, string>(new UITypeComparer())
		{
			{ UI_TYPE.UI_MAIN, "UI/MainView"},
		};
	}
}

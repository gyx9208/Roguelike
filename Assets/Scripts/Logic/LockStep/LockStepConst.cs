namespace Logic.LockStep
{
	public static class LockStepConst
	{
		public const int DECIMAL_PRECISION = 100;
		public const int DECIMAL_PRECISION_Length = 2;

		public static int ParseInLevelInt(string s)
		{
			int ret = 0;
			TryParseInLevelInt(s, out ret);
			return ret;
		}

		public static bool TryParseInLevelInt(string s, out int ret)
		{
			ret = 0;
			var pointIndex = s.IndexOf('.');
			int interger = 0, fraction = 0;
			bool positive = true;
			if (s.Contains("-"))
			{
				positive = false;
			}
			if (pointIndex >= 0)
			{
				string[] parts = s.Split('.');

				if (!int.TryParse(parts[0], out interger))
				{
					return false;
				}

				if (parts[1].Length > DECIMAL_PRECISION_Length)
				{
					parts[1] = parts[1].Substring(0, DECIMAL_PRECISION_Length);
				}

				if (!int.TryParse(parts[1], out fraction))
				{
					return false;
				}

				for (int i = parts[1].Length; i < DECIMAL_PRECISION_Length; i++)
				{
					fraction = fraction * 10;
				}
			}
			else
			{
				if (!int.TryParse(s, out interger))
				{
					return false;
				}
			}
			ret = interger * DECIMAL_PRECISION + (positive ? fraction : -fraction);
			return true;
		}
	}
}
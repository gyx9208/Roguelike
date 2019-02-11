using Logic.EP;

namespace Logic.Calculation
{
	public class SimpleAddMulCalculation : BaseCalculation
	{
		public const int BASE_ADD = 0;
		public const int BASE_MUL = 1;

		public override int GetSubParamIndex(string p)
		{
			switch (p)
			{
				case "ADD":
					return 0;
				case "MUL":
					return 1;
			}
			return 0;
		}

		public SimpleAddMulCalculation(int baseValue)
			: base(baseValue)
		{
		}

		protected override void Init()
		{
			_valueDict[BASE_ADD] = 0;
			_valueDict[BASE_MUL] = InLevelData.DECIMAL_PRECISION;
		}

		public override int GetCalResult()
		{
			int result = _baseValue;
			result += _valueDict[BASE_ADD];
			result = result * _valueDict[BASE_MUL] / InLevelData.DECIMAL_PRECISION;

			return result;
		}

		public override int GetCalResult(ExpressionContext context)
		{
			int result = _baseValue;
			result += GetOneKeySum(BASE_ADD, context);
			result = result * GetOneKeySum(BASE_MUL, context) / InLevelData.DECIMAL_PRECISION;

			return result;
		}
	}
}
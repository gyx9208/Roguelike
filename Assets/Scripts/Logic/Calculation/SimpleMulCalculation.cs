using Logic.FixedMath;


namespace Logic.Calculation
{
	public class SimpleMulCalculation : BaseCalculation
	{
		public const int DEFAULT_KEY = 0;

		public SimpleMulCalculation(int baseValue = 0)
			: base(baseValue)
		{
		}

		protected override void Init()
		{
			_valueDict[DEFAULT_KEY] = 0;
		}

		public override int GetCalResult()
		{
			int result = _baseValue;
			result = result * _valueDict[DEFAULT_KEY] / InLevelData.DECIMAL_PRECISION;

			return result;
		}

		public override int GetCalResult(ExpressionContext context)
		{
			int result = _baseValue;
			result = result * GetOneKeySum(DEFAULT_KEY, context) / InLevelData.DECIMAL_PRECISION;

			return result;
		}
	}
}
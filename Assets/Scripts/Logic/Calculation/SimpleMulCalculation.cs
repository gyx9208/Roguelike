using Logic.LockStep;

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
			result = result * _valueDict[DEFAULT_KEY] / LockStepConst.DECIMAL_PRECISION;

			return result;
		}

		public override int GetCalResult(ExpressionContext context)
		{
			int result = _baseValue;
			result = result * GetOneKeySum(DEFAULT_KEY, context) / LockStepConst.DECIMAL_PRECISION;

			return result;
		}
	}
}
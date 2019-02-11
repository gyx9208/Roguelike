using Logic.EP;

namespace Logic.Calculation
{
	public class SimpleAddCalculation : BaseCalculation
	{
		public const int DEFAULT_KEY = 0;

		public SimpleAddCalculation(int baseValue = 0)
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
			result += _valueDict[DEFAULT_KEY];

			return result;
		}

		public override int GetCalResult(ExpressionContext context)
		{
			int result = _baseValue;
			result += GetOneKeySum(DEFAULT_KEY, context);
			return result;
		}
	}
}

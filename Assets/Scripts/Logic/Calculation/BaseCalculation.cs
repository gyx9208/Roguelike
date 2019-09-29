using System.Collections.Generic;
using Logic.LockStep;

namespace Logic.Calculation
{
	public delegate void ValueChangedDelegate(BaseCalculation cal);

	public abstract class BaseCalculation
	{
		protected int _baseValue;
		protected Dictionary<int, int> _valueDict;
		protected Dictionary<int, List<Expression>> _expressionDict;

		public event ValueChangedDelegate onValueChanged;

		protected BaseCalculation(int baseValue)
		{
			_baseValue = baseValue;
			_valueDict = new Dictionary<int, int>();
			_expressionDict = new Dictionary<int, List<Expression>>();

			Init();
		}

		protected abstract void Init();

		public void SetBaseValue(int baseValue)
		{
			_baseValue = baseValue;
		}

		public int GetBaseValue()
		{
			return _baseValue;
		}

		public void AddValue(int key, int addValue)
		{
			_valueDict[key] += addValue;
			if (onValueChanged != null)
			{
				onValueChanged(this);
			}
		}

		public void AddExpression(int key, Expression exp)
		{
			if (!_expressionDict.ContainsKey(key))
				_expressionDict.Add(key, new List<Expression>());
			_expressionDict[key].Add(exp);
		}

		internal void RemoveExpression(int key, Expression value)
		{
			_expressionDict[key].Remove(value);
		}

		public void RegisterValueChange(ValueChangedDelegate action)
		{
			onValueChanged += action;
		}

		public void UnRegisterValueChange(ValueChangedDelegate action)
		{
			onValueChanged -= action;
		}

		/// <summary>
		/// The cal result is multiplied by decimal precision
		/// </summary>
		/// <returns></returns>
		public abstract int GetCalResult();

		/// <summary>
		/// The real result
		/// </summary>
		/// <returns></returns>
		public int GetResult()
		{
			return GetCalResult() / LockStepConst.DECIMAL_PRECISION;
		}

		public abstract int GetCalResult(ExpressionContext context);

		public int GetResult(ExpressionContext context)
		{
			return GetCalResult(context) / LockStepConst.DECIMAL_PRECISION;
		}

		protected int GetOneKeySum(int key, ExpressionContext context)
		{
			int ret = _valueDict[key];

			if (_expressionDict.ContainsKey(key))
			{
				var list = _expressionDict[key];
				for (int i = 0; i < list.Count; i++)
				{
					var exp = list[i];

					exp.SetContext(context);
					ret += exp.CalValue;
					exp.ClearContext();
				}
			}

			return ret;
		}

		public virtual int GetSubParamIndex(string p)
		{
			return 0;
		}

		public virtual int GetSubParamCalValue(string p, ExpressionContext context)
		{
			if ("BASE".Equals(p))
			{
				return _baseValue;
			}
			else
			{
				int index = GetSubParamIndex(p);
				return GetOneKeySum(index, context);
			}
		}
	}

	public class IntCalValue
	{
		private int value = 0;

		private static Queue<IntCalValue> _calValueQueue = new Queue<IntCalValue>();
		public static IntCalValue GetCalValue(int value)
		{
			IntCalValue calValue = (_calValueQueue.Count == 0) ? new IntCalValue() : _calValueQueue.Dequeue();
			calValue.value = value;
			return calValue;
		}
		public static void RecycleCalValue(IntCalValue calValue)
		{
			_calValueQueue.Enqueue(calValue);
		}
	}

}

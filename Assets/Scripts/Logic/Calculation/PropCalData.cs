using System.Collections;
using System.Collections.Generic;
using Fundamental;
using Logic.FixedMath;

namespace Logic.Calculation
{
	public class PropCalData : IEnumerable<KeyValuePair<int, BaseCalculation>>
	{
		protected Dictionary<int, BaseCalculation> _calDict;
		private System.Func<int, BaseCalculation> _GetBaseCal = null;

		public PropCalData()
		{
			_calDict = new Dictionary<int, BaseCalculation>();
		}

		public PropCalData(System.Func<int, BaseCalculation> func)
		{
			_calDict = new Dictionary<int, BaseCalculation>();
			_GetBaseCal = func;
		}

		public void AddPropCal(int propCal, BaseCalculation cal)
		{
			_calDict.Add(propCal, cal);
		}

		/// <summary>
		/// Use this function carefully, because it may return 'null'.
		/// </summary>
		/// <param name="propCal"></param>
		/// <returns></returns>
		public BaseCalculation GetCal(int propCal)
		{
			if (_calDict.ContainsKey(propCal))
			{
				return _calDict[propCal];
			}

			if (_GetBaseCal != null)
			{
				BaseCalculation cal = _GetBaseCal(propCal);
				if (cal != null)
				{
					_calDict.Add(propCal, cal);
					return cal;
				}
			}

			SuperDebug.LogError(propCal + " doesn't exist");
			return null;
		}

		public void SetBaseValue(int propCal, int value)
		{
			var cal = GetCal(propCal);
			if (cal != null)
			{
				cal.SetBaseValue(value);
			}
		}
		public void AddCalValue(int propCal, int key, int addValue)
		{
			var cal = GetCal(propCal);
			if (cal!=null)
			{
				cal.AddValue(key, addValue);
			}
		}

		public void AddExpression(int propCal, int key, Expression exp)
		{
			var cal = GetCal(propCal);
			if (cal != null)
			{
				cal.AddExpression(key, exp);
			}
		}

		public void RemoveExpression(int propCal, int key, Expression exp)
		{
			if (_calDict.ContainsKey(propCal))
			{
				_calDict[(int)propCal].RemoveExpression(key, exp);
			}
		}

		public void RegisterValueChange(int propCal, ValueChangedDelegate act)
		{
			var cal = GetCal(propCal);
			if (cal != null)
			{
				cal.RegisterValueChange(act);
			}
		}

		public void UnRegisterValueChange(int propCal, ValueChangedDelegate act)
		{
			if (_calDict.ContainsKey(propCal))
			{
				_calDict[(int)propCal].UnRegisterValueChange(act);
			}
		}

		IEnumerator<KeyValuePair<int, BaseCalculation>> IEnumerable<KeyValuePair<int, BaseCalculation>>.GetEnumerator()
		{
			return _calDict.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return _calDict.GetEnumerator();
		}

		public int GetCalResult(int propCal)
		{
			var cal = GetCal(propCal);
			if (cal != null)
				return cal.GetCalResult();

			return 0;
		}

		public int GetResult(int propCal)
		{
			var cal = GetCal(propCal);
			if (cal != null)
				return cal.GetResult();

			return 0;
		}
	}
}
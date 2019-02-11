using UnityEngine;
using System.Collections.Generic;

namespace Logic.Calculation
{
	public delegate void RuntimeValueChangedDelegate(int oldValue, int newValue);

	public class ValueCup
	{
		public enum ChangeCup
		{
			ChangeWater,
			KeepWater
		}

		private ChangeCup _changeCup;

		private int _maxValue = 0;
		private int _minValue = 0;
		private int _currentValue = 0;

		public event RuntimeValueChangedDelegate onCurrentValueChanged;

		public ValueCup(int value, int maxValue, ChangeCup changeCup,int min)
		{
			_changeCup = changeCup;

			_maxValue = maxValue;
			_minValue = min;
			_currentValue = value;
		}

		int ClampLiterally(int value)
		{
			return Mathf.Clamp(value, 0, _maxValue);
		}

		void ClampValue(int value)
		{
			_currentValue = Mathf.Clamp(value, _minValue, _maxValue);
		}

		public int LiteralValue
		{
			get
			{
				return ClampLiterally(_currentValue);
			}
		}

		public int Value
		{
			set
			{
				int oldValue = _currentValue;
				ClampValue(value);
				if (oldValue != _currentValue && onCurrentValueChanged != null)
				{
					onCurrentValueChanged(oldValue, _currentValue);
				}
			}
			get
			{
				return _currentValue;
			}
		}

		public int ChangeValue(int change)
		{
			int oldValue = _currentValue;
			ClampValue(_currentValue + change);
			if (oldValue != _currentValue && onCurrentValueChanged != null)
			{
				onCurrentValueChanged(oldValue, _currentValue);
			}
			return _currentValue - oldValue;
		}

		public int MaxValue
		{
			set
			{
				switch (_changeCup)
				{
					case ChangeCup.ChangeWater:
						_currentValue = _currentValue / _maxValue * value;
						_maxValue = value;
						break;
					case ChangeCup.KeepWater:
						_maxValue = value;
						ClampValue(_currentValue);
						break;
				}
				onCurrentValueChanged?.Invoke(_currentValue, _currentValue);
			}
			get
			{
				return _maxValue;
			}
		}

		public int MinValue
		{
			set
			{
				switch (_changeCup)
				{
					case ChangeCup.ChangeWater:
					case ChangeCup.KeepWater:
						_minValue = value;
						ClampValue(_currentValue);
						break;
				}
			}
			get
			{
				return _minValue;
			}
		}

		public int LossValue
		{
			get
			{
				return _maxValue - _currentValue;
			}
		}

		public int Ratio
		{
			get
			{
				return LiteralValue * InLevelData.DECIMAL_PRECISION / _maxValue;
			}
		}
	}

	public class PropRuntimeValue
	{
		private Dictionary<int, ValueCup> _valueDict;

		public PropRuntimeValue()
		{
			_valueDict = new Dictionary<int, ValueCup>();
		}

		public void AddPropValue(int propValue, ValueCup cup)
		{
			_valueDict.Add(propValue, cup);
		}

		public int GetLiteralValue(int propValue)
		{
			if (_valueDict.ContainsKey(propValue))
			{
				return _valueDict[propValue].LiteralValue;
			}
			return 0;
		}

		public int GetValue(int propValue)
		{
			if (_valueDict.ContainsKey(propValue))
			{
				return _valueDict[propValue].Value;
			}
			return 0;
		}

		public int GetCalValue(int propValue)
		{
			return GetLiteralValue(propValue) * InLevelData.DECIMAL_PRECISION;
		}

		public int ChangeValue(int propValue, int changedValue)
		{
			if (_valueDict.ContainsKey(propValue))
			{
				return _valueDict[propValue].ChangeValue(changedValue);
			}
			return 0;
		}

		public void SetValue(int propValue, int value)
		{
			if (_valueDict.ContainsKey(propValue))
			{
				_valueDict[propValue].Value = value;
			}
		}

		public void RegisterValueChange(int propValue, RuntimeValueChangedDelegate func)
		{
			if (_valueDict.ContainsKey(propValue))
			{
				_valueDict[propValue].onCurrentValueChanged += func;
			}
		}

		public void UnRegisterValueChange(int propValue, RuntimeValueChangedDelegate func)
		{
			if (_valueDict.ContainsKey(propValue))
			{
				_valueDict[propValue].onCurrentValueChanged -= func;
			}
		}

		public int GetMaxValue(int propValue)
		{
			if (_valueDict.ContainsKey(propValue))
			{
				return _valueDict[propValue].MaxValue;
			}
			return 0;
		}

		public int GetCalMaxValue(int prop)
		{
			return GetMaxValue(prop) * InLevelData.DECIMAL_PRECISION;
		}

		public void SetMaxValue(int propValue, int maxValue)
		{
			if (_valueDict.ContainsKey(propValue))
			{
				_valueDict[propValue].MaxValue = maxValue;
			}
		}

		public int GetMinValue(int propValue)
		{
			if (_valueDict.ContainsKey(propValue))
			{
				return _valueDict[propValue].MinValue;
			}
			return 0;
		}

		public void SetMinValue(int propValue, int minValue)
		{
			if (_valueDict.ContainsKey(propValue))
			{
				_valueDict[propValue].MinValue = minValue;
			}
		}

		public int GetLossValue(int propValue)
		{
			if (_valueDict.ContainsKey(propValue))
			{
				return _valueDict[propValue].LossValue;
			}
			return 0;
		}

		public int GetRatio(int propValue)
		{
			if (_valueDict.ContainsKey(propValue))
			{
				return _valueDict[propValue].Ratio;
			}
			return 0;
		}
	}
}
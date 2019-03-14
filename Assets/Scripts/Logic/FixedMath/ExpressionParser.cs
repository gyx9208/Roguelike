/* * * * * * * * * * * * * *
 * A simple expression parser
 * --------------------------
 * 
 * The parser can parse a mathematical expression into a simple custom
 * expression tree. It can recognise methods and fields/contants which
 * are user extensible. It can also contain expression parameters which
 * are registrated automatically. An expression tree can be "converted"
 * into a delegate.
 * 
 * Written by Bunny83
 * 2014-11-02
 * 
 * Features:
 * - Elementary arithmetic [ + - * / ]
 * - Power [ ^ ]
 * - Brackets ( )
 * - Most function from System.Math (abs, sin, round, floor, min, ...)
 * - Constants ( e, PI )
 * - MultiValue return (quite slow, produce extra garbage each call)
 *
 * mod by gyx:
 * - compare: > < >= <= == != ,if true, return 1, else return 0
 * - compare?a:b
 * we use 2point precision, 100*100=100 means 1*1=1
 * * * * * * * * * * * * * */

using Fundamental;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Logic.FixedMath
{
	public interface IValue
	{
		int CalValue { get; }
	}

	public class Number : IValue
	{
		private int m_Value;
		public int CalValue
		{
			get { return m_Value; }
			set { m_Value = value; }
		}
		public Number(int aValue)
		{
			m_Value = aValue;
		}

		public override string ToString()
		{
			return "" + (float)m_Value / InLevelData.DECIMAL_PRECISION + "";
		}
	}
	public class OperationSum : IValue
	{
		private IValue[] m_Values;
		public int CalValue
		{
			get { return m_Values.Select(v => v.CalValue).Sum(); }
		}
		public OperationSum(params IValue[] aValues)
		{
			// collapse unnecessary nested sum operations.
			List<IValue> v = new List<IValue>(aValues.Length);
			foreach (var I in aValues)
			{
				var sum = I as OperationSum;
				if (sum == null)
					v.Add(I);
				else
					v.AddRange(sum.m_Values);
			}
			m_Values = v.ToArray();
		}
		public override string ToString()
		{
			return "( " + string.Join(" + ", m_Values.Select(v => v.ToString()).ToArray()) + " )";
		}
	}
	public class OperationProduct : IValue
	{
		private IValue[] m_Values;
		public int CalValue
		{
			get { return m_Values.Select(v => v.CalValue).Aggregate((v1, v2) => v1 * v2 / InLevelData.DECIMAL_PRECISION); }
		}
		public OperationProduct(params IValue[] aValues)
		{
			m_Values = aValues;
		}
		public override string ToString()
		{
			return "( " + string.Join(" * ", m_Values.Select(v => v.ToString()).ToArray()) + " )";
		}
	}
	/*
	public class OperationPower : IValue
	{
		private IValue m_Value;
		private IValue m_Power;
		public double CalValue
		{
			get { return System.Math.Pow(m_Value.CalValue, m_Power.CalValue); }
		}
		public OperationPower(IValue aValue, IValue aPower)
		{
			m_Value = aValue;
			m_Power = aPower;
		}
		public override string ToString()
		{
			return "( " + m_Value + "^" + m_Power + " )";
		}

	}*/

	public class OperationNegate : IValue
	{
		private IValue m_Value;
		public int CalValue
		{
			get { return -m_Value.CalValue; }
		}
		public OperationNegate(IValue aValue)
		{
			m_Value = aValue;
		}
		public override string ToString()
		{
			return "( -" + m_Value + " )";
		}

	}

	public class OperationReciprocal : IValue
	{
		private IValue m_Value1, m_Value2;
		public int CalValue
		{
			get { return InLevelData.DECIMAL_PRECISION * m_Value1.CalValue / m_Value2.CalValue; }
		}
		public OperationReciprocal(IValue v1, IValue v2)
		{
			m_Value1 = v1;
			m_Value2 = v2;
		}
		public override string ToString()
		{
			return "( " + m_Value1 + " / " + m_Value2 + " )";
		}
	}


	public class MultiParameterList : IValue
	{
		private IValue[] m_Values;
		public IValue[] Parameters { get { return m_Values; } }
		public int CalValue
		{
			get { return m_Values.Select(v => v.CalValue).FirstOrDefault(); }
		}
		public MultiParameterList(params IValue[] aValues)
		{
			m_Values = aValues;
		}
		public override string ToString()
		{
			return string.Join(", ", m_Values.Select(v => v.ToString()).ToArray());
		}
	}

	public class CustomFunction : IValue
	{
		private IValue[] m_Params;
		private System.Func<int[], int> m_Delegate;
		private string m_Name;
		public int CalValue
		{
			get
			{
				if (m_Params == null)
					return m_Delegate(null);
				return m_Delegate(m_Params.Select(p => p.CalValue).ToArray());
			}
		}
		public CustomFunction(string aName, System.Func<int[], int> aDelegate, params IValue[] aValues)
		{
			m_Delegate = aDelegate;
			m_Params = aValues;
			m_Name = aName;
		}
		public override string ToString()
		{
			if (m_Params == null)
				return m_Name;
			return m_Name + "(" + string.Join(", ", m_Params.Select(v => v.ToString()).ToArray()) + ")[" + (float)CalValue / InLevelData.DECIMAL_PRECISION + "]";
		}
	}

	public class NamespacedFunction : IValue
	{
		private IValue[] _Params;
		private string _NameSpace, _Function;
		private Expression _Expression;

		public int CalValue
		{
			get
			{
				return _Expression.GetContextValue(_NameSpace, _Function, _Params.Select(p => p.CalValue).ToArray());
			}
		}

		public override string ToString()
		{
			return _NameSpace + "." + _Function + "(" + string.Join(", ", _Params.Select(v => v.ToString()).ToArray()) + ")[" + (float)CalValue / InLevelData.DECIMAL_PRECISION + "]";
		}

		public NamespacedFunction(string f, Expression expression, params IValue[] aValues)
		{
			_Expression = expression;
			_Params = aValues;

			var parts = f.Split('.');
			if (parts.Length == 2)
			{
				_NameSpace = parts[0];
				_Function = parts[1];
			}
			else
			{
				throw new ParseException("Invalid Parameter");
			}
		}
	}

	public class Parameter : IValue
	{
		private int _Value;
		private string _NameSpace, _Parameter;
		private ParameterDelegate _Func;

		public int CalValue
		{
			get
			{
				if (_Func != null)
					return _Func(_NameSpace, _Parameter);
				return _Value;
			}
			set
			{
				_Value = value;
			}
		}

		public override string ToString()
		{
			return _NameSpace + "." + _Parameter + "[" + (float)CalValue / InLevelData.DECIMAL_PRECISION + "]";
		}
		public Parameter(string aName, Expression expression)
		{
			_Value = 0;

			var parts = aName.Split('.');
			if (parts.Length == 1)
			{
				_NameSpace = string.Empty;
				_Parameter = parts[0].ToUpper();

			}
			else if (parts.Length == 2)
			{
				_NameSpace = parts[0].ToUpper();
				_Parameter = parts[1].ToUpper();
				_Func = expression.GetContextValue;
			}
			else
			{
				throw new ParseException("Invalid Parameter");
			}
		}
	}
	#region compare
	public class OperationMoreThan : IValue
	{
		private IValue m_Value1, m_Value2;
		public int CalValue
		{
			get { return m_Value1.CalValue > m_Value2.CalValue ? InLevelData.DECIMAL_PRECISION : 0; }
		}
		public OperationMoreThan(IValue v1, IValue v2)
		{
			m_Value1 = v1;
			m_Value2 = v2;
		}
		public override string ToString()
		{
			return "( " + m_Value1 + " > " + m_Value2 + " )";
		}
	}

	public class OperationMoreThanEqual : IValue
	{
		private IValue m_Value1, m_Value2;
		public int CalValue
		{
			get { return m_Value1.CalValue >= m_Value2.CalValue ? InLevelData.DECIMAL_PRECISION : 0; }
		}
		public OperationMoreThanEqual(IValue v1, IValue v2)
		{
			m_Value1 = v1;
			m_Value2 = v2;
		}
		public override string ToString()
		{
			return "( " + m_Value1 + " >= " + m_Value2 + " )";
		}
	}

	public class OperationLessThan : IValue
	{
		private IValue m_Value1, m_Value2;
		public int CalValue
		{
			get { return m_Value1.CalValue < m_Value2.CalValue ? InLevelData.DECIMAL_PRECISION : 0; }
		}
		public OperationLessThan(IValue v1, IValue v2)
		{
			m_Value1 = v1;
			m_Value2 = v2;
		}
		public override string ToString()
		{
			return "( " + m_Value1 + " < " + m_Value2 + " )";
		}
	}

	public class OperationLessThanEqual : IValue
	{
		private IValue m_Value1, m_Value2;
		public int CalValue
		{
			get { return m_Value1.CalValue <= m_Value2.CalValue ? InLevelData.DECIMAL_PRECISION : 0; }
		}
		public OperationLessThanEqual(IValue v1, IValue v2)
		{
			m_Value1 = v1;
			m_Value2 = v2;
		}
		public override string ToString()
		{
			return "( " + m_Value1 + " <= " + m_Value2 + " )";
		}
	}

	public class OperationEqual : IValue
	{
		private IValue m_Value1, m_Value2;
		public int CalValue
		{
			get { return m_Value1.CalValue == m_Value2.CalValue ? InLevelData.DECIMAL_PRECISION : 0; }
		}
		public OperationEqual(IValue v1, IValue v2)
		{
			m_Value1 = v1;
			m_Value2 = v2;
		}
		public override string ToString()
		{
			return "( " + m_Value1 + " == " + m_Value2 + " )";
		}
	}

	public class OperationNotEqual : IValue
	{
		private IValue m_Value1, m_Value2;
		public int CalValue
		{
			get { return m_Value1.CalValue != m_Value2.CalValue ? InLevelData.DECIMAL_PRECISION : 0; }
		}
		public OperationNotEqual(IValue v1, IValue v2)
		{
			m_Value1 = v1;
			m_Value2 = v2;
		}
		public override string ToString()
		{
			return "( " + m_Value1 + " != " + m_Value2 + " )";
		}
	}

	public class OperationChoose : IValue
	{
		private IValue m_Value1, m_Value2, m_Value3;
		public int CalValue
		{
			get { return m_Value1.CalValue > 0 ? m_Value2.CalValue : m_Value3.CalValue; }
		}
		public OperationChoose(IValue v1, IValue v2, IValue v3)
		{
			m_Value1 = v1;
			m_Value2 = v2;
			m_Value3 = v3;
		}
		public override string ToString()
		{
			return "(" + m_Value1 + "?" + m_Value2 + ":" + m_Value3 + ")";
		}
	}
	#endregion
	public class Expression : IValue
	{
		public Dictionary<string, Parameter> Parameters = new Dictionary<string, Parameter>();
		public IValue ExpressionTree { get; set; }
		public int CalValue
		{
			get
			{
#if UNITY_EDITOR
				SuperDebug.Log(DebugPrefix.Expression, ToString());
#endif
				return ExpressionTree.CalValue;
			}
		}

		public int Value
		{
			get { return CalValue / InLevelData.DECIMAL_PRECISION; }
		}
		/*
		public int[] MultiValue
		{
			get
			{
				var t = ExpressionTree as MultiParameterList;
				if (t != null)
				{
					int[] res = new int[t.Parameters.Length];
					for (int i = 0; i < res.Length; i++)
						res[i] = t.Parameters[i].CalValue;
					return res;
				}
				return null;
			}
		}*/
		public override string ToString()
		{
			return ExpressionTree.ToString();
		}

		public void SetParameterValue(string p, int value)
		{
			if (Parameters.ContainsKey(p))
			{
				Parameters[p].CalValue = value * InLevelData.DECIMAL_PRECISION;
			}
		}
		public void SetParameterCalValue(string p, int calvalue)
		{
			if (Parameters.ContainsKey(p))
			{
				Parameters[p].CalValue = calvalue;
			}
		}

		/*
		public ExpressionDelegate ToDelegate(params string[] aParamOrder)
		{
			var parameters = new List<Parameter>(aParamOrder.Length);
			for (int i = 0; i < aParamOrder.Length; i++)
			{
				if (Parameters.ContainsKey(aParamOrder[i]))
					parameters.Add(Parameters[aParamOrder[i]]);
				else
					parameters.Add(null);
			}
			var parameters2 = parameters.ToArray();

			return (p) => Invoke(p, parameters2);
		}
		public MultiResultDelegate ToMultiResultDelegate(params string[] aParamOrder)
		{
			var parameters = new List<Parameter>(aParamOrder.Length);
			for (int i = 0; i < aParamOrder.Length; i++)
			{
				if (Parameters.ContainsKey(aParamOrder[i]))
					parameters.Add(Parameters[aParamOrder[i]]);
				else
					parameters.Add(null);
			}
			var parameters2 = parameters.ToArray();


			return (p) => InvokeMultiResult(p, parameters2);
		}

		int Invoke(int[] aParams, Parameter[] aParamList)
		{
			int count = System.Math.Min(aParamList.Length, aParams.Length);
			for (int i = 0; i < count; i++)
			{
				if (aParamList[i] != null)
					aParamList[i].CalValue = aParams[i];
			}
			return CalValue;
		}
		int[] InvokeMultiResult(int[] aParams, Parameter[] aParamList)
		{
			int count = System.Math.Min(aParamList.Length, aParams.Length);
			for (int i = 0; i < count; i++)
			{
				if (aParamList[i] != null)
					aParamList[i].CalValue = aParams[i];
			}
			return MultiValue;
		}
		public static Expression Parse(string aExpression)
		{
			return new ExpressionParser().EvaluateExpression(aExpression);
		}
		*/


		public virtual void SetContext(ExpressionContext context)
		{

		}

		public virtual void ClearContext()
		{

		}

		public virtual int GetContextValue(string nameSpace, string parameter)
		{
			return 0;
		}

		public virtual int GetContextValue(string nameSpace, string function, int[] aParams)
		{
			return 0;
		}
	}

	public delegate int ExpressionDelegate(params int[] aParams);
	public delegate int[] MultiResultDelegate(params int[] aParams);
	public delegate int ParameterDelegate(string s1, string s2);

	public class ExpressionParser : Singleton<ExpressionParser>
	{
		private List<string> m_BracketHeap = new List<string>();
		private Dictionary<string, System.Func<int>> m_Consts = new Dictionary<string, System.Func<int>>();
		private Dictionary<string, System.Func<int[], int>> m_Funcs = new Dictionary<string, System.Func<int[], int>>();
		private Expression m_Context;

		public ExpressionParser()
		{
			//m_Consts.Add("RANDOM", () => FrameRandom.Random(InLevelData.DECIMAL_PRECISION));

			m_Funcs.Add("MIN", (p) => System.Math.Min(p.FirstOrDefault(), p.ElementAtOrDefault(1)));
			m_Funcs.Add("MAX", (p) => System.Math.Max(p.FirstOrDefault(), p.ElementAtOrDefault(1)));

			/*
			var rnd = new System.Random();
			
			m_Consts.Add("PI", () => System.Math.PI);
			m_Consts.Add("e", () => System.Math.E);
			m_Funcs.Add("sqrt", (p) => System.Math.Sqrt(p.FirstOrDefault()));
			m_Funcs.Add("abs", (p) => System.Math.Abs(p.FirstOrDefault()));
			m_Funcs.Add("ln", (p) => System.Math.Log(p.FirstOrDefault()));
			m_Funcs.Add("floor", (p) => System.Math.Floor(p.FirstOrDefault()));
			m_Funcs.Add("ceiling", (p) => System.Math.Ceiling(p.FirstOrDefault()));
			m_Funcs.Add("round", (p) => System.Math.Round(p.FirstOrDefault()));

			m_Funcs.Add("sin", (p) => System.Math.Sin(p.FirstOrDefault()));
			m_Funcs.Add("cos", (p) => System.Math.Cos(p.FirstOrDefault()));
			m_Funcs.Add("tan", (p) => System.Math.Tan(p.FirstOrDefault()));

			m_Funcs.Add("asin", (p) => System.Math.Asin(p.FirstOrDefault()));
			m_Funcs.Add("acos", (p) => System.Math.Acos(p.FirstOrDefault()));
			m_Funcs.Add("atan", (p) => System.Math.Atan(p.FirstOrDefault()));
			m_Funcs.Add("atan2", (p) => System.Math.Atan2(p.FirstOrDefault(), p.ElementAtOrDefault(1)));

			//System.Math.Floor
			
			
			m_Funcs.Add("rnd", (p) =>
			{
				if (p.Length == 2)
					return p[0] + rnd.NextDouble() * (p[1] - p[0]);
				if (p.Length == 1)
					return rnd.NextDouble() * p[0];
				return rnd.NextDouble();
			});*/
		}

		public override void Init()
		{
			base.Init();
		}

		public void AddFunc(string aName, System.Func<int[], int> aMethod)
		{
			if (m_Funcs.ContainsKey(aName))
				m_Funcs[aName] = aMethod;
			else
				m_Funcs.Add(aName, aMethod);
		}

		public void AddConst(string aName, System.Func<int> aMethod)
		{
			if (m_Consts.ContainsKey(aName))
				m_Consts[aName] = aMethod;
			else
				m_Consts.Add(aName, aMethod);
		}
		public void RemoveFunc(string aName)
		{
			if (m_Funcs.ContainsKey(aName))
				m_Funcs.Remove(aName);
		}
		public void RemoveConst(string aName)
		{
			if (m_Consts.ContainsKey(aName))
				m_Consts.Remove(aName);
		}

		int FindClosingBracket(ref string aText, int aStart, char aOpen, char aClose)
		{
			int counter = 0;
			for (int i = aStart; i < aText.Length; i++)
			{
				if (aText[i] == aOpen)
					counter++;
				if (aText[i] == aClose)
					counter--;
				if (counter == 0)
					return i;
			}
			return -1;
		}

		void SubstitudeBracket(ref string aExpression, int aIndex)
		{
			int closing = FindClosingBracket(ref aExpression, aIndex, '(', ')');
			if (closing > aIndex + 1)
			{
				string inner = aExpression.Substring(aIndex + 1, closing - aIndex - 1);
				m_BracketHeap.Add(inner);
				string sub = "&" + (m_BracketHeap.Count - 1) + ";";
				aExpression = aExpression.Substring(0, aIndex) + sub + aExpression.Substring(closing + 1);
			}
			else throw new ParseException("Bracket not closed!");
		}

		IValue Parse(string aExpression)
		{
			aExpression = aExpression.Trim();
			int index = aExpression.IndexOf('(');
			while (index >= 0)
			{
				SubstitudeBracket(ref aExpression, index);
				index = aExpression.IndexOf('(');
			}
			if (aExpression.Contains(','))
			{
				string[] parts = aExpression.Split(',');
				List<IValue> exp = new List<IValue>(parts.Length);
				for (int i = 0; i < parts.Length; i++)
				{
					string s = parts[i].Trim();
					if (!string.IsNullOrEmpty(s))
						exp.Add(Parse(s));
				}
				return new MultiParameterList(exp.ToArray());
			}
			#region compare
			else if (aExpression.Contains("?"))
			{
				int qindex = aExpression.IndexOf("?");
				int colon = aExpression.IndexOf(":");
				string p1 = aExpression.Substring(0, qindex);
				string p2 = aExpression.Substring(qindex + 1, colon - qindex - 1);
				string p3 = aExpression.Substring(colon + 1);
				IValue v1 = Parse(p1);
				IValue v2 = Parse(p2);
				IValue v3 = Parse(p3);
				return new OperationChoose(v1, v2, v3);
			}
			else if (aExpression.Contains(">="))
			{
				int firstMoreThan = aExpression.IndexOf(">=");
				string p1 = aExpression.Substring(0, firstMoreThan);
				string p2 = aExpression.Substring(firstMoreThan + 2);
				IValue v1 = Parse(p1);
				IValue v2 = Parse(p2);
				return new OperationMoreThanEqual(v1, v2);
			}
			else if (aExpression.Contains('>'))
			{
				int firstMoreThan = aExpression.IndexOf('>');
				string p1 = aExpression.Substring(0, firstMoreThan);
				string p2 = aExpression.Substring(firstMoreThan + 1);
				IValue v1 = Parse(p1);
				IValue v2 = Parse(p2);
				return new OperationMoreThan(v1, v2);
			}
			else if (aExpression.Contains("<="))
			{
				int firstMoreThan = aExpression.IndexOf("<=");
				string p1 = aExpression.Substring(0, firstMoreThan);
				string p2 = aExpression.Substring(firstMoreThan + 2);
				IValue v1 = Parse(p1);
				IValue v2 = Parse(p2);
				return new OperationLessThanEqual(v1, v2);
			}
			else if (aExpression.Contains('<'))
			{
				int firstMoreThan = aExpression.IndexOf('<');
				string p1 = aExpression.Substring(0, firstMoreThan);
				string p2 = aExpression.Substring(firstMoreThan + 1);
				IValue v1 = Parse(p1);
				IValue v2 = Parse(p2);
				return new OperationLessThan(v1, v2);
			}
			else if (aExpression.Contains("=="))
			{
				int firstMoreThan = aExpression.IndexOf("==");
				string p1 = aExpression.Substring(0, firstMoreThan);
				string p2 = aExpression.Substring(firstMoreThan + 2);
				IValue v1 = Parse(p1);
				IValue v2 = Parse(p2);
				return new OperationEqual(v1, v2);
			}
			else if (aExpression.Contains("!="))
			{
				int firstMoreThan = aExpression.IndexOf("!=");
				string p1 = aExpression.Substring(0, firstMoreThan);
				string p2 = aExpression.Substring(firstMoreThan + 2);
				IValue v1 = Parse(p1);
				IValue v2 = Parse(p2);
				return new OperationNotEqual(v1, v2);
			}
			#endregion
			else if (aExpression.Contains('+'))
			{
				string[] parts = aExpression.Split('+');
				List<IValue> exp = new List<IValue>(parts.Length);
				for (int i = 0; i < parts.Length; i++)
				{
					string s = parts[i].Trim();
					if (!string.IsNullOrEmpty(s))
						exp.Add(Parse(s));
				}
				if (exp.Count == 1)
					return exp[0];
				return new OperationSum(exp.ToArray());
			}
			else if (aExpression.Contains('-'))
			{
				string[] parts = aExpression.Split('-');
				List<IValue> exp = new List<IValue>(parts.Length);
				if (!string.IsNullOrEmpty(parts[0].Trim()))
					exp.Add(Parse(parts[0]));
				for (int i = 1; i < parts.Length; i++)
				{
					string s = parts[i].Trim();
					if (!string.IsNullOrEmpty(s))
						exp.Add(new OperationNegate(Parse(s)));
				}
				if (exp.Count == 1)
					return exp[0];
				return new OperationSum(exp.ToArray());
			}
			else if (aExpression.Contains('*'))
			{
				string[] parts = aExpression.Split('*');
				List<IValue> exp = new List<IValue>(parts.Length);
				for (int i = 0; i < parts.Length; i++)
				{
					exp.Add(Parse(parts[i]));
				}
				if (exp.Count == 1)
					return exp[0];
				return new OperationProduct(exp.ToArray());
			}
			else if (aExpression.Contains('/'))
			{
				int lastDivide = aExpression.LastIndexOf('/');

				string p1 = aExpression.Substring(0, lastDivide);
				string p2 = aExpression.Substring(lastDivide + 1);
				IValue v1 = Parse(p1);
				IValue v2 = Parse(p2);
				return new OperationReciprocal(v1, v2);
			}/*
			else if (aExpression.Contains('^'))
			{
				int pos = aExpression.IndexOf('^');
				var val = Parse(aExpression.Substring(0, pos));
				var pow = Parse(aExpression.Substring(pos + 1));
				return new OperationPower(val, pow);
			}*/


			int pPos = aExpression.IndexOf("&");
			if (pPos > 0)
			{
				string fName = aExpression.Substring(0, pPos);

				string inner;
				IValue param;
				MultiParameterList multiParams;
				IValue[] parameters;
				foreach (var M in m_Funcs)
				{
					if (fName == M.Key)
					{
						inner = aExpression.Substring(M.Key.Length);
						param = Parse(inner);
						multiParams = param as MultiParameterList;
						if (multiParams != null)
							parameters = multiParams.Parameters;
						else
							parameters = new IValue[] { param };
						return new CustomFunction(M.Key, M.Value, parameters);
					}
				}

				inner = aExpression.Substring(fName.Length);
				param = Parse(inner);
				multiParams = param as MultiParameterList;
				if (multiParams != null)
					parameters = multiParams.Parameters;
				else
					parameters = new IValue[] { param };
				return new NamespacedFunction(fName, m_Context, parameters);
			}

			foreach (var C in m_Consts)
			{
				if (aExpression == C.Key)
				{
					return new CustomFunction(C.Key, (p) => C.Value(), null);
				}
			}

			int index2a = aExpression.IndexOf('&');
			int index2b = aExpression.IndexOf(';');
			if (index2a >= 0 && index2b >= 2)
			{
				var inner = aExpression.Substring(index2a + 1, index2b - index2a - 1);
				int bracketIndex;
				if (int.TryParse(inner, out bracketIndex) && bracketIndex >= 0 && bracketIndex < m_BracketHeap.Count)
				{
					return Parse(m_BracketHeap[bracketIndex]);
				}
				else
					throw new ParseException("Can't parse substitude token");
			}

			if (ValidIdentifier(aExpression))
			{
				if (m_Context.Parameters.ContainsKey(aExpression))
					return m_Context.Parameters[aExpression];
				var val = new Parameter(aExpression, m_Context);
				m_Context.Parameters.Add(aExpression, val);
				return val;
			}

			int intValue;
			if (InLevelData.TryParseInLevelInt(aExpression, out intValue))
			{
				return new Number(intValue);
			}

			throw new ParseException("Reached unexpected end within the parsing tree");
		}

		private bool ValidIdentifier(string aExpression)
		{
			aExpression = aExpression.Trim();
			if (string.IsNullOrEmpty(aExpression))
				return false;
			if (aExpression.Length < 1)
				return false;
			if (aExpression.Contains(" "))
				return false;
			if (!"abcdefghijklmnopqrstuvwxyz§$".Contains(char.ToLower(aExpression[0])))
				return false;
			if (m_Consts.ContainsKey(aExpression))
				return false;
			if (m_Funcs.ContainsKey(aExpression))
				return false;
			return true;
		}

		public Expression EvaluateExpression(string aExpression)
		{
			try
			{
				var val = new Expression();
				m_Context = val;
				val.ExpressionTree = Parse(aExpression);
				m_Context = null;
				m_BracketHeap.Clear();
				return val;
			}
			catch (ParseException ex)
			{
				Debug.LogError("expression:" + aExpression + "\n" + ex);
				throw ex;
			}
		}
	}

	public class ParseException : System.Exception
	{
		public ParseException(string aMessage) : base(aMessage) { }
	}

	public class ExpressionContext
	{

	}
}
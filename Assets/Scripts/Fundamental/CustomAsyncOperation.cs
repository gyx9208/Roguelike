using System;
using System.Collections;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.Util;

namespace Fundamental
{
	/// <summary>
	/// Async operation for counting missions
	/// </summary>
	public class CountingMission : PooledClassObject, IEnumerator
	{
		Action<CountingMission> m_completedAction;
		Action<object> m_updatAction;
		protected int m_current, m_target;

		public CountingMission()
		{
		}

		public override void OnUse()
		{
		}

		public override void OnRelease()
		{
			m_current = 0;
			m_target = 0;
			m_completedAction = null;
			m_updatAction = null;
		}

		public object Current
		{
			get
			{
				return m_current;
			}
		}

		public bool MoveNext()
		{
			return !IsDone;
		}

		public void Reset()
		{
		}

		public override string ToString()
		{
			return "";
		}

		public event Action<CountingMission> Completed
		{
			add
			{
				if (IsDone)
					DelayedActionManager.AddAction(value, 0, this);
				else
					m_completedAction += value;
			}

			remove
			{
				m_completedAction -= value;
			}
		}

		public event Action<object> Update
		{
			add
			{

				m_updatAction += value;
			}

			remove
			{
				m_updatAction -= value;
			}
		}

		public bool IsDone
		{
			get
			{
				return m_current >= m_target;
			}
		}

		public float PercentComplete
		{
			get
			{
				return (float)m_current / m_target;
			}
		}

		public void FinishMission(object mission)
		{
			m_current++;
			m_updatAction?.Invoke(mission);
			if (IsDone)
				m_completedAction?.Invoke(this);
		}

		public void AddMission<T>(IAsyncOperation<T> asyncOperation)
		{
			m_target++;
			asyncOperation.Completed += FinishMission;
		}

		public void AddMission(ResourceRequest asyncOperation)
		{
			m_target++;
			asyncOperation.completed += FinishMission;
		}
	}
}
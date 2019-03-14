using System;
using System.Collections.Generic;

namespace Fundamental
{
	public class CommonDispathcer<T1>
	{
		protected Dictionary<T1, Delegate> _dictEvent = new Dictionary<T1, Delegate>();

		#region Null param
		public void AddEventListener(T1 eventId, Action handle)
		{
			Delegate d = null;
			if (!_dictEvent.TryGetValue(eventId, out d))
			{
				_dictEvent.Add(eventId, handle);
			}
			else
			{
				if (d is Action)
				{
					_dictEvent[eventId] = (Action)d + handle;
				}
				else
				{
					SuperDebug.LogError("event id is " + eventId + " d is " + d.Method + " not match ");
					foreach (var item in d.GetInvocationList())
					{
						SuperDebug.LogError("d invocation is " + item.Method);
					}
				}
			}
		}

		public void RemoveEventListener(T1 eventId, Action handle)
		{
			Delegate d = null;
			if (_dictEvent.TryGetValue(eventId, out d))
			{
				d = (Action)d - handle;
				_dictEvent[eventId] = d;
			}

			if (d == null)
			{
				_dictEvent.Remove(eventId);
			}
		}

		public void EventDispatch(T1 eventId)
		{
			Delegate d = null;
			if (_dictEvent.TryGetValue(eventId, out d))
			{
				Action action = d as Action;
				if (action != null)
				{
					action();
				}
			}
		}
		#endregion

		#region One Param
		public void AddEventListener<T>(T1 eventId, Action<T> handle)
		{
			Delegate d = null;
			if (!_dictEvent.TryGetValue(eventId, out d))
			{
				_dictEvent.Add(eventId, handle);
			}
			else
			{
				if (d is Action<T>)
				{
					_dictEvent[eventId] = (Action<T>)d + handle;
				}
				else
				{
					SuperDebug.LogError("event id is " + eventId + " d is " + d.Method + " not match ");
					foreach (var item in d.GetInvocationList())
					{
						SuperDebug.LogError("d invocation is " + item.Method);
					}
				}
			}
		}

		public void RemoveEventListener<T>(T1 eventId, Action<T> handle)
		{
			Delegate d = null;
			if (_dictEvent.TryGetValue(eventId, out d))
			{
				d = (Action<T>)d - handle;
				_dictEvent[eventId] = d;
			}

			if (d == null)
			{
				_dictEvent.Remove(eventId);
			}
		}

		public void EventDispatch<T>(T1 eventId, T arg1)
		{
			Delegate d = null;
			if (_dictEvent.TryGetValue(eventId, out d))
			{
				Action<T> action = d as Action<T>;
				if (action != null)
				{
					action(arg1);
				}
			}
		}
		#endregion
	}
}
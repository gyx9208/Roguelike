using System;
using System.Collections.Generic;

namespace Fundamental
{
	public interface IObjPoolCtrl
	{
		void Release(PooledClassObject obj);
	}

	public class ClassObjPool<T> : IObjPoolCtrl where T : PooledClassObject, new()
	{
		protected List<object> pool = new List<object>(128);
		private static ClassObjPool<T> instance;

		public static T Get()
		{
			if (instance == null)
			{
				instance = new ClassObjPool<T>();
			}
			if (instance.pool.Count > 0)
			{
				T t = (T)((object)instance.pool[instance.pool.Count - 1]);
				instance.pool.RemoveAt(instance.pool.Count - 1);
				t.holder = instance;
				t.OnUse();
				return t;
			}
			T t2 = Activator.CreateInstance<T>();
			t2.holder = instance;
			t2.OnUse();
			return t2;
		}

		public void Release(PooledClassObject obj)
		{
			T t = obj as T;
			obj.holder = null;
			this.pool.Add(t);
		}
	}
}
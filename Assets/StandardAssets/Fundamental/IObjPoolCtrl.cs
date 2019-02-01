using System;

namespace Fundamental
{
	public interface IObjPoolCtrl
	{
		void Release(PooledClassObject obj);
	}
}
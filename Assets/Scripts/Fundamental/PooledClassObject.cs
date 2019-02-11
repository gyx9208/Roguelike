namespace Fundamental
{
	public abstract class PooledClassObject
	{
		public IObjPoolCtrl holder;

		public abstract void OnUse();

		public abstract void OnRelease();

		public void Release()
		{
			if (this.holder != null)
			{
				this.OnRelease();
				this.holder.Release(this);
			}
		}
	}
}
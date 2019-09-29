namespace Logic.LockStep
{
	public interface ILockStepLogic
	{
		void UpdateLogic(int frame);
		void UpdateRenderPosition(float past);
	}

	public class LockStepLogic
	{
		public static readonly int FRAME = 50;
		public static readonly Fix64 FIX_FRAME = new Fix64(50);

		float _accumilatedTime = 0;
		float _nextGameTime = 0;
		static readonly float _frameLen = 1f / FRAME;
		int _logicFrame = 0;
		ILockStepLogic _updateTarget = null;

		public LockStepLogic()
		{
			Init();
		}

		public void Init()
		{
			_accumilatedTime = 0;
			_nextGameTime = 0;
		}

		public void UpdateLogic()
		{
			_accumilatedTime = _accumilatedTime + UnityEngine.Time.deltaTime;

			while (_accumilatedTime > _nextGameTime)
			{
				_logicFrame += 1;
				_updateTarget.UpdateLogic(_logicFrame);
				_nextGameTime += _frameLen;
			}

			float interpolation = (_accumilatedTime + _frameLen - _nextGameTime) / _frameLen;

			_updateTarget.UpdateRenderPosition(interpolation);
		}

		public void SetCallUnit(ILockStepLogic unit)
		{
			_updateTarget = unit;
		}
	}
}
using Logic.LockStep;
using System;

namespace Logic.Battle
{
	public class Entity : ILockStepLogic
	{
		public FixVector3 Position;
		public FixVector3 Euler;

		Action _updateLocomotion;
		public void UpdateLogic(int frame)
		{
			_updateLocomotion?.Invoke();
		}

		public void UpdateRenderPosition(float past)
		{

		}

		#region locomotion
		public Fix64 MoveSpeed = new Fix64(2) / LockStepLogic.FIX_FRAME;
		public Fix64 RotateSpeed = new Fix64(180) / LockStepLogic.FIX_FRAME;

		FixVector3 _MoveTarget;
		// from 0 to 360
		Fix64 _targetEulerY, _targetSin, _targetCos;

		public void MoveTowards(FixVector3 position)
		{

		}

		public void MoveTowards(Entity target)
		{

		}

		public void MoveByDirection(Fix64 eulerY)
		{
			_targetEulerY = eulerY.ClampAngle();
			var rad = _targetEulerY * Fix64.Deg2Rad;
			_targetSin = Fix64.Sin(rad);
			_targetCos = Fix64.Cos(rad);

			_updateLocomotion = UpdateMoveByDirection;
		}

		private void UpdateMoveByDirection()
		{
			var currentY = Euler.y;
			for (; _targetEulerY - currentY > Fix64.I180; currentY += Fix64.I360) ;
			for (; currentY - _targetEulerY >= Fix64.I180; currentY -= Fix64.I360) ;
			var diff = _targetEulerY - currentY;

			if (diff != Fix64.Zero)
			{
				if (diff.RawValue > 0)
				{
					var add = Fix64.Min(diff, RotateSpeed);
					currentY = currentY + add;
				}
				else
				{
					diff = -diff;
					var minus = Fix64.Min(diff, RotateSpeed);
					currentY = currentY - minus;
				}
				Euler.y = currentY.ClampAngle();
			}

			Position += new FixVector3(MoveSpeed * _targetSin, Fix64.Zero, MoveSpeed * _targetCos);
		}

		public void StopMove()
		{
			_updateLocomotion = null;
		}
		#endregion
	}
}

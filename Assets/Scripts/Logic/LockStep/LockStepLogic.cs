//
// @brief: 帧同步核心逻辑
// @version: 1.0.0
// @author helin
// @date: 8/20/2018
// 
// 
//

namespace Logic.LockStep
{
	public interface ILockStepLogic
	{
		void UpdateLogic();
		void UpdateRenderPosition(float past);
	}

	public class LockStepLogic
	{
		//累计运行的时间
		float _accumilatedTime = 0;

		//下一个逻辑帧的时间
		float _nextGameTime = 0;

		//预定的每帧的时间长度
		static readonly float _frameLen = 1f / 15;

		int _logicFrame = 0;

		//挂载的逻辑对象
		ILockStepLogic _callUnit = null;

		//两帧之间的时间差
		float _interpolation = 0;

		public LockStepLogic()
		{
			Init();
		}

		public void Init()
		{
			_accumilatedTime = 0;

			_nextGameTime = 0;

			_interpolation = 0;
		}

		public void UpdateLogic()
		{
			float deltaTime = 0;

			deltaTime = UnityEngine.Time.deltaTime;

			/**************以下是帧同步的核心逻辑*********************/
			_accumilatedTime = _accumilatedTime + deltaTime;

			//如果真实累计的时间超过游戏帧逻辑原本应有的时间,则循环执行逻辑,确保整个逻辑的运算不会因为帧间隔时间的波动而计算出不同的结果
			while (_accumilatedTime > _nextGameTime)
			{
				//运行与游戏相关的具体逻辑
				_callUnit.UpdateLogic();

				//计算下一个逻辑帧应有的时间
				_nextGameTime += _frameLen;

				//游戏逻辑帧自增
				_logicFrame += 1;
			}

			//计算两帧的时间差,用于运行补间动画
			_interpolation = (_accumilatedTime + _frameLen - _nextGameTime) / _frameLen;

			//更新绘制位置
			_callUnit.UpdateRenderPosition(_interpolation);
			/**************帧同步的核心逻辑完毕*********************/
		}

		//- 设置调用的宿主
		// 
		// @param unit 调用的宿主
		// @return none
		public void SetCallUnit(ILockStepLogic unit)
		{
			_callUnit = unit;
		}
	}

}
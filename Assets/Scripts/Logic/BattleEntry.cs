using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fundamental;
using Logic.LockStep;

namespace Logic.Battle
{
	public enum BattleEntryEnum
	{
		Normal,
		DevLevel
	}

	[AutoSingleton(false)]
	public class BattleEntry : MonoSingleton<BattleEntry>
	{
		public BattleEntryEnum EntryType;

		LockStepLogic _lockStep;
		LevelManager _level;
		bool _gamestart;

		//once
		void Start()
		{
			_lockStep = new LockStepLogic();
			_level = new LevelManager();
			_lockStep.SetCallUnit(_level);

			StartLevel();
		}

		void Update()
		{
			if (_gamestart)
				_lockStep.UpdateLogic();
		}

		//each time
		public void StartLevel()
		{
			switch (EntryType)
			{
				case BattleEntryEnum.DevLevel:
					StartDevLevel();
					break;
			}
		}

		public void StartDevLevel()
		{
			_level.InitDev();
			CommonStartLevel();
		}

		public void CommonStartLevel()
		{
			_lockStep.Init();
			_gamestart = true;
		}
	}
}
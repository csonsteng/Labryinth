using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : Singleton<GameManager>
{
	[SerializeField] private Enemy _enemy;
	[SerializeField] private Transform _finish;

	private GameState _state;
	private GameState _suspendedState;

	public enum GameState
	{
		Initializing,
		Running,
		Paused,
		Ending,
	}
	public static bool IsRunning => NullableInstance != null && Instance._state == GameState.Running;

	private async void Start()
	{
		_state = GameState.Initializing;
		Logger.Disable();
		MazeGenerator.Instance.Redraw();
		PathRenderer.Instance.Generate();
		await UniTask.WaitForEndOfFrame(this);

		Cursor.lockState = CursorLockMode.Locked;
		Cursor.visible = false;

		PlaceEnd();

		Player.Instance.Initialize();
		_enemy.Spawn();
		_state = GameState.Running;
	}

	private void PlaceEnd()
	{
		_finish.localPosition = Maze.EndNode.Position;
	}

	private void OnApplicationPause(bool pause) => SetPauseState(pause);

	private void OnApplicationFocus(bool focus) => SetPauseState(focus);

	private void SetPauseState(bool paused)
	{
		if (paused)
		{ 
			_suspendedState = _state;
			_state = GameState.Paused;
			return;
		}

		_state = _suspendedState;
	}

	private void OnApplicationQuit()
	{
		_state = GameState.Ending;
	}
}

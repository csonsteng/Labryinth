using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
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

	[Button]
	public async void Restart()
	{
		_state = GameState.Ending;
		PathRenderer.Instance.Destroy();
		await UIController.Instance.HideGameOverScreen();
		// todo: have a seprate build and start step. (loading screen? def need a black screen)
		Start();
	}

	public void GameOver()
	{
		_state = GameState.Ending;

		Cursor.lockState = CursorLockMode.Confined;
		Cursor.visible = true;
		UIController.Instance.ShowGameOverScreen();
		// todo: events system? or some better way of handling this kind of stuff
		Player.Instance.OnGameOver();
		Enemy.Instance.OnGameOver();
	}

	private void PlaceEnd()
	{
		_finish.localPosition = Maze.EndNode.Position;
	}

	private void OnApplicationPause(bool pause) => SetPauseState(pause);

	private void OnApplicationFocus(bool focus) => SetPauseState(!focus);

	private void SetPauseState(bool paused)
	{
		if (paused)
		{
			Time.timeScale = 0f;
			_suspendedState = _state;
			_state = GameState.Paused;
			return;
		}
		Time.timeScale = 1;
		_state = _suspendedState;
	}

	private void OnApplicationQuit()
	{
		_state = GameState.Ending;
	}
}

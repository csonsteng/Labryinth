using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : Singleton<GameManager>
{
	[SerializeField] private Enemy _enemy;



	public Maze Maze;

	private async void Start()
	{
		Logger.Disable();
		MazeGenerator.Instance.Redraw();
		PathRenderer.Instance.Generate();
		await UniTask.WaitForEndOfFrame(this);

		Cursor.lockState = CursorLockMode.Locked;
		Cursor.visible = false;

		Player.Instance.Initialize();
		_enemy.Spawn();
	}



}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
	


	[SerializeField] private MazeGenerator _mazeGenerator;
	[SerializeField] private FirstPersonController _player;
	[SerializeField] private PathRenderer _pathRenderer;
	[SerializeField] private Enemy _enemy;

	public Vector3 PlayerPosition => _player.gameObject.transform.position;


	public Maze Maze;
	public static GameManager Instance => GetInstance();
    private static GameManager GetInstance()
	{
		if (_instance == null)
		{
			_instance = FindObjectOfType<GameManager>();
		}
		return _instance;
	}
    private static GameManager _instance;

	private void OnDestroy()
	{
		_instance = null;
	}

	private void Start()
	{
		Logger.Disable();
		Maze = new Maze();
		_mazeGenerator.Redraw();
		_pathRenderer.Generate();
		_player.Initialize();
		_enemy.Spawn();
	}



}

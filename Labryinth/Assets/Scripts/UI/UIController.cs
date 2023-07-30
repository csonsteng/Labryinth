using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIController : Singleton<UIController> 
{
	[SerializeField] private GameOverScreen _gameOverScreen;
	public void ShowGameWonScreen()
	{
		_gameOverScreen.ShowGameWon();
	}
	public void ShowGameLostScreen()
	{
		_gameOverScreen.ShowGameLost();
	}
	public async UniTask HideGameOverScreen()
	{
		await _gameOverScreen.Hide();
	}

}

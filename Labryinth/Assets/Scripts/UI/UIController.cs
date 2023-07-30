using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIController : Singleton<UIController> 
{
	[SerializeField] private GameOverScreen _gameOverScreen;

	public void ShowGameOverScreen()
	{
		_gameOverScreen.Show();
	}
	public async UniTask HideGameOverScreen()
	{
		await _gameOverScreen.Hide();
	}

}

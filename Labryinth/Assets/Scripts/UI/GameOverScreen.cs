using Cysharp.Threading.Tasks;
using DG.Tweening;
using Sirenix.OdinInspector;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameOverScreen : MonoBehaviour
{
    [SerializeField] private Image _screenFader;
	[SerializeField] private GameObject _clickBlocker;

	[SerializeField] private GameOverState _gameLost;
	[SerializeField] private GameOverState _gameWon;

	private GameOverState _targetGameState;

	[System.Serializable]
	public class GameOverState
	{
		public RectTransform RetryButton;
		public RectTransform TextHolder;
		public TextMeshProUGUI[] TextComponents;
		public string Text;
	}

	private void Start()
    {
		HideImmediate();

	}

#if UNITY_EDITOR
	[Button("Turn On Game Won")]
    public void TurnOnGameWon()
    {
		_screenFader.SetAlpha(0.8f);
		_clickBlocker.SetActive(true);
		SetStateImmediate(_gameWon);
		SetStateImmediate(_gameLost, false);
	}
	[Button("Turn On Game Lost")]
	public void TurnOnGameLost()
	{
		_screenFader.SetAlpha(0.8f);
		_clickBlocker.SetActive(true);
		SetStateImmediate(_gameWon, false);
		SetStateImmediate(_gameLost);
	}
#endif

	[Button("Turn Off")]
	private void HideImmediate()
	{
		_screenFader.SetAlpha(0f);
		_clickBlocker.SetActive(false);
		SetStateImmediate(_gameWon, false);
		SetStateImmediate(_gameLost, false);
	}


	public void ShowGameLost()
	{
		_targetGameState = _gameLost;
		Show();
	}

	public void ShowGameWon()
	{
		_targetGameState = _gameWon;
		Show();
	}

	private async void Show()
    {
		ReadyStateForAnimateIn();
		_clickBlocker.SetActive(true);
		_ = _screenFader.DOFade(0.8f, 0.25f);
		await AnimateText();
        await UniTask.Delay(300);
        _ = _targetGameState.RetryButton.DOScale(1f, 0.5f);
    }

    public async UniTask Hide()
	{
		if(_targetGameState == null)
		{
			return;
		}
		_ = _screenFader.DOFade(0f, 0.5f);
		_ = _targetGameState.RetryButton.DOScale(0f, 0.5f);
		await _targetGameState.TextHolder.DOScale(0f, 0.5f);

		_clickBlocker.SetActive(false);
	}

    /// <summary>
    /// Exposed for Inspector
    /// </summary>
    public void Retry()
    {
        GameManager.Instance.Restart();
    }

	private async UniTask AnimateText()
	{
		var typingText = new StringBuilder();
		string displayString;
		foreach(var character in _targetGameState.Text)
		{
			typingText.Append('|');
			displayString = typingText.ToString();
			foreach (var component in _targetGameState.TextComponents)
			{
				component.text = displayString;
			}
			await UniTask.Delay(50);
			typingText.Replace('|', character);
		}

		displayString = typingText.ToString();
		foreach (var component in _targetGameState.TextComponents)
		{
			component.text = displayString;
		}
	}

    private void ReadyStateForAnimateIn()
    {
		_targetGameState.RetryButton.localScale = Vector3.zero;
		_targetGameState.RetryButton.gameObject.SetActive(true);

		_targetGameState.TextHolder.gameObject.SetActive(true);
		_targetGameState.TextHolder.localScale = Vector3.one;
		foreach (var component in _targetGameState.TextComponents) 
		{
			component.text = "";
		}
	}

	private void SetStateImmediate(GameOverState state, bool on = true)
	{
		state.RetryButton.gameObject.SetActive(on);
		state.TextHolder.gameObject.SetActive(on);
		if (on)
		{
			foreach (var component in state.TextComponents)
			{
				component.text = state.Text;
			}
		}
	}
}

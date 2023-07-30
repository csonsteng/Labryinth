using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class GameOverScreen : MonoBehaviour
{
    [SerializeField] private Image _screenFader;
	[SerializeField] private GameObject _clickBlocker;
	[SerializeField] private RectTransform _gameOverText;
	[SerializeField] private RectTransform _retryButton;
	// Start is called before the first frame update
	private void Start()
    {
        _screenFader.SetAlpha(0f);
        _clickBlocker.SetActive(false);
        _gameOverText.gameObject.SetActive(false);
        _retryButton.gameObject.SetActive(false);
    }

    public async void Show()
    {
		ReadyRectForAnimateIn(_gameOverText);
		ReadyRectForAnimateIn(_retryButton);
		_clickBlocker.SetActive(true);
		_ = _screenFader.DOFade(0.8f, 0.25f);
        await _gameOverText.DOScale(1f, 0.5f);
        await UniTask.Delay(300);
        _ = _retryButton.DOScale(1f, 0.5f);
    }

    public async UniTask Hide()
	{
		_ = _screenFader.DOFade(0f, 0.5f);
		_ = _retryButton.DOScale(0f, 0.5f);
		await _gameOverText.DOScale(0f, 0.5f);

		_clickBlocker.SetActive(false);
	}

    /// <summary>
    /// Exposed for Inspector
    /// </summary>
    public void Retry()
    {
        GameManager.Instance.Restart();
    }

    private void ReadyRectForAnimateIn(RectTransform rectTransform)
    {
		rectTransform.localScale = Vector3.zero;
		rectTransform.gameObject.SetActive(true);
	}
}

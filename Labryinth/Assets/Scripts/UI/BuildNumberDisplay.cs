using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class BuildNumberDisplay : MonoBehaviour
{
	[SerializeField] private TextMeshProUGUI _display;

	private void Start()
	{
		_display.text = Application.version;
	}
}

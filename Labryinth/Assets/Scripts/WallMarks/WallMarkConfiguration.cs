using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class WallMarkConfiguration : ScriptableObject
{
	[SerializeField] private Material _material;
	[SerializeField] private Sprite _icon;

	public Sprite Sprite => _icon;
	public Material Material => _material;
}

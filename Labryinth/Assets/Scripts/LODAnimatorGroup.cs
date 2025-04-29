using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LODAnimatorGroup : MonoBehaviour
{
	[SerializeField] Animator[] _animators;

	public void SetTrigger(string name)
	{
		foreach (var animator in _animators)
		{
			animator.SetTrigger(name);
		}
	}
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Sounds/Sound Effect")]
public class SoundEffect : ScriptableObject
{
	public List<AudioClip> sounds = new List<AudioClip>();

	public AudioClip GetClip()
	{
		var randomIndex = Random.Range(0, sounds.Count);
		return sounds[randomIndex];
	}

	public void Play(AudioSource audioSource)
	{
		audioSource.clip = GetClip();
		audioSource.Play();
	}

}

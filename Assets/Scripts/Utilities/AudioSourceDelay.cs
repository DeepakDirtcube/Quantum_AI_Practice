using UnityEngine;

namespace SimpleFPS
{
	/// <summary>
	/// Delay AudioSource play. This is useful to prevent audio distortion when multiple
	/// same sounds are playing at the same time (e.g. shotgun projectiles impact).
	/// </summary>
	[RequireComponent(typeof(AudioSource)), DefaultExecutionOrder(-100)]
	public class AudioSourceDelay : MonoBehaviour
	{
		public float DelayFrom;
		public float DelayTo;

		private AudioSource _audioSource;

		private void Awake()
		{
			_audioSource = GetComponent<AudioSource>();
			_audioSource.playOnAwake = false;
		}

		private void OnEnable()
		{
			_audioSource.PlayDelayed(Random.Range(DelayFrom, DelayTo));
		}
	}
}

using Quantum;
using UnityEngine;

namespace SimpleFPS
{
	/// <summary>
	/// Handles footstep sound effects.
	/// </summary>
	public class Footsteps : QuantumEntityViewComponent<SceneContext>
	{
		public AudioClip[] FootstepClips;
		public AudioSource FootstepSource;
		public float       FootstepDuration = 0.5f;

		private float _footstepCooldown;
		private bool  _wasGrounded;

		public override void OnActivate(Frame frame)
		{
			// We start as grounded.
			_wasGrounded = true;

			if (frame.TryGet(EntityRef, out Player player) == false)
				return;

			bool isLocal = Game.PlayerIsLocal(player.PlayerRef);
			if (isLocal)
			{
				// Local player has slightly quieter sounds.
				FootstepSource.volume -= 0.1f;
			}
		}

		public override void OnUpdateView()
		{
			Frame predictedFrame = PredictedFrame;
			if (predictedFrame.Exists(EntityRef) == false)
				return;

			KCC kcc = predictedFrame.Get<KCC>(EntityRef);

			if (kcc.IsGrounded != _wasGrounded)
			{
				// When player jumps or lands, always play footstep.
				PlayFootstep();

				_wasGrounded = kcc.IsGrounded;
			}

			if (kcc.IsGrounded == false)
				return;

			_footstepCooldown -= Time.deltaTime;

			if (kcc.RealSpeed.AsFloat < 0.5f)
				return;

			if (_footstepCooldown <= 0f)
			{
				PlayFootstep();
			}
		}

		private void PlayFootstep()
		{
			var clip = FootstepClips[Random.Range(0, FootstepClips.Length)];
			FootstepSource.PlayOneShot(clip);

			_footstepCooldown = FootstepDuration;
		}
	}
}

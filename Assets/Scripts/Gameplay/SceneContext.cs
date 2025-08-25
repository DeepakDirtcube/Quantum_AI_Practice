using Quantum;
using UnityEngine;

namespace SimpleFPS
{
	public class SceneContext : MonoBehaviour, IQuantumViewContext
	{
		public GameplayView Gameplay;
		public PlayerInput  PlayerInput;

		public PlayerRef    LocalPlayer;
		public EntityRef    LocalPlayerEntity;
		public PlayerView   LocalPlayerView;
	}
}

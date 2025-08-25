using Quantum;

namespace SimpleFPS
{
	public class GameplayView : QuantumEntityViewComponent<SceneContext>
	{
		public override void OnActivate(Frame frame)
		{
			QuantumEvent.Subscribe<EventGameplayStateChanged>(this, OnStateChanged);
		}

		public override void OnDeactivate()
		{
			QuantumEvent.UnsubscribeListener(this);
		}

		private void OnStateChanged(EventGameplayStateChanged callback)
		{
			if (callback.State == EGameplayState.Finished)
			{
				var client = QuantumRunner.Default.NetworkClient;
				if (client.LocalPlayer.IsMasterClient)
				{
					// Close room when gameplay is finished
					client.CurrentRoom.IsOpen = false;
				}
			}
		}
	}
}

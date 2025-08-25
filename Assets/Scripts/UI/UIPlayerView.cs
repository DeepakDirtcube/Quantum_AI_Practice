using TMPro;
using UnityEngine;
using Quantum;

namespace SimpleFPS
{
	public class UIPlayerView : MonoBehaviour
	{
		public TextMeshProUGUI Nickname;
		public UIHealth        Health;
		public UIWeapons       Weapons;
		public UICrosshair     Crosshair;

		public void UpdatePlayer(PlayerRef playerRef, PlayerView playerView)
		{
			RuntimePlayer playerData = playerView.PredictedFrame.GetPlayerData(playerRef);

			Nickname.text = playerData.PlayerNickname;

			var health = playerView.GetPredictedQuantumComponent<Health>();

			Health.UpdateHealth(health);
			Weapons.UpdateWeapons(playerView.GetComponent<WeaponsView>());

			Crosshair.gameObject.SetActive(health.IsAlive);
		}
	}
}

using TMPro;
using UnityEngine;
using Quantum;

namespace SimpleFPS
{
	/// <summary>
	/// View showed at the end of the match.
	/// </summary>
	public class UIGameOverView : MonoBehaviour
	{
		public TextMeshProUGUI Winner;
		public GameObject      VictoryGroup;
		public GameObject      DefeatGroup;
		public AudioSource     GameOverMusic;

		private GameUI _gameUI;
		private EGameplayState _lastState;

		// Called from button OnClick event.
		public void GoToMenu()
		{
			_gameUI.GoToMenu();
		}

		private void Awake()
		{
			_gameUI = GetComponentInParent<GameUI>();
		}

		private void Update()
		{
			if (_gameUI.Frame == null)
				return;

			// Unlock cursor.
			Cursor.lockState = CursorLockMode.None;
			Cursor.visible = true;

			var gameplay = _gameUI.Frame.GetSingleton<Gameplay>();

			if (_lastState == gameplay.State)
				return;

			GameOverMusic.PlayDelayed(1f);

			_lastState = gameplay.State;

			bool localPlayerIsWinner = false;
			Winner.text = string.Empty;

			foreach (var playerPair in _gameUI.Frame.ResolveDictionary(gameplay.PlayerData))
			{
				if (playerPair.Value.StatisticPosition != 1)
					continue;

				var playerData = _gameUI.Frame.GetPlayerData(playerPair.Key);

				Winner.text = $"Winner is {playerData.PlayerNickname}";
				localPlayerIsWinner = playerPair.Key == _gameUI.Context.LocalPlayer;
			}

			VictoryGroup.SetActive(localPlayerIsWinner);
			DefeatGroup.SetActive(localPlayerIsWinner == false);
		}
	}
}

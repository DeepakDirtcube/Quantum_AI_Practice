using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using Quantum;

namespace SimpleFPS
{
	/// <summary>
	/// Main UI script that stores references to other elements (views).
	/// </summary>
	[DefaultExecutionOrder(-1)]
	public class GameUI : MonoBehaviour
	{
		public SceneContext   Context;
		public Frame          Frame   { get; private set; }

		public UIPlayerView   PlayerView;
		public UIGameplayView GameplayView;
		public UIGameOverView GameOverView;
		public GameObject     ScoreboardView;
		public GameObject     MenuView;
		public UISettingsView SettingsView;
		public GameObject     DisconnectedView;

		public void OnGameDestroyed(CallbackGameDestroyed callback)
		{
			if (GameOverView.gameObject.activeSelf)
				return; // Regular shutdown - GameOver already active

			ScoreboardView.SetActive(false);
			SettingsView.gameObject.SetActive(false);
			MenuView.gameObject.SetActive(false);

			DisconnectedView.SetActive(true);
		}

		public void GoToMenu()
		{
			QuantumCallback.UnsubscribeListener<CallbackGameDestroyed>(this);

			if (QuantumRunner.Default != null)
			{
				QuantumRunner.Default.Shutdown();
			}

			SceneManager.LoadScene("Startup");
		}

		private void Awake()
		{
			PlayerView.gameObject.SetActive(false);
			MenuView.SetActive(false);
			SettingsView.gameObject.SetActive(false);
			DisconnectedView.SetActive(false);

			SettingsView.LoadSettings();

			// Make sure the cursor starts unlocked
			Cursor.lockState = CursorLockMode.None;
			Cursor.visible = true;

			QuantumEvent.Subscribe<EventPlayerKilled>(this, OnPlayerKilled);
			QuantumCallback.Subscribe<CallbackGameDestroyed>(this, OnGameDestroyed);
		}

		private void OnDestroy()
		{
			QuantumCallback.UnsubscribeListener<CallbackGameDestroyed>(this);
			QuantumEvent.UnsubscribeListener<EventPlayerKilled>(this);
		}

		private void Update()
		{
			Frame = QuantumRunner.DefaultGame?.Frames?.Predicted;

			if (Application.isBatchMode == true)
				return;
			if (Frame == null)
				return;

			var gameplay = Frame.GetSingleton<Gameplay>();

			var keyboard = Keyboard.current;
			bool gameplayActive = gameplay.State < EGameplayState.Finished;

			ScoreboardView.SetActive(gameplayActive && keyboard != null && keyboard.tabKey.isPressed);

			if (gameplayActive && keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
			{
				MenuView.SetActive(!MenuView.activeSelf);
			}

			GameplayView.gameObject.SetActive(gameplayActive);
			GameOverView.gameObject.SetActive(gameplayActive == false);

			if (Context.LocalPlayerView != null && Frame.Exists(Context.LocalPlayerEntity))
			{
				PlayerView.gameObject.SetActive(gameplayActive);
				PlayerView.UpdatePlayer(Context.LocalPlayer, Context.LocalPlayerView);
			}
			else
			{
				PlayerView.gameObject.SetActive(false);
			}
		}

		private void OnPlayerKilled(EventPlayerKilled playerKilled)
		{
			string killerNickname = "";
			string victimNickname = "";

			RuntimePlayer killerData = Frame.GetPlayerData(playerKilled.KillerPlayerRef);
			RuntimePlayer victimData = Frame.GetPlayerData(playerKilled.VictimPlayerRef);

			if (killerData != null)
			{
				killerNickname = killerData.PlayerNickname;
			}

			if (victimData != null)
			{
				victimNickname = victimData.PlayerNickname;
			}

			GameplayView.KillFeed.ShowKill(killerNickname, victimNickname, playerKilled.WeaponType, playerKilled.IsCriticalKill);
		}
	}
}

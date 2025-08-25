using UnityEngine.Scripting;

namespace Quantum
{
	/// <summary>
	/// Gameplay system controls state of the game, spawns players, maintains statistics, checks win conditions.
	/// </summary>
	[Preserve]
	public unsafe class GameplaySystem : SystemMainThread, ISignalOnPlayerAdded, ISignalOnPlayerRemoved, ISignalPlayerKilled
	{
		public override void Update(Frame frame)
		{
			var gameplay = frame.Unsafe.GetPointerSingleton<Gameplay>();

			// Start gameplay when there are enough players connected.
			if (gameplay->State == EGameplayState.Skirmish && frame.ComponentCount<Player>() > 1)
			{
				gameplay->StartGameplay(frame);
			}

			if (gameplay->State == EGameplayState.Running)
			{
				gameplay->RemainingTime -= frame.DeltaTime;

				if (gameplay->RemainingTime <= 0)
				{
					gameplay->StopGameplay(frame);
				}
			}

			if (gameplay->State != EGameplayState.Finished)
			{
				gameplay->TryRespawnPlayers(frame);
			}
		}

		void ISignalOnPlayerAdded.OnPlayerAdded(Frame frame, PlayerRef playerRef, bool firstTime)
		{
			var gameplay = frame.Unsafe.GetPointerSingleton<Gameplay>();
			gameplay->ConnectPlayer(frame, playerRef);
		}

		void ISignalOnPlayerRemoved.OnPlayerRemoved(Frame frame, PlayerRef playerRef)
		{
			var gameplay = frame.Unsafe.GetPointerSingleton<Gameplay>();
			gameplay->DisconnectPlayer(frame, playerRef);
		}

		void ISignalPlayerKilled.PlayerKilled(Frame frame, PlayerRef killerPlayerRef, PlayerRef victimPlayerRef, byte weaponType, QBoolean isCriticalKill)
		{
			var gameplay = frame.Unsafe.GetPointerSingleton<Gameplay>();
			var players = frame.ResolveDictionary(gameplay->PlayerData);

			// Update statistics of the killer player.
			if (players.TryGetValue(killerPlayerRef, out PlayerData killerData))
			{
				killerData.Kills++;
				killerData.LastKillFrame = frame.Number;
				players[killerPlayerRef] = killerData;
			}

			// Update statistics of the victim player.
			if (players.TryGetValue(victimPlayerRef, out PlayerData playerData))
			{
				playerData.Deaths++;
				playerData.IsAlive = false;
				playerData.RespawnTimer = gameplay->PlayerRespawnTime;
				players[victimPlayerRef] = playerData;
			}

			frame.Events.PlayerKilled(killerPlayerRef, victimPlayerRef, weaponType, isCriticalKill);

			gameplay->RecalculateStatisticPositions(frame);
		}
	}
}

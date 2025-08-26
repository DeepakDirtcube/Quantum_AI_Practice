using System.Collections.Generic;

namespace Quantum
{
	public unsafe partial struct Gameplay
	{
		public bool IsDoubleDamageActive => State == EGameplayState.Running && RemainingTime < DoubleDamageDuration;

		public void ConnectPlayer(Frame frame, PlayerRef playerRef)
		{
			var players = frame.ResolveDictionary(PlayerData);

			if (players.TryGetValue(playerRef, out var playerData) == false)
			{
				playerData = new PlayerData();
				playerData.PlayerRef = playerRef;
				playerData.StatisticPosition = int.MaxValue;
				playerData.IsAlive = false;
				playerData.IsConnected = false;
			}

			if (playerData.IsConnected)
				return;

			Log.Warn($"{playerRef} connected.");

			playerData.IsConnected = true;
			players[playerRef] = playerData;

			RespawnPlayer(frame, playerRef);
			RecalculateStatisticPositions(frame);
		}

		public void DisconnectPlayer(Frame frame, PlayerRef playerRef)
		{
			var players = frame.ResolveDictionary(PlayerData);

			if (players.TryGetValue(playerRef, out var playerData))
			{
				if (playerData.IsConnected)
				{
					Log.Warn($"{playerRef} disconnected.");
				}

				playerData.IsConnected = false;
				playerData.IsAlive = false;
				players[playerRef] = playerData;
			}

			var playerEntity = frame.GetPlayerEntity(playerRef);
			if (playerEntity.IsValid)
			{
				frame.Destroy(playerEntity);
			}

			RecalculateStatisticPositions(frame);
		}

		public void StartGameplay(Frame frame)
		{
			SetState(frame, EGameplayState.Running);
			RemainingTime = GameDuration;

			// Reset player data after skirmish and respawn players.
			var players = frame.ResolveDictionary(PlayerData);
			foreach (var playerPair in players)
			{
				var playerData = playerPair.Value;

				playerData.RespawnTimer = 0;
				playerData.Kills = 0;
				playerData.Deaths = 0;
				playerData.StatisticPosition = int.MaxValue;

				players[playerData.PlayerRef] = playerData;

				RespawnPlayer(frame, playerData.PlayerRef);
			}
		}

		public void StopGameplay(Frame frame)
		{
			RecalculateStatisticPositions(frame);

			SetState(frame, EGameplayState.Finished);
		}

		public void TryRespawnPlayers(Frame frame)
		{
			var players = frame.ResolveDictionary(PlayerData);
			foreach (var playerPair in players)
			{
				var playerData = playerPair.Value;
				if (playerData.RespawnTimer <= 0)
					continue;

				playerData.RespawnTimer -= frame.DeltaTime;
				players[playerData.PlayerRef] = playerData;

				if (playerData.RespawnTimer <= 0)
				{
					RespawnPlayer(frame, playerPair.Key);
				}
			}
		}

		public void RecalculateStatisticPositions(Frame frame)
		{
			if (State == EGameplayState.Finished)
				return;

			var tempPlayerData = new List<PlayerData>();

			var players = frame.ResolveDictionary(PlayerData);
			foreach (var pair in players)
			{
				tempPlayerData.Add(pair.Value);
			}

			tempPlayerData.Sort((a, b) =>
			{
				if (a.Kills != b.Kills)
					return b.Kills.CompareTo(a.Kills);

				return a.LastKillFrame.CompareTo(b.LastKillFrame);
			});

			for (int i = 0; i < tempPlayerData.Count; i++)
			{
				var playerData = tempPlayerData[i];
				playerData.StatisticPosition = playerData.Kills > 0 ? i + 1 : int.MaxValue;

				players[playerData.PlayerRef] = playerData;
			}
		}

		private void SetState(Frame frame, EGameplayState state)
		{
			State = state;
			frame.Events.GameplayStateChanged(state);
		}

		private void RespawnPlayer(Frame frame, PlayerRef playerRef)
		{
			var players = frame.ResolveDictionary(PlayerData);

			// Despawn old player object if it exists.
			var playerEntity = frame.GetPlayerEntity(playerRef);
			if (playerEntity.IsValid)
			{
				frame.Destroy(playerEntity);
			}

			// Don't spawn the player for disconnected clients.
			if (players.TryGetValue(playerRef, out PlayerData playerData) == false || playerData.IsConnected == false)
				return;

			// Update player data.
			playerData.IsAlive = true;
			players[playerRef] = playerData;

			// var runtimePlayer = frame.GetPlayerData(playerRef);
			// playerEntity = frame.Create(runtimePlayer.PlayerAvatar);
			var avatarRef = GetAvatarFor(frame, playerRef);     // override-first
			if (avatarRef.IsValid == false)
			{
				Log.Error($"RespawnPlayer: No avatar configured for {playerRef}. (RuntimeConfig override missing and runner slot has no avatar)");
				return;                                           // avoid NRE
			}
			playerEntity = frame.Create(avatarRef);

			frame.AddOrGet<Player>(playerEntity, out var player);
			player->PlayerRef = playerRef;

			var playerTransform = frame.Unsafe.GetPointer<Transform3D>(playerEntity);

			SpawnPointData spawnPoint = GetSpawnPoint(frame);
			playerTransform->Position = spawnPoint.Position;
			playerTransform->Rotation = spawnPoint.Rotation;

			var playerKCC = frame.Unsafe.GetPointer<KCC>(playerEntity);
			playerKCC->SetLookRotation(spawnPoint.Rotation.AsEuler.XY);
		}

		private SpawnPointData GetSpawnPoint(Frame frame)
		{
			var gameplayData = frame.FindAsset<GameplayData>(frame.Map.UserAsset);

			SpawnPointData spawnPointData = default;
			int spawnPointIndex = 0;

			var recentSpawnPoints = frame.ResolveList(RecentSpawnPoints);
			int randomOffset = frame.RNG->Next(0, gameplayData.SpawnPoints.Length);

			// Iterate over all spawn points in the scene.
			for (int i = 0; i < gameplayData.SpawnPoints.Length; i++)
			{
				spawnPointIndex = (randomOffset + i) % gameplayData.SpawnPoints.Length;
				spawnPointData = gameplayData.SpawnPoints[spawnPointIndex];

				if (recentSpawnPoints.Contains(spawnPointIndex) == false)
					break;
			}

			// Add spawn point to list of recently used spawn points.
			recentSpawnPoints.Add(spawnPointIndex);

			// Ignore only last 3 spawn points.
			if (recentSpawnPoints.Count > 3)
			{
				recentSpawnPoints.RemoveAt(0);
			}

			return spawnPointData;
		}

		// inside partial struct Gameplay
		private AssetRef<EntityPrototype> GetAvatarFor(Frame frame, PlayerRef playerRef)
		{
			// 1) Try override from RuntimeConfig by slot (PlayerRef is 1-based)
			var cfg = frame.RuntimeConfig.CollectorsSampleConfig;
			if (cfg.Bots != null)
			{
				int index = (int)playerRef - 1;
				if (index >= 0 && index < cfg.Bots.Length)
				{
					var overrideRef = cfg.Bots[index];              // AssetRef<EntityPrototype>
					if (overrideRef.IsValid)
						return overrideRef;                           // use bot prefab from RuntimeConfig
				}
			}

			// 2) Fallback to runner-configured avatar (human/default)
			var rp = frame.GetPlayerData(playerRef);
			return rp.PlayerAvatar;                             // AssetRef<EntityPrototype>
		}

	}
}

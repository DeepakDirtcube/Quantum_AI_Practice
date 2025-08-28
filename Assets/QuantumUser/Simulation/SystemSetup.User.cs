using System.Collections.Generic;
using Quantum.BotSDK;

namespace Quantum
{
	public static partial class DeterministicSystemSetup
	{
		static partial void AddSystemsUser(ICollection<SystemBase> systems, RuntimeConfig gameConfig, SimulationConfig simulationConfig, SystemsConfig systemsConfig)
		{
			var list = systems as List<SystemBase>;
			if (list == null) return;

			int IndexOf<T>() where T : SystemBase => list.FindIndex(s => s is T);
			bool Has<T>() where T : SystemBase => IndexOf<T>() >= 0;

			// 1) Make sure Bot SDK runtime is FIRST (initializes HFSM & Blackboard)
			if (!Has<Quantum.BotSDK.BotSDKSystem>())
			{
				list.Insert(0, new Quantum.BotSDK.BotSDKSystem());
			}

			// (Optional) If you want the on-screen HFSM debugger, insert it right after:
			// if (!Has<Quantum.BotSDK.BotSDKDebuggerSystem>()) {
			//   list.Insert(IndexOf<Quantum.BotSDK.BotSDKSystem>() + 1, new Quantum.BotSDK.BotSDKDebuggerSystem());
			// }

			// 2) Connect bot player slots (your existing system)
			if (!Has<FpsBotsInitSystem>())
			{
				list.Insert(IndexOf<Quantum.BotSDK.BotSDKSystem>() + 1, new FpsBotsInitSystem());
			}

			// 3) Ensure the HFSM→Input bridge exists and sits BEFORE PlayerSystem
			var bridgeIdx = IndexOf<BotHFSMInputSystem>();
			if (bridgeIdx < 0)
			{
				var bridge = new BotHFSMInputSystem(); // uses fallback key names ("TargetPos","MoveDir",...)
				var iPlayer = IndexOf<PlayerSystem>();
				if (iPlayer >= 0) list.Insert(iPlayer, bridge);
				else list.Add(bridge); // fallback if PlayerSystem not present yet
			}
			else
			{
				// If it already exists but is AFTER PlayerSystem, move it up
				var iPlayer = IndexOf<PlayerSystem>();
				if (iPlayer >= 0 && bridgeIdx > iPlayer)
				{
					var bridge = list[bridgeIdx];
					list.RemoveAt(bridgeIdx);
					list.Insert(iPlayer, bridge);
				}
			}

			// (Optional) Print the final order once to verify
			// Log.Info("=== System order ===");
			// for (int i = 0; i < list.Count; i++) Log.Info($"{i}: {list[i].GetType().Name}");
		}
	}
}

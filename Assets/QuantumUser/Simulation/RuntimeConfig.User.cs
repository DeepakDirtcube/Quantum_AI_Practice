using System;
using Photon.Deterministic;

namespace Quantum
{

	[Serializable]
	public struct CollectorsSampleRuntimeConfig
	{
		// public AssetRef<EntityPrototype> CollectiblePrototype;

		// The prototype to be spawned for players, by default without any bot specific component
		public AssetRef<EntityPrototype> PlayerCollectorPrototype;

		// Which HFSMs should take control when replacing disconnected players
		public AssetRef<HFSMRoot> ReplacementHFSM;
		public AssetRef<AIBlackboard> ReplacementAIBlackboard;

		// Should players be replaced if they disconnect?
		public bool ReplaceOnDisconnect;

		// Should the room be filled if not enough players connect to it?
		public bool FillRoom;

		// How many time should be waited before filling the room with bots?
		public FP FillRoomCooldown;

		// Bots to be created independently of player replacement and fill room features
		public AssetRef<EntityPrototype>[] Bots;

		public FPVector2[] SpawnPositions;      // if true, uses these

	}
	public partial class RuntimeConfig
	{
		public CollectorsSampleRuntimeConfig CollectorsSampleConfig;
	}
}

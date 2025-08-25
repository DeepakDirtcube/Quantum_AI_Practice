using System;
using Photon.Deterministic;

namespace Quantum
{
	/// <summary>
	/// Asset that holds gameplay related data baked from scene objects.
	/// </summary>
	public class GameplayData : AssetObject
	{
		public SpawnPointData[] SpawnPoints;
	}

	[Serializable]
	public struct SpawnPointData
	{
		public FPVector3    Position;
		public FPQuaternion Rotation;
	}
}

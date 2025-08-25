namespace Quantum
{
	public unsafe partial class Frame
	{
		public EntityRef GetPlayerEntity(PlayerRef playerRef)
		{
			foreach (EntityComponentPair<Player> ecp in GetComponentIterator<Player>())
			{
				if (ecp.Component.PlayerRef == playerRef)
					return ecp.Entity;
			}

			return EntityRef.None;
		}

	#if UNITY_ENGINE
	#endif
	}
}

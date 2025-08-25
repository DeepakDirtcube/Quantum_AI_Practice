namespace Quantum
{
	public static class LagCompensationUtility
	{
		private const int COLLIDER_LAYER_OFFSET = 16;

		public static int GetProxyColliderLayer(PlayerRef playerRef)
		{
			return playerRef + COLLIDER_LAYER_OFFSET;
		}

		public static int GetProxyCollisionLayerMask(PlayerRef playerRef)
		{
			return 1 << (playerRef + COLLIDER_LAYER_OFFSET);
		}
	}
}


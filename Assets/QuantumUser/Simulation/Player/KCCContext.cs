namespace Quantum
{
	using Quantum.Physics3D;

	/// <summary>
	/// This partial class implements collision filtering based on player health.
	/// With this a player will be able to move through dead players.
	/// </summary>
	public unsafe partial class KCCContext
	{
		partial void PrepareUserContext()
		{
			ResolveCollision = ResolvePlayerCollision;
		}

		/// <summary>
		/// Use this to reset your data in KCCContext.
		/// </summary>
		private bool ResolvePlayerCollision(KCCContext context, Hit3D hit)
		{
			if (context.Entity.IsValid && hit.Entity.IsValid && context.Frame.TryGet(context.Entity, out Health health) && context.Frame.TryGet(hit.Entity, out Health otherHealth))
				return health.IsAlive && otherHealth.IsAlive;

			return true;
		}
	}
}

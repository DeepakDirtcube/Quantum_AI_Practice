using UnityEngine.Scripting;

namespace Quantum
{
	/// <summary>
	/// LateLagCompensationSystem destroys all proxies created in EarlyLagCompensationSystem and records transforms of entities with LagCompensationTarget component.
	/// This system should run last.
	/// </summary>
	[Preserve]
	public unsafe class LateLagCompensationSystem : SystemMainThread
	{
		public override void Update(Frame frame)
		{
			// Cleanup - destroy all existing proxies.
			var proxies = frame.Filter<LagCompensationProxy>();
			proxies.UseCulling = false;

			while (proxies.NextUnsafe(out EntityRef proxyEntity, out LagCompensationProxy* proxy))
			{
				frame.Destroy(proxyEntity);
			}

			if (frame.IsVerified)
			{
				// Store transforms of lag compensated entities.
				// It is fine to record in verified frames only because the snapshot interpolation happens between two verified frames.
				var targets = frame.Filter<Transform3D, LagCompensationTarget>();
				while (targets.NextUnsafe(out EntityRef entity, out Transform3D* transform, out LagCompensationTarget* target))
				{
					target->StoreTransform(frame.Number, transform);
				}
			}
		}
	}
}


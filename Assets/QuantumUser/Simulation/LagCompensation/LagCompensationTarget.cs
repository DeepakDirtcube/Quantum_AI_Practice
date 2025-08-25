using Photon.Deterministic;

namespace Quantum
{
	unsafe partial struct LagCompensationTarget
	{
		public void StoreTransform(int tick, Transform3D* transform)
		{
			BufferIndex = (BufferIndex + 1) % Buffer.Length;
			Buffer[BufferIndex] = *transform;
		}

		public Transform3D GetInterpolatedTransform(int offset, FP alpha)
		{
			offset = System.Math.Clamp(offset, 0, Buffer.Length - 1);

			int fromIndex = ((BufferIndex - offset) + Buffer.Length) % Buffer.Length;
			var fromTransform = Buffer[fromIndex];

			int toIndex = (fromIndex + 1) % Buffer.Length;
			var toTransform = Buffer[toIndex];

			var interpolatedTransform = new Transform3D();
			interpolatedTransform.Position = FPVector3.Lerp(fromTransform.Position, toTransform.Position, alpha);
			interpolatedTransform.Rotation = FPQuaternion.Slerp(fromTransform.Rotation, toTransform.Rotation, alpha);

			return interpolatedTransform;
		}
	}
}


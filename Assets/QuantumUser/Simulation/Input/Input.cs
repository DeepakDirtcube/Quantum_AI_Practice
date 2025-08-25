using Photon.Deterministic;

namespace Quantum
{
	partial struct Input
	{
		// The interpolation alpha is encoded to a single byte.
		public FP InterpolationAlpha
		{
			get => ((FP)InterpolationAlphaEncoded) / 255;
			set
			{
				FP clamped = FPMath.Clamp(value * 255, 0, 255);
				InterpolationAlphaEncoded = (byte)clamped.AsInt;
			}
		}
	}
}


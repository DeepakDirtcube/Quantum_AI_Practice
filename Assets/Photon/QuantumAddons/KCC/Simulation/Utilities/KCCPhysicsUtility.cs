namespace Quantum
{
	using Photon.Deterministic;

	public static unsafe partial class KCCPhysicsUtility
	{
		public static bool ProjectOnGround(FPVector3 groundNormal, FPVector3 vector, out FPVector3 projectedVector)
		{
			FP dot1 = FPVector3.Dot(FPVector3.Up, groundNormal);
			FP dot2 = -FPVector3.Dot(vector, groundNormal);

			if (FPMath.Abs(dot1) > FP.EN4)
			{
				projectedVector = new FPVector3(vector.X, vector.Y + dot2 / dot1, vector.Z);
				return true;
			}

			projectedVector = default;
			return false;
		}

		public static void ProjectVerticalPenetration(ref FPVector3 direction, ref FP distance)
		{
			FPVector3 desiredCorrection    = direction * distance;
			FPVector2 desiredCorrectionXZ  = desiredCorrection.XZ;
			FP        correctionDistanceXZ = desiredCorrectionXZ.Magnitude;

			if (correctionDistanceXZ >= FP.EN4)
			{
				FP reflectedDistanceXZ = desiredCorrection.Y * desiredCorrection.Y / correctionDistanceXZ;

				direction = desiredCorrectionXZ.XOY / correctionDistanceXZ;
				distance  = correctionDistanceXZ + reflectedDistanceXZ;
			}
		}

		public static void ProjectHorizontalPenetration(ref FPVector3 direction, ref FP distance)
		{
			FPVector3 desiredCorrection = direction * distance;

			direction = FPVector3.Up;
			distance  = FP._0;

			if (desiredCorrection.Y > -FP.EN4 && desiredCorrection.Y < FP.EN4)
				return;

			distance = desiredCorrection.Y + (desiredCorrection.X * desiredCorrection.X + desiredCorrection.Z * desiredCorrection.Z) / desiredCorrection.Y;

			if (distance < FP._0)
			{
				direction = -direction;
				distance  = -distance;
			}
		}

		public static FPVector3 GetAcceleration(FPVector3 velocity, FPVector3 direction, FPVector3 axis, FP maxSpeed, bool clampSpeed, FP inputAcceleration, FP constantAcceleration, FP relativeAcceleration, FP proportionalAcceleration, FP deltaTime)
		{
			if (inputAcceleration <= FP._0)
				return FPVector3.Zero;
			if (constantAcceleration <= FP._0 && relativeAcceleration <= FP._0 && proportionalAcceleration <= FP._0)
				return FPVector3.Zero;
			if (direction == default)
				return FPVector3.Zero;

			FP        baseSpeed     = new FPVector3(velocity.X  * axis.X, velocity.Y  * axis.Y, velocity.Z  * axis.Z).Magnitude;
			FPVector3 baseDirection = new FPVector3(direction.X * axis.X, direction.Y * axis.Y, direction.Z * axis.Z).Normalized;

			FP missingSpeed = FPMath.Max(FP._0, maxSpeed - baseSpeed);

			if (constantAcceleration     < FP._0) { constantAcceleration     = FP._0; }
			if (relativeAcceleration     < FP._0) { relativeAcceleration     = FP._0; }
			if (proportionalAcceleration < FP._0) { proportionalAcceleration = FP._0; }

			constantAcceleration     *= inputAcceleration;
			relativeAcceleration     *= inputAcceleration;
			proportionalAcceleration *= inputAcceleration;

			FP speedGain = (constantAcceleration + maxSpeed * relativeAcceleration + missingSpeed * proportionalAcceleration) * deltaTime;
			if (speedGain <= FP._0)
				return FPVector3.Zero;

			if (clampSpeed == true && speedGain > missingSpeed)
			{
				speedGain = missingSpeed;
			}

			return baseDirection.Normalized * speedGain;
		}

		public static FPVector3 GetAcceleration(FPVector3 velocity, FPVector3 direction, FPVector3 axis, FPVector3 normal, FP targetSpeed, bool clampSpeed, FP inputAcceleration, FP constantAcceleration, FP relativeAcceleration, FP proportionalAcceleration, FP deltaTime)
		{
			FP accelerationMultiplier = FP._1 - FPMath.Clamp01(FPVector3.Dot(direction.Normalized, normal));

			constantAcceleration     *= accelerationMultiplier;
			relativeAcceleration     *= accelerationMultiplier;
			proportionalAcceleration *= accelerationMultiplier;

			return GetAcceleration(velocity, direction, axis, targetSpeed, clampSpeed, inputAcceleration, constantAcceleration, relativeAcceleration, proportionalAcceleration, deltaTime);
		}

		public static FPVector3 GetFriction(FPVector3 velocity, FPVector3 direction, FPVector3 axis, FP maxSpeed, bool clampSpeed, FP constantFriction, FP relativeFriction, FP proportionalFriction, FP deltaTime)
		{
			if (constantFriction <= FP._0 && relativeFriction <= FP._0 && proportionalFriction <= FP._0)
				return FPVector3.Zero;
			if (direction == default)
				return FPVector3.Zero;

			FP        baseSpeed     = new FPVector3(velocity.X  * axis.X, velocity.Y  * axis.Y, velocity.Z  * axis.Z).Magnitude;
			FPVector3 baseDirection = new FPVector3(direction.X * axis.X, direction.Y * axis.Y, direction.Z * axis.Z).Normalized;

			if (constantFriction     < FP._0) { constantFriction     = FP._0; }
			if (relativeFriction     < FP._0) { relativeFriction     = FP._0; }
			if (proportionalFriction < FP._0) { proportionalFriction = FP._0; }

			FP speedDrop = (constantFriction + maxSpeed * relativeFriction + baseSpeed * proportionalFriction) * deltaTime;
			if (speedDrop <= FP._0)
				return FPVector3.Zero;

			if (clampSpeed == true && speedDrop > baseSpeed)
			{
				speedDrop = baseSpeed;
			}

			return -baseDirection * speedDrop;
		}

		public static FPVector3 GetFriction(FPVector3 velocity, FPVector3 direction, FPVector3 axis, FPVector3 normal, FP maxSpeed, bool clampSpeed, FP constantFriction, FP relativeFriction, FP proportionalFriction, FP deltaTime)
		{
			FP frictionMultiplier = FP._1 - FPMath.Clamp01(FPVector3.Dot(direction.Normalized, normal));

			constantFriction     *= frictionMultiplier;
			relativeFriction     *= frictionMultiplier;
			proportionalFriction *= frictionMultiplier;

			return GetFriction(velocity, direction, axis, maxSpeed, clampSpeed, constantFriction, relativeFriction, proportionalFriction, deltaTime);
		}

		public static FPVector3 CombineAccelerationAndFriction(FPVector3 velocity, FPVector3 acceleration, FPVector3 friction)
		{
			velocity.X = CombineAxis(velocity.X, acceleration.X, friction.X);
			velocity.Y = CombineAxis(velocity.Y, acceleration.Y, friction.Y);
			velocity.Z = CombineAxis(velocity.Z, acceleration.Z, friction.Z);

			return velocity;

			static FP CombineAxis(FP axisVelocity, FP axisAcceleration, FP axisFriction)
			{
				FP velocityDelta = axisAcceleration + axisFriction;

				if (FPMath.Abs(axisAcceleration) >= FPMath.Abs(axisFriction))
				{
					axisVelocity += velocityDelta;
				}
				else
				{
					if (axisVelocity > FP._0)
					{
						axisVelocity = FPMath.Max(FP._0, axisVelocity + velocityDelta);
					}
					else if (axisVelocity < FP._0)
					{
						axisVelocity = FPMath.Min(axisVelocity + velocityDelta, FP._0);
					}
				}

				return axisVelocity;
			}
		}

		public static bool Raycast(FPVector3 rayOrigin, FPVector3 rayDirection, FPVector3 planeNormal, FPVector3 planePoint, out FP distance)
		{
			FP a = FPVector3.Dot(rayDirection, planeNormal);
			if (a > -FP.EN4 && a < FP.EN4)
			{
				distance = FP._0;
				return false;
			}

			distance = (FPVector3.Dot(planeNormal, planePoint) - FPVector3.Dot(rayOrigin, planeNormal)) / a;
			return distance > FP.EN4;
		}
	}
}

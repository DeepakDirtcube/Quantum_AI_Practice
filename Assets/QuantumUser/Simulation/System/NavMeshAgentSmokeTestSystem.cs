namespace Quantum
{
    using Photon.Deterministic;
    using UnityEngine.Scripting;

    [Preserve]
    public unsafe class NavMeshAgentSmokeTestSystem : SystemMainThread
    {
        public override void OnInit(Frame frame)
        {
            var e = frame.Create();

            frame.Set(e, new Transform3D
            {
                Position = FPVector3.Zero,
                Rotation = FPQuaternion.Identity
            });

            // Create pathfinder (no config yet)
            var pf = NavMeshPathfinder.Create(frame, e, null);

            // If you made a NavMeshAgentConfig asset, apply it like this later:
            // pf.SetConfig(frame, e, MyAssets.NavAgentConfigAssetRef); // AssetRef<NavMeshAgentConfig> OK

            var nav = frame.Map.NavMeshes["Navmesh"]; // use your navmesh asset name if different
            pf.SetTarget(frame, new FPVector3(12, 0, 0), nav);

            frame.Set(e, pf);
            frame.Set(e, new NavMeshSteeringAgent());
        }

        public override void Update(Frame f)
        {
        }

    }
}

using Quantum;
using UnityEngine;

namespace SimpleFPS
{
	public class GameplayDataBaker : MapDataBakerCallback
	{
		public override void OnBeforeBake(QuantumMapData data)
		{
		}

		public override void OnBake(QuantumMapData data)
		{
#if UNITY_EDITOR
			var gameplayData = QuantumUnityDB.GetGlobalAssetEditorInstance<GameplayData>(data.GetAsset(true).UserAsset);
			var spawnPoints = GameObject.FindObjectsOfType<SpawnPoint>(true);

			gameplayData.SpawnPoints = new SpawnPointData[spawnPoints.Length];

			for (int i = 0; i < spawnPoints.Length; ++i)
			{
				SpawnPointData spawnPointData = new SpawnPointData();
				spawnPointData.Position = spawnPoints[i].transform.position.ToFPVector3();
				spawnPointData.Rotation = spawnPoints[i].transform.rotation.ToFPQuaternion();

				gameplayData.SpawnPoints[i] = spawnPointData;
			}

			UnityEditor.EditorUtility.SetDirty(gameplayData);
#endif
		}
	}
}

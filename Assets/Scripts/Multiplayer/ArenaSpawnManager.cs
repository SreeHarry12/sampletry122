using System.Collections.Generic;
using Mirror;
using UnityEngine;

namespace Rocket.Multiplayer
{
    public class ArenaSpawnManager : MonoBehaviour
    {
        public static ArenaSpawnManager Instance { get; private set; }

        [SerializeField] private List<Transform> spawnPoints = new List<Transform>();

        private void Awake()
        {
            Instance = this;
            CacheSpawnPoints();
        }

        private void CacheSpawnPoints()
        {
            spawnPoints.Clear();

            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                if (child.name.StartsWith("SpawnPoint_", System.StringComparison.OrdinalIgnoreCase))
                {
                    spawnPoints.Add(child);
                }
            }
        }

        public Transform GetSpawnPoint(int playerIndex)
        {
            if (spawnPoints.Count == 0)
            {
                return transform;
            }

            return spawnPoints[playerIndex % spawnPoints.Count];
        }
    }
}

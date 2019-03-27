using UnityEngine;

namespace Mirror
{
    public class NetworkRigidbodyManager : MonoBehaviour
    {
        internal static NetworkRigidbodyManager Instance { get; private set; }

        private bool SimulationIsDirty = false;

        void Start()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
            Physics.autoSimulation = false;
        }

        void Update()
        {
            float dt = Time.fixedDeltaTime;
            if (SimulationIsDirty)
            {
                Physics.Simulate(dt);
                SimulationIsDirty = false;
            }
        }

        internal void MarkSimulationDirty()
        {
            SimulationIsDirty = true;
        }
    }
}

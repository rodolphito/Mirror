using UnityEngine;
using UnityEngine.SceneManagement;

namespace Mirror
{
    public class NetworkRigidbodyManager : MonoBehaviour
    {
        internal static NetworkRigidbodyManager Instance { get; private set; }

        [SerializeField,Tooltip("You can set this programmatically to control it at runtime, but it must be set True at Start() time to initialize itself.")]
        private bool ClearTicksOnSceneChange = true;

        private bool SimulationIsDirty = false;

        private int TicksToSimulate = 0;

        void Start()
        {
            if (Instance == null)
            {
                Instance = this;
                if (ClearTicksOnSceneChange)
                {
                    SceneManager.activeSceneChanged += NewSceneChange;
                }
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
            Physics.autoSimulation = false;
        }

        private void NewSceneChange(Scene current, Scene next)
        {
            if (ClearTicksOnSceneChange)
            {
                TicksToSimulate = 0;
            }
        }

        void Update()
        {
            float dt = Time.fixedDeltaTime;
            if (SimulationIsDirty)
            {
                for (int i = 0; i < TicksToSimulate; i++)
                {
                    Physics.Simulate(dt);
                }
                TicksToSimulate = 0;
                SimulationIsDirty = false;
            }
        }

        internal void MarkSimulationDirty()
        {
            SimulationIsDirty = true;
        }

        internal void IncrementTick(float Time)
        {
            TicksToSimulate++;
            SimulationIsDirty = true;
        }
    }
}

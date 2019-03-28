using System.Collections.Generic;
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

        internal uint TickNumber = 0;
        private int TicksToSimulate = 0;

        private Dictionary<uint, List<NetworkRigidbody>> DirtyClientTicksToSimulate = new Dictionary<uint, List<NetworkRigidbody>>();

        private Dictionary<uint, List<NetworkRigidbody>> DirtyServerTicksToSimulate = new Dictionary<uint, List<NetworkRigidbody>>();

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
                //for (int i = 0; i < TicksToSimulate; i++)
                //{
                //    Physics.Simulate(dt);
                //}

                foreach (var item in DirtyClientTicksToSimulate)
                {
                    Physics.Simulate(dt);
                    foreach (var item2 in item.Value)
                    {
                        item2.SendInputs(item.Key);
                    }
                    TickNumber++;
                }
                DirtyClientTicksToSimulate.Clear();

                foreach (var item in DirtyServerTicksToSimulate)
                {
                    Physics.Simulate(dt);
                    foreach (var item2 in item.Value)
                    {
                        item2.SendServerState(item.Key);
                    }
                    TickNumber++;
                }
                DirtyServerTicksToSimulate.Clear();

                TicksToSimulate = 0;
                SimulationIsDirty = false;
            }
        }

        internal void MarkSimulationDirty()
        {
            SimulationIsDirty = true;
        }

        internal void IncrementClientTick(NetworkRigidbody DirtyClientRb, uint TickRequested)
        {
            if (DirtyClientTicksToSimulate.ContainsKey(TickRequested))
            {
                DirtyClientTicksToSimulate[TickRequested].Add(DirtyClientRb);
            }
            else
            {
                DirtyClientTicksToSimulate[TickRequested] = new List<NetworkRigidbody> { DirtyClientRb };
            }
            TicksToSimulate++;
            SimulationIsDirty = true;
        }

        internal void IncrementServerTick(NetworkRigidbody DirtyServerRb, uint TickRequested)
        {
            if (DirtyServerTicksToSimulate.ContainsKey(TickRequested))
            {
                DirtyServerTicksToSimulate[TickRequested].Add(DirtyServerRb);
            }
            else
            {
                DirtyServerTicksToSimulate[TickRequested] = new List<NetworkRigidbody> { DirtyServerRb };
            }
            TicksToSimulate++;
            SimulationIsDirty = true;
        }
    }
}

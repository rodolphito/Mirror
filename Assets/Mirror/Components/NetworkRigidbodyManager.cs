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

        private List<NetworkRigidbody> WaitingInputs = new List<NetworkRigidbody>();

        private List<NetworkRigidbody> RigidbodiesWithMessages = new List<NetworkRigidbody>();

        private List<NetworkRigidbody> ServerRigidbodiesWithMessages = new List<NetworkRigidbody>();

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
                if (WaitingInputs.Count > 0)
                {
                    foreach (var WaitingRb in WaitingInputs)
                    {
                        WaitingRb.AuthorityPreUpdate();
                    }
                    Physics.Simulate(dt);
                    foreach (var WaitingRb in WaitingInputs)
                    {
                        WaitingRb.AuthorityPostUpdate();
                    }
                    ++TickNumber;
                    WaitingInputs.Clear();
                }

                if (RigidbodiesWithMessages.Count > 0)
                {
                    uint RewindTick = 0;
                    foreach (var RbWithMessage in RigidbodiesWithMessages)
                    {
                        uint TempRewindTick = 0;
                        RbWithMessage.ClientPreUpdate(ref TempRewindTick);
                        RewindTick = (uint)Mathf.Min(RewindTick, TempRewindTick);
                    }
                    if (RewindTick != 0)
                    {
                        while (RewindTick < TickNumber)
                        {
                            var buffer_slot = RewindTick % 1024;
                            foreach (var RbWithMessage in RigidbodiesWithMessages)
                            {
                                RbWithMessage.ClientApplyRewindState(buffer_slot);
                            }
                            Physics.Simulate(dt);
                            ++RewindTick;
                        }
                    }
                    RigidbodiesWithMessages.Clear();
                }

                if (ServerRigidbodiesWithMessages.Count > 0)
                {
                    foreach (var ServerRb in ServerRigidbodiesWithMessages)
                    {
                        while (ServerRb.ServerInputMsgs.Count > 0)
                        {
                            ServerRb.ServerPreUpdate();
                        }
                    }
                    Physics.Simulate(dt);
                    foreach (var ServerRb in ServerRigidbodiesWithMessages)
                    {
                        ServerRb.ServerPostUpdate();
                    }
                    TickNumber++;
                    ServerRigidbodiesWithMessages.Clear();
                }
                SimulationIsDirty = false;
            }
        }

        internal void MarkSimulationDirty()
        {
            SimulationIsDirty = true;
        }

        internal void ClientHasInputs(NetworkRigidbody nrb)
        {
            WaitingInputs.Add(nrb);
            SimulationIsDirty = true;
        }

        internal void RigidbodyHasMessages(NetworkRigidbody networkRigidbody)
        {
            RigidbodiesWithMessages.Add(networkRigidbody);
            SimulationIsDirty = true;
        }

        internal void ServerRigidbodyHasMessages(NetworkRigidbody networkRigidbody)
        {
            ServerRigidbodiesWithMessages.Add(networkRigidbody);
            SimulationIsDirty = true;
        }
    }
}

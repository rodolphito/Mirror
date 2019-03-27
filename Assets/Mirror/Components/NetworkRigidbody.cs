using UnityEngine;
using System.Collections.Generic;
using Mirror;

public class NetworkRigidbody : NetworkBehaviour
{
    public struct Inputs
    {
        public bool up;
        public bool down;
        public bool left;
        public bool right;
        public bool jump;
        public bool TurnLeft;
        public bool TurnRight;
    }

    public struct InputMessage
    {
        public float delivery_time;
        public uint start_tick_number;
        public Inputs[] inputs;
    }

    public struct ClientState
    {
        public Vector3 position;
        public Quaternion rotation;
    }

    public struct StateMessage
    {
        public float delivery_time;
        public uint tick_number;
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 velocity;
        public Vector3 angular_velocity;
    }

    #region Shared Fields

    public float player_movement_impulse;
    public float player_jump_y_threshold;
    public GameObject smoothed_client_player;
    public GameObject server_display_player;

    private Rigidbody Rb;

    #endregion

    #region Client Fields

    public bool EnableClientCorrections = true;
    public bool EnableClientCorrectionSmoothing = true;
    public bool SendRedundantInputs = true;
    private float ClientTimer = 0;
    private uint ClientTickNumber = 0;
    private uint ClientLastReceivedStateTick = 0;
    private const int ClientBufferSize = 1024;
    private ClientState[] ClientStateBuffer = new ClientState[ClientBufferSize]; // client stores predicted moves here
    private Inputs[] ClientInputBuffer = new Inputs[ClientBufferSize]; // client stores predicted inputs here
    private Queue<StateMessage> ClientStateMessages = new Queue<StateMessage>();
    private Vector3 ClientPosError = Vector3.zero;
    private Quaternion ClientRotError = Quaternion.identity;

    #endregion

    #region Server Fields

    public uint ServerSnapshotRate;
    private uint ServerTickNumber = 0;
    private uint ServerTickAccumulator = 0;
    private Queue<InputMessage> ServerInputMsgs = new Queue<InputMessage>();

    #endregion

    #region Unity Callbacks

    private void Start()
    {
        Rb = GetComponent<Rigidbody>();
        Physics.autoSimulation = false;
        if (isServerOnly)
        {
            smoothed_client_player.SetActive(false);
        }

        if (isClientOnly)
        {
            server_display_player.SetActive(false);
        }
    }

    private void Update()
    {
        float dt = Time.fixedDeltaTime;

        if (isClient)
        {
            ClientUpdate(dt);
        }

        if (isServer)
        {
            ServerUpdate(dt);
        }
    }

    [Client]
    private void ClientUpdate(float dt)
    {
        float client_timer = this.ClientTimer;
        uint client_tick_number = this.ClientTickNumber;

        client_timer += Time.deltaTime;
        while (client_timer >= dt)
        {
            client_timer -= dt;

            if (isLocalPlayer)
            {
                uint buffer_slot = client_tick_number % ClientBufferSize;

                // sample and store inputs for this tick
                Inputs inputs = new Inputs
                {
                    up = Input.GetKey(KeyCode.W),
                    down = Input.GetKey(KeyCode.S),
                    left = Input.GetKey(KeyCode.A),
                    right = Input.GetKey(KeyCode.D),
                    jump = Input.GetKey(KeyCode.Space),
                    TurnLeft = Input.GetKey(KeyCode.Q),
                    TurnRight = Input.GetKey(KeyCode.E)
                };
                this.ClientInputBuffer[buffer_slot] = inputs;

                // store state for this tick, then use current state + input to step simulation
                this.ClientStoreCurrentStateAndStep(
                    ref this.ClientStateBuffer[buffer_slot],
                    inputs,
                    dt);

                // send input packet to server
                InputMessage input_msg;
                input_msg.delivery_time = Time.time;
                input_msg.start_tick_number = this.SendRedundantInputs ? this.ClientLastReceivedStateTick : client_tick_number;
                var InputBuffer = new List<Inputs>();

                for (uint tick = input_msg.start_tick_number; tick <= client_tick_number; ++tick)
                {
                    InputBuffer.Add(ClientInputBuffer[tick % ClientBufferSize]);
                }
                input_msg.inputs = InputBuffer.ToArray();
                Debug.Log("Inputs count: " + input_msg.inputs.Length);
                CmdSendInputMsg(input_msg);
            }

            ++client_tick_number;
        }

        if (this.ClientHasStateMessage())
        {
            StateMessage state_msg = this.ClientStateMessages.Dequeue();
            while (this.ClientHasStateMessage()) // make sure if there are any newer state messages available, we use those instead
            {
                state_msg = this.ClientStateMessages.Dequeue();
            }

            this.ClientLastReceivedStateTick = state_msg.tick_number;

            if (this.EnableClientCorrections)
            {
                uint buffer_slot = state_msg.tick_number % ClientBufferSize;
                Vector3 position_error = state_msg.position - this.ClientStateBuffer[buffer_slot].position;
                float rotation_error = 1.0f - Quaternion.Dot(state_msg.rotation, this.ClientStateBuffer[buffer_slot].rotation);

                if (position_error.sqrMagnitude > 0.0000001f ||
                    rotation_error > 0.00001f)
                {
                    // capture the current predicted pos for smoothing
                    Vector3 prev_pos = Rb.position + this.ClientPosError;
                    Quaternion prev_rot = Rb.rotation * this.ClientRotError;

                    // rewind & replay
                    Rb.position = state_msg.position;
                    Rb.rotation = state_msg.rotation;
                    Rb.velocity = state_msg.velocity;
                    Rb.angularVelocity = state_msg.angular_velocity;

                    uint rewind_tick_number = state_msg.tick_number;
                    while (rewind_tick_number < client_tick_number)
                    {
                        buffer_slot = rewind_tick_number % ClientBufferSize;
                        this.ClientStoreCurrentStateAndStep(
                            ref this.ClientStateBuffer[buffer_slot],
                            this.ClientInputBuffer[buffer_slot],
                            dt);

                        ++rewind_tick_number;
                    }

                    // if more than 2ms apart, just snap
                    if ((prev_pos - Rb.position).sqrMagnitude >= 4.0f)
                    {
                        this.ClientPosError = Vector3.zero;
                        this.ClientRotError = Quaternion.identity;
                    }
                    else
                    {
                        this.ClientPosError = prev_pos - Rb.position;
                        this.ClientRotError = Quaternion.Inverse(Rb.rotation) * prev_rot;
                    }
                }
            }
        }

        this.ClientTimer = client_timer;
        this.ClientTickNumber = client_tick_number;

        if (this.EnableClientCorrectionSmoothing)
        {
            this.ClientPosError *= 0.9f;
            this.ClientRotError = Quaternion.Slerp(this.ClientRotError, Quaternion.identity, 0.1f);
        }
        else
        {
            this.ClientPosError = Vector3.zero;
            this.ClientRotError = Quaternion.identity;
        }

        this.smoothed_client_player.transform.position = Rb.position + this.ClientPosError;
        this.smoothed_client_player.transform.rotation = Rb.rotation * this.ClientRotError;
    }

    [Server]
    private void ServerUpdate(float dt)
    {
        uint server_tick_number = this.ServerTickNumber;
        uint server_tick_accumulator = this.ServerTickAccumulator;

        while (this.ServerInputMsgs.Count > 0 && Time.time >= this.ServerInputMsgs.Peek().delivery_time)
        {
            InputMessage input_msg = this.ServerInputMsgs.Dequeue();

            // message contains an array of inputs, calculate what tick the final one is
            uint max_tick = input_msg.start_tick_number + (uint)input_msg.inputs.Length - 1;

            // if that tick is greater than or equal to the current tick we're on, then it
            // has inputs which are new
            if (max_tick >= server_tick_number)
            {
                // there may be some inputs in the array that we've already had,
                // so figure out where to start
                uint start_i = server_tick_number > input_msg.start_tick_number ? (server_tick_number - input_msg.start_tick_number) : 0;

                // run through all relevant inputs, and step player forward
                for (int i = (int)start_i; i < input_msg.inputs.Length; ++i)
                {
                    this.PrePhysicsStep(Rb, input_msg.inputs[i]);
                    Physics.Simulate(dt);

                    ++server_tick_number;
                    ++server_tick_accumulator;
                    if (server_tick_accumulator >= this.ServerSnapshotRate)
                    {
                        server_tick_accumulator = 0;

                        StateMessage state_msg;
                        state_msg.delivery_time = Time.time;
                        state_msg.tick_number = server_tick_number;
                        state_msg.position = Rb.position;
                        state_msg.rotation = Rb.rotation;
                        state_msg.velocity = Rb.velocity;
                        state_msg.angular_velocity = Rb.angularVelocity;
                        RpcSendClientState(state_msg);
                    }
                }

                this.server_display_player.transform.position = Rb.position;
                this.server_display_player.transform.rotation = Rb.rotation;
            }
        }

        this.ServerTickNumber = server_tick_number;
        this.ServerTickAccumulator = server_tick_accumulator;
    }

    // exploratory/unfinished
    public void AddNetworkedForce(Vector3 Force, ForceMode ForceMode)
    {
        Rb.AddForce(Force, ForceMode);
    }

    public void AddRelativeNetworkedForce(Vector3 Force, ForceMode Mode)
    {
        Rb.AddRelativeForce(Force, Mode);
    }

    public void AddNetworkedTorque(Vector3 Torque, ForceMode Mode)
    {
        Rb.AddTorque(Torque, Mode);
    }

    public void AddNetworkedRelativeTorque(Vector3 Torque, ForceMode Mode)
    {
        Rb.AddRelativeTorque(Torque, Mode);
    }

    [ClientRpc]
    public void RpcSendClientState(StateMessage state_msg)
    {
        ClientStateMessages.Enqueue(state_msg);
    }

    [Command]
    public void CmdSendInputMsg(InputMessage input_msg)
    {
        ServerInputMsgs.Enqueue(input_msg);
    }

    #endregion

    private void PrePhysicsStep(Rigidbody rigidbody, Inputs inputs)
    {
        if (inputs.up)
        {
            rigidbody.AddForce(transform.forward * this.player_movement_impulse, ForceMode.Impulse);
        }
        if (inputs.down)
        {
            rigidbody.AddForce(-transform.forward * player_movement_impulse, ForceMode.Impulse);
        }
        if (inputs.left)
        {
            rigidbody.AddForce(-transform.right * this.player_movement_impulse, ForceMode.Impulse);
        }
        if (inputs.right)
        {
            rigidbody.AddForce(transform.right * this.player_movement_impulse, ForceMode.Impulse);
        }
        if (rigidbody.transform.position.y <= this.player_jump_y_threshold && inputs.jump)
        {
            rigidbody.AddForce(transform.up * this.player_movement_impulse, ForceMode.Impulse);
        }

        if (inputs.TurnLeft)
        {
            rigidbody.AddTorque(-transform.up * 0.05f, ForceMode.Impulse);
        }

        if (inputs.TurnRight)
        {
            rigidbody.AddTorque(transform.up * 0.05f, ForceMode.Impulse);
        }
    }

    private bool ClientHasStateMessage()
    {
        return this.ClientStateMessages.Count > 0 && Time.time >= this.ClientStateMessages.Peek().delivery_time;
    }

    private void ClientStoreCurrentStateAndStep(ref ClientState current_state, Inputs inputs, float dt)
    {
        current_state.position = Rb.position;
        current_state.rotation = Rb.rotation;

        this.PrePhysicsStep(Rb, inputs);
        Physics.Simulate(dt);
    }
}

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

    public Transform local_player_camera_transform;
    public float player_movement_impulse;
    public float player_jump_y_threshold;
    private GameObject DisplayPlayer;
    public float latency = 0.1f;

    private Rigidbody Rb;

    #endregion

    #region Client Fields

    public bool client_enable_corrections = true;
    public bool client_correction_smoothing = true;
    public bool client_send_redundant_inputs = true;
    private float client_timer = 0;
    private uint client_tick_number = 0;
    private uint client_last_received_state_tick = 0;
    private const int c_client_buffer_size = 1024;
    private ClientState[] client_state_buffer = new ClientState[c_client_buffer_size]; // client stores predicted moves here
    private Inputs[] client_input_buffer = new Inputs[c_client_buffer_size]; // client stores predicted inputs here
    private Queue<StateMessage> client_state_msgs = new Queue<StateMessage>();
    private Vector3 client_pos_error = Vector3.zero;
    private Quaternion client_rot_error = Quaternion.identity;

    #endregion

    #region Server Fields

    public uint server_snapshot_rate;
    private uint server_tick_number = 0;
    private uint server_tick_accumulator = 0;
    private Queue<InputMessage> server_input_msgs = new Queue<InputMessage>();

    #endregion

    #region Unity Callbacks

    private void Start()
    {
        Rb = GetComponent<Rigidbody>();
        DisplayPlayer = gameObject;
    }

    private void Update()
    {
        // client update

        // enable client player, disable server player
        //this.server_player.SetActive(false);
        //this.client_player.SetActive(true);
        float dt = Time.fixedDeltaTime;

        if (isClient)
        {
            
            float client_timer = this.client_timer;
            uint client_tick_number = this.client_tick_number;

            client_timer += Time.deltaTime;
            while (client_timer >= dt)
            {
                client_timer -= dt;

                uint buffer_slot = client_tick_number % c_client_buffer_size;

                // sample and store inputs for this tick
                Inputs inputs;
                inputs.up = Input.GetKey(KeyCode.W);
                inputs.down = Input.GetKey(KeyCode.S);
                inputs.left = Input.GetKey(KeyCode.A);
                inputs.right = Input.GetKey(KeyCode.D);
                inputs.jump = Input.GetKey(KeyCode.Space);
                this.client_input_buffer[buffer_slot] = inputs;

                // store state for this tick, then use current state + input to step simulation
                this.ClientStoreCurrentStateAndStep(
                    ref this.client_state_buffer[buffer_slot],
                    Rb,
                    inputs,
                    dt);

                // send input packet to server
                InputMessage input_msg;
                input_msg.delivery_time = Time.time + this.latency;
                input_msg.start_tick_number = this.client_send_redundant_inputs ? this.client_last_received_state_tick : client_tick_number;

                var InputBuffer = new List<Inputs>();
                for (uint tick = input_msg.start_tick_number; tick <= client_tick_number; ++tick)
                {
                    InputBuffer.Add(client_input_buffer[tick % c_client_buffer_size]);
                }
                input_msg.inputs = InputBuffer.ToArray();

                //this.server_input_msgs.Enqueue(input_msg);
                CmdSendInputMsg(input_msg);

                ++client_tick_number;
            }

            if (this.ClientHasStateMessage())
            {
                StateMessage state_msg = this.client_state_msgs.Dequeue();
                while (this.ClientHasStateMessage()) // make sure if there are any newer state messages available, we use those instead
                {
                    state_msg = this.client_state_msgs.Dequeue();
                }

                this.client_last_received_state_tick = state_msg.tick_number;

                DisplayPlayer.transform.position = state_msg.position;
                DisplayPlayer.transform.rotation = state_msg.rotation;

                if (this.client_enable_corrections)
                {
                    uint buffer_slot = state_msg.tick_number % c_client_buffer_size;
                    Vector3 position_error = state_msg.position - this.client_state_buffer[buffer_slot].position;
                    float rotation_error = 1.0f - Quaternion.Dot(state_msg.rotation, this.client_state_buffer[buffer_slot].rotation);

                    if (position_error.sqrMagnitude > 0.0000001f ||
                        rotation_error > 0.00001f)
                    {
                        // capture the current predicted pos for smoothing
                        Vector3 prev_pos = Rb.position + this.client_pos_error;
                        Quaternion prev_rot = Rb.rotation * this.client_rot_error;

                        // rewind & replay
                        Rb.position = state_msg.position;
                        Rb.rotation = state_msg.rotation;
                        Rb.velocity = state_msg.velocity;
                        Rb.angularVelocity = state_msg.angular_velocity;

                        uint rewind_tick_number = state_msg.tick_number;
                        while (rewind_tick_number < client_tick_number)
                        {
                            buffer_slot = rewind_tick_number % c_client_buffer_size;
                            this.ClientStoreCurrentStateAndStep(
                                ref this.client_state_buffer[buffer_slot],
                                Rb,
                                this.client_input_buffer[buffer_slot],
                                dt);

                            ++rewind_tick_number;
                        }

                        // if more than 2ms apart, just snap
                        if ((prev_pos - Rb.position).sqrMagnitude >= 4.0f)
                        {
                            this.client_pos_error = Vector3.zero;
                            this.client_rot_error = Quaternion.identity;
                        }
                        else
                        {
                            this.client_pos_error = prev_pos - Rb.position;
                            this.client_rot_error = Quaternion.Inverse(Rb.rotation) * prev_rot;
                        }
                    }
                }
            }

            this.client_timer = client_timer;
            this.client_tick_number = client_tick_number;

            if (this.client_correction_smoothing)
            {
                this.client_pos_error *= 0.9f;
                this.client_rot_error = Quaternion.Slerp(this.client_rot_error, Quaternion.identity, 0.1f);
            }
            else
            {
                this.client_pos_error = Vector3.zero;
                this.client_rot_error = Quaternion.identity;
            }

            DisplayPlayer.transform.position = Rb.position + this.client_pos_error;
            DisplayPlayer.transform.rotation = Rb.rotation * this.client_rot_error;
        }

        // server update

        // enable server player, disable client player
        //this.client_player.SetActive(false);
        //this.server_player.SetActive(true);

        if (isServer)
        {
            uint server_tick_number = this.server_tick_number;
            uint server_tick_accumulator = this.server_tick_accumulator;

            while (this.server_input_msgs.Count > 0 && Time.time >= this.server_input_msgs.Peek().delivery_time)
            {
                InputMessage input_msg = this.server_input_msgs.Dequeue();

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
                        if (server_tick_accumulator >= this.server_snapshot_rate)
                        {
                            server_tick_accumulator = 0;

                            StateMessage state_msg;
                            state_msg.delivery_time = Time.time + this.latency;
                            state_msg.tick_number = server_tick_number;
                            state_msg.position = Rb.position;
                            state_msg.rotation = Rb.rotation;
                            state_msg.velocity = Rb.velocity;
                            state_msg.angular_velocity = Rb.angularVelocity;
                            RpcSendClientState(state_msg);
                        }
                    }

                    DisplayPlayer.transform.position = Rb.position;
                    DisplayPlayer.transform.rotation = Rb.rotation;
                }
            }

            this.server_tick_number = server_tick_number;
            this.server_tick_accumulator = server_tick_accumulator;
        }

        

        // finally, we're viewing the client, so enable the client player, disable server again
        //this.server_player.SetActive(false);
        //this.client_player.SetActive(true);
    }

    [ClientRpc]
    public void RpcSendClientState(StateMessage state_msg)
    {
        client_state_msgs.Enqueue(state_msg);
    }

    [Command]
    public void CmdSendInputMsg(InputMessage input_msg)
    {
        server_input_msgs.Enqueue(input_msg);
    }

    #endregion

    private void PrePhysicsStep(Rigidbody rigidbody, Inputs inputs)
    {
        if (this.local_player_camera_transform != null)
        {
            if (inputs.up)
            {
                rigidbody.AddForce(this.local_player_camera_transform.forward * this.player_movement_impulse, ForceMode.Impulse);
            }
            if (inputs.down)
            {
                rigidbody.AddForce(-this.local_player_camera_transform.forward * this.player_movement_impulse, ForceMode.Impulse);
            }
            if (inputs.left)
            {
                rigidbody.AddForce(-this.local_player_camera_transform.right * this.player_movement_impulse, ForceMode.Impulse);
            }
            if (inputs.right)
            {
                rigidbody.AddForce(this.local_player_camera_transform.right * this.player_movement_impulse, ForceMode.Impulse);
            }
            if (rigidbody.transform.position.y <= this.player_jump_y_threshold && inputs.jump)
            {
                rigidbody.AddForce(this.local_player_camera_transform.up * this.player_movement_impulse, ForceMode.Impulse);
            }
        }
    }

    private bool ClientHasStateMessage()
    {
        return this.client_state_msgs.Count > 0 && Time.time >= this.client_state_msgs.Peek().delivery_time;
    }

    private void ClientStoreCurrentStateAndStep(ref ClientState current_state, Rigidbody rigidbody, Inputs inputs, float dt)
    {
        current_state.position = rigidbody.position;
        current_state.rotation = rigidbody.rotation;

        this.PrePhysicsStep(rigidbody, inputs);
        Physics.Simulate(dt);
    }
}

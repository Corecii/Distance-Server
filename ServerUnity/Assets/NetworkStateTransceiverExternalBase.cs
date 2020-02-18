using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public abstract class NetworkStateTransceiverExternalBase
{
    public NetworkStateTransceiverInternal transceiver;
    public abstract void OnSerializeNetworkView(BitStream stream, NetworkMessageInfo info);

    public abstract void Awake();

    public abstract void Start();

    public abstract void OnEnable();

    public abstract void OnDisable();

    public abstract void OnDestroy();

    public abstract void Update();

    public abstract void FixedUpdate();

    public abstract void LateUpdate();
}

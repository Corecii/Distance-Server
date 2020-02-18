using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class NetworkStateTransceiverInternal : MonoBehaviour
{
    public string TransceiverId;

    NetworkStateTransceiverExternalBase external = null;
    public NetworkStateTransceiverExternalBase External
    {
        get
        {
            return external;
        }
        set
        {
            external = value;
            value.transceiver = this;
            External.Awake();
            if (this.enabled)
            {
                External.Start();
            }
            if (this.isActiveAndEnabled)
            {
                External.OnEnable();
            }
        }
    }

    // Token: 0x06005141 RID: 20801 RVA: 0x001712F0 File Offset: 0x0016F4F0
    public void OnSerializeNetworkView(BitStream stream, NetworkMessageInfo info)
    {
        External?.OnSerializeNetworkView(stream, info);
    }

    public void Start()
    {
        External?.Start();
    }

    public void Awake()
    {
        External?.Awake();
    }

    public void OnEnable()
    {
        External?.OnEnable();
    }

    public void OnDisable()
    {
        External?.OnDisable();
    }

    public void OnDestroy()
    {
        External?.OnDestroy();
    }

    public void Update()
    {
        External?.Update();
    }

    public void FixedUpdate()
    {
        External?.FixedUpdate();
    }

    public void LateUpdate()
    {
        External?.LateUpdate();
    }
}
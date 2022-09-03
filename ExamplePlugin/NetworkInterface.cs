using System;
using System.Collections.Generic;
using System.Text;

using R2API.Networking.Interfaces;
using UnityEngine.Networking;

namespace AddCharacterSkills
{
    public class NetworkInterface : INetMessage
    {
        NetworkInstanceId netId;

        public NetworkInterface()
        {

        }

        public NetworkInterface(NetworkInstanceId netid)
        {
            this.netId = netid;
        }

        public void Deserialize(NetworkReader reader)
        {
            throw new NotImplementedException();
        }

        public void OnReceived()
        {
            if (NetworkServer.active)
            {
                return;
            }
        }

        public void Serialize(NetworkWriter writer)
        {
            throw new NotImplementedException();
        }
    }
}

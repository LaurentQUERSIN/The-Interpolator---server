using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using Stormancer;
using Stormancer.Plugins;
using Stormancer.Core;
using Stormancer.Server;
using Stormancer.Server.Components;
using Stormancer.Diagnostics;

namespace Stormancer
{
    public struct ReplicatorDTO
    {
        public uint Id;
        public int PrefabId;
        public long ClientId;
    }

    public class ReplicatorBehaviour
    {

        private ISceneHost _scene;
        private IEnvironment _env;
        private ILogger _log;

        private uint Ids = 0;

        public void Init(ISceneHost scene)
        {
            _scene = scene;
            _env = _scene.GetComponent<IEnvironment>();
            _log = _scene.GetComponent<ILogger>();
            _scene.AddProcedure("RegisterObject", OnRegisterObject);
            _scene.AddRoute("RemoveObject", OnRemoveObject);
            _scene.AddRoute("UpdateSynchedObject", OnUpdateObject);
            _scene.Connected.Add(OnClientConnected);
            _scene.Disconnected.Add(OnClientDisconnected);
        }

        public Task OnClientConnected(IScenePeerClient client)
        {
            _log.Debug("replicator", "client connected, sending object request to every connected peers");
            foreach (IScenePeerClient clt in _scene.RemotePeers)
            {
                if (client.Id != clt.Id)
                {
                    _log.Debug("replicator", "sending request to client " + clt.Id);
                    clt.RpcTask<long, List<ReplicatorDTO>>("RequestObjects", client.Id).ContinueWith(task =>
                    {
                        if (task.IsCanceled == true)
                        {
                            _log.Debug("replicator", "eeror: task cancelled");
                        }
                        if (task.IsFaulted == false)
                        {
                            var clientDtos = task.Result;
                            _log.Debug("replicator", "Object request received: " + clientDtos.Count + " from user " + clt.Id);
                            foreach (ReplicatorDTO dto in clientDtos)
                            {
                                client.Send<ReplicatorDTO>("CreateObject", dto);
                            }
                        }
                        else
                        {
                            _log.Debug("replicator", "object request failed: " + task.Exception.InnerException.Message);
                        }
                    });
                }
            }

            _log.Debug("replicator", "player connected");
            return Task.FromResult(true);
        }

        public Task OnClientDisconnected(DisconnectedArgs args)
        {
            _log.Debug("replicator", "player disconnected");
            foreach(IScenePeerClient client in _scene.RemotePeers)
            {
                client.Send<long>("PlayerDisconnected", args.Peer.Id, PacketPriority.MEDIUM_PRIORITY, PacketReliability.RELIABLE);
            }
            return Task.FromResult(true);
        }

        public Task OnRegisterObject(RequestContext<IScenePeerClient> ctx)
        {
            _log.Debug("replicator", "registering object");
            var dto = ctx.ReadObject<ReplicatorDTO>();
            var obj = new ReplicatorObject();

            obj.Client = ctx.RemotePeer;
            obj.PrefabId = dto.PrefabId;
            obj.Id = Ids++;

            dto.Id = obj.Id;

            ctx.SendValue<ReplicatorDTO>(dto);

            foreach(IScenePeerClient client in _scene.RemotePeers)
            {
                if (client.Id != ctx.RemotePeer.Id)
                {
                    client.Send<ReplicatorDTO>("CreateObject", dto);
                }
            }
            return Task.FromResult(true);
        }

        public void OnRemoveObject(Packet<IScenePeerClient> packet)
        {
            _log.Debug("replicator", "removing object");
            var dto = packet.ReadObject<ReplicatorDTO>();

            _scene.Broadcast<ReplicatorDTO>("DestroyObject", dto);
        }

        public void OnUpdateObject(Packet<IScenePeerClient> packet)
        {
            PacketReliability reliability;
            using (var reader = new BinaryReader(packet.Stream, Encoding.UTF8))
            {
                var temp = reader.ReadByte();
                reliability = (PacketReliability)temp;
                _scene.Broadcast("UpdateObject", s => { packet.Stream.CopyTo(s); }, PacketPriority.MEDIUM_PRIORITY, reliability);
            }
        }
    }
}

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    }

    public class ReplicatorBehaviour
    {
        private ISceneHost _scene;
        private IEnvironment _env;
        private ILogger _log;

        private uint Ids = 0;
        private ConcurrentDictionary<long, ReplicatorObject> Objects = new ConcurrentDictionary<long, ReplicatorObject>();

        public void Init(ISceneHost scene)
        {
            _scene = scene;
            _env = _scene.GetComponent<IEnvironment>();
            _log = _scene.GetComponent<ILogger>();
            _scene.AddProcedure("RegisterObject", OnRegisterObject);
            _scene.AddRoute("RemoveObject", OnRemoveObject);
            _scene.AddRoute("update_synchedObject", OnUpdateObject);
            _scene.Connected.Add(OnClientConnected);
            _scene.Disconnected.Add(OnClientDisconnected);
        }

        public Task OnClientConnected(IScenePeerClient client)
        {
            _log.Debug("replicator", "player connected");
            ReplicatorDTO dto = new ReplicatorDTO();
            foreach(ReplicatorObject obj in Objects.Values)
            {
                dto.Id = obj.Id;
                dto.PrefabId = obj.PrefabId;
                client.Send<ReplicatorDTO>("CreateObject", dto);
            }
            return Task.FromResult(true);
        }

        public Task OnClientDisconnected(DisconnectedArgs args)
        {
            _log.Debug("replicator", "player disconnected");
            var dto = new ReplicatorDTO();
            ReplicatorObject trash;
            foreach(ReplicatorObject obj in Objects.Values)
            {
                if (args.Peer.Id == obj.Client.Id)
                {
                    dto.Id = obj.Id;
                    _scene.Broadcast<ReplicatorDTO>("DestroyObject", dto);
                    Objects.TryRemove(obj.Id, out trash);
                }
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

            Objects.TryAdd(obj.Id, obj);

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
            ReplicatorObject trash;

            _scene.Broadcast<ReplicatorDTO>("DestroyObject", dto);

            Objects.TryRemove(dto.Id, out trash);
        }

        public void OnUpdateObject(Packet<IScenePeerClient> packet)
        {
            _scene.Broadcast("UpdateObject", packet.Stream, PacketPriority.MEDIUM_PRIORITY, PacketReliability.UNRELIABLE);
        }
        
    }
}

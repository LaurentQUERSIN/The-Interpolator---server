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

        private uint Ids = 0;
        private ConcurrentDictionary<long, ReplicatorObject> Objects = new ConcurrentDictionary<long, ReplicatorObject>();

        public void Init(ISceneHost scene)
        {
            _scene = scene;
            _env = _scene.GetComponent<IEnvironment>();
            _scene.AddProcedure("RegisterObject", OnRegisterObject);
            _scene.AddRoute("RemoveObject", OnRemoveObject);
            _scene.AddRoute("update_synchedObject", OnUpdateObject);
        }

        public Task OnRegisterObject(RequestContext<IScenePeerClient> ctx)
        {
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

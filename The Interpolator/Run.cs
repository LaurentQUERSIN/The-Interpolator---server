using Stormancer;
using Stormancer.Core;
using Stormancer.Diagnostics;
using Stormancer.Server.Components;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.IO;
using System;

namespace Interpolator
{
    class InterpolatorScene
    {
        private ISceneHost _scene;
        private ILogger _log;
        private IEnvironment _env;

        private string version = "a0.1";
        private bool _isRunning = false;

        private ConcurrentDictionary<long, Player> _players = new ConcurrentDictionary<long, Player>();

        public void Run(IAppBuilder builder)
        {
            builder.SceneTemplate("interpolator_scene", InterpolatorSceneBuilder);
        }

        private void InterpolatorSceneBuilder(ISceneHost scene)
        {
            _scene = scene;
            _log = _scene.GetComponent<ILogger>();
            _env = _scene.GetComponent<IEnvironment>();
            _scene.Connecting.Add(OnConnecting);
            _scene.Connected.Add(OnConnected);
            _scene.Disconnected.Add(OnDisconnected);
            _scene.Starting.Add(OnStarting);
            _scene.Shuttingdown.Add(OnSD);
            _scene.AddRoute("update_position", OnUpdatePosition);
            _scene.AddRoute("chat", OnChat);
        }

        private Task OnConnecting(IScenePeerClient client)
        {
            ConnectionDTO cdto = client.GetUserData<ConnectionDTO>();
            _log.Debug("interpolator", "new client connecting :: name = " + cdto.name + " :: " + cdto.version);
            if (cdto.version != version)
                throw new ClientException("Wrong version");
            if (_players.Count > 15)
                throw new ClientException("server full");
            foreach(Player p in _players.Values)
            {
                if (cdto.name == p.name)
                    throw new ClientException("nickname already in use");
            }
            return Task.FromResult(true);
        }

        private async Task OnConnected(IScenePeerClient client)
        {
            ConnectionDTO cdto = client.GetUserData<ConnectionDTO>();

            client.Send("get_id", w =>
            {
                var writer = new BinaryWriter(w, System.Text.Encoding.UTF8, false);
                writer.Write(client.Id);
            }, PacketPriority.HIGH_PRIORITY, PacketReliability.RELIABLE);
            await Task.Delay(100);
            client.Send("create_player",w =>
            {
                var writer = new BinaryWriter(w, System.Text.Encoding.UTF8, false);
                foreach (Player p in _players.Values)
                {
                    writer.Write(p.Id);
                }
            }, PacketPriority.MEDIUM_PRIORITY, PacketReliability.RELIABLE);
            _scene.Broadcast("create_player", w =>
            {
                var writer = new BinaryWriter(w, System.Text.Encoding.UTF8, false);
                writer.Write(client.Id);
            });
            _scene.Broadcast("chat", cdto.name + " has connected.");
            _players.TryAdd(client.Id, new Player(cdto.name, client.Id));
        }

        private Task OnDisconnected(DisconnectedArgs args)
        {
            _log.Debug("interpolator", args.Peer.GetUserData<ConnectionDTO>().name + "has disconnected. reason: " + args.Reason);
            Player p;
            _players.TryRemove(args.Peer.Id, out p);
            _scene.Broadcast("chat", p.name + " has disconnected. (" + args.Reason + ")");
            _scene.Broadcast("remove_player", w =>
            {
                var writer = new BinaryWriter(w, System.Text.Encoding.UTF8, false);
                writer.Write(args.Peer.Id);
            });
            return Task.FromResult(true);
        }

        private void OnChat(Packet<IScenePeerClient> packet)
        {
            string message = packet.ReadObject<string>();
            Player p;
            if (_players.TryGetValue(packet.Connection.Id, out p))
            {
                message = p.name + " :" + message;
                _scene.Broadcast("chat", message);
            }
        }

        private void OnUpdatePosition(Packet<IScenePeerClient> packet)
        {
            var reader = new BinaryReader(packet.Stream);

            var id = reader.ReadInt64();
            var x = reader.ReadSingle();
            var y = reader.ReadSingle();
            var z = reader.ReadSingle();

            var vx = reader.ReadSingle();
            var vy = reader.ReadSingle();
            var vz = reader.ReadSingle();

            var rx = reader.ReadSingle();
            var ry = reader.ReadSingle();
            var rz = reader.ReadSingle();

            Player p;
            if (_players.TryGetValue(id, out p))
            {
                p.UpdatePosition(x, y, z, vx, vy, vz, rx, ry, rz);
            }
        }

        private Task _gameLoop;

        private Task OnStarting(dynamic args)
        {
            _log.Debug("Interpolator", "scene succefully loaded, starting game loop...");
            _gameLoop = StartGameLoop();
            return Task.FromResult(true);
        }

        private async Task OnSD(ShutdownArgs args)
        {
            _log.Debug("Interpolator", "the scene shuts down: reason: " + args.Reason);
            _isRunning = false;
            try
            {
                await _gameLoop;
            }
            catch (Exception e)
            {
                _log.Log(LogLevel.Error, "runtimeError", "an error occurred in the game loop", e);
            }
        }

        private async Task StartGameLoop()
        {
            _isRunning = true;
            while (_isRunning == true)
            {
                _scene.Broadcast("update_position", w =>
                {
                    var writer = new BinaryWriter(w, System.Text.Encoding.UTF8, false);

                    foreach(Player p in _players.Values)
                    {
                        writer.Write(p.Id);
                        writer.Write(p.x);
                        writer.Write(p.y);
                        writer.Write(p.z);

                        writer.Write(p.vx);
                        writer.Write(p.vy);
                        writer.Write(p.vz);

                        writer.Write(p.rx);
                        writer.Write(p.ry);
                        writer.Write(p.rz);
                    }

                }, PacketPriority.MEDIUM_PRIORITY, PacketReliability.UNRELIABLE_SEQUENCED);
                await Task.Delay(100);
            }
        }
    }
}

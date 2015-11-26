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
    public class SceneRun
    {
        public void Run(IAppBuilder builder)
        {
            builder.SceneTemplate("interpolator_scene", InterpolatorSceneBuilder);
        }

        public void InterpolatorSceneBuilder(ISceneHost scene)
        {
            InterpolatorScene newScene = new InterpolatorScene(scene);
        }
    }


    public class InterpolatorScene
    {
        private ISceneHost _scene;
        private ILogger _log;
        private IEnvironment _env;

        private string version = "a0.1";
        private bool _isRunning = false;

        private ConcurrentDictionary<long, Player> _players = new ConcurrentDictionary<long, Player>();

        private ReplicatorBehaviour replicator = new ReplicatorBehaviour();

        public InterpolatorScene(ISceneHost scene)
        {
            _scene = scene;
            _log = _scene.GetComponent<ILogger>();
            _env = _scene.GetComponent<IEnvironment>();
            ChatServerExtensions.AddChat(_scene);
            replicator.Init(_scene);
        }
    }
}

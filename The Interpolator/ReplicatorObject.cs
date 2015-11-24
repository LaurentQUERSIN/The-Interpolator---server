using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stormancer
{
    public class ReplicatorObject
    {
        public uint Id;
        public int PrefabId;
        public IScenePeerClient Client;
    }
}

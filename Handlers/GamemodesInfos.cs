using Impostor.Api.Net;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Numerics;

namespace Gamemodes
{
    public class InfectionInfos
    {
        public readonly List<IClientPlayer> impostors;
        public InfectionInfos(List<IClientPlayer> impostors)
        {
            this.impostors = impostors;
        }
    }

    public class HNSInfo
    {
        public readonly List<IClientPlayer> impostors;
        public HNSInfo(List<IClientPlayer> impostors)
        {
            this.impostors = impostors;
        }
    }

    public class FTAGInfo
    {
        public readonly List<IClientPlayer> impostors;
        public readonly ConcurrentDictionary<IClientPlayer, Vector2> frozens;
        public FTAGInfo(List<IClientPlayer> impostors, ConcurrentDictionary<IClientPlayer, Vector2> frozens)
        {
            this.impostors = impostors;
            this.frozens = frozens;
        }
    }
}

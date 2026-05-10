using Content.Shared.Sich.Sponsors;
using Robust.Shared.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Client.Sich.Sponsors;
public sealed partial class SponsorSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
    }
}

public interface IClientSponsorManager
{
    void Initialize();
    bool HasTag(string tag);
}

public sealed class ClientSponsorManager : IClientSponsorManager
{
    [Dependency] private readonly INetManager _net = default!;

    private readonly HashSet<string> _tags = new();

    public void Initialize()
    {
        _net.RegisterNetMessage<MsgSponsorInfo>(HandleSponsorInfo);
    }

    private void HandleSponsorInfo(MsgSponsorInfo msg)
    {
        _tags.Clear();
        foreach (var tag in msg.Tags)
        {
            _tags.Add(tag);
        }
    }

    public bool HasTag(string tag)
    {
        return _tags.Contains(tag);
    }
}

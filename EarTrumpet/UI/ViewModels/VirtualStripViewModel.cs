using EarTrumpet.DataModel.VirtualMixer;
using System.Collections.Generic;

namespace EarTrumpet.UI.ViewModels;

public class VirtualStripViewModel : AudioSessionViewModel
{
    private readonly IVirtualStrip _strip;

    public VirtualStripViewModel(IVirtualStrip strip) : base(strip)
    {
        _strip = strip;
    }

    public string DisplayName => _strip.DisplayName;
    public bool IsVirtual => _strip.IsVirtual;
    public IReadOnlyList<IVirtualRoute> Routes => _strip.Routes;
}

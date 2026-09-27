using EarTrumpet.DataModel.VirtualMixer;
using System.Collections.Generic;

namespace EarTrumpet.UI.ViewModels;

public class EarTrumpetVirtualMixerSettingsPageViewModel : SettingsPageViewModel
{
    public EarTrumpetVirtualMixerSettingsPageViewModel(IVirtualMixerBackend backend) : base(null)
    {
        Title = Properties.Resources.VirtualMixerSettingsPageText;
        Glyph = "\xE8D6";
        Buses = backend.Buses;
    }

    // Empty when the mixer isn't running.
    public IReadOnlyList<IVirtualBus> Buses { get; }
    public bool IsConnected => Buses.Count > 0;
}

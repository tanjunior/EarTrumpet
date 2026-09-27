using System.ComponentModel;

namespace EarTrumpet.DataModel.VirtualMixer;

// Whether a strip is sent to one output bus (e.g. "A1" = speakers, "B1" = a virtual recording device).
public interface IVirtualRoute : INotifyPropertyChanged
{
    string Name { get; }
    bool IsEnabled { get; set; }
}

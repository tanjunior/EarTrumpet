using System.ComponentModel;

namespace EarTrumpet.DataModel.VirtualMixer;

// Whether a strip is sent to one output bus.
public interface IVirtualRoute : INotifyPropertyChanged
{
    IVirtualBus Bus { get; }
    bool IsEnabled { get; set; }
}

using System.ComponentModel;

namespace EarTrumpet.DataModel.VirtualMixer;

// An output bus of a virtual mixer, e.g. "A1" (a physical output) or "B1" (a virtual recording device).
public interface IVirtualBus : INotifyPropertyChanged
{
    // Fixed identifier, e.g. "A1".
    string Name { get; }

    // User-chosen name stored in the mixer; empty when unset.
    string Label { get; set; }

    // Label, or Name when no label is set.
    string DisplayName { get; }

    // Where the bus goes, e.g. "A1: Speakers (Realtek(R) Audio)".
    string Description { get; }
}

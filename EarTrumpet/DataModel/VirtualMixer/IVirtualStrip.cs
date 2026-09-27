using EarTrumpet.DataModel.Audio;
using System.Collections.Generic;

namespace EarTrumpet.DataModel.VirtualMixer;

// An input channel of a virtual mixer. Volume/IsMuted/PeakValue follow the same conventions as
// Windows audio streams, so the existing slider and meter visuals can bind to it unchanged.
public interface IVirtualStrip : IStreamWithVolumeControl
{
    string DisplayName { get; }

    // True for virtual inputs (e.g. "Voicemeeter VAIO"), false for hardware inputs.
    bool IsVirtual { get; }

    // The Windows playback device that feeds this strip (e.g. "Voicemeeter Input"), or null.
    string WindowsDeviceName { get; }

    // Sets the strip gain in dB, independent of the app's linear/logarithmic setting.
    void SetGain(float gainDb);

    // One per output bus, in the mixer's order.
    IReadOnlyList<IVirtualRoute> Routes { get; }
}

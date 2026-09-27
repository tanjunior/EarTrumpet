using EarTrumpet.DataModel.Audio;

namespace EarTrumpet.DataModel.VirtualMixer;

// An input channel of a virtual mixer. Volume/IsMuted/PeakValue follow the same conventions as
// Windows audio streams, so the existing slider and meter visuals can bind to it unchanged.
public interface IVirtualStrip : IStreamWithVolumeControl
{
    string DisplayName { get; }

    // True for virtual inputs (e.g. "Voicemeeter VAIO"), false for hardware inputs.
    bool IsVirtual { get; }
}

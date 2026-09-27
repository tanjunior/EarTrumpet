using EarTrumpet.DataModel.VirtualMixer;
using System;
using System.Collections.ObjectModel;
using System.Windows.Threading;

namespace EarTrumpet.UI.ViewModels;

// Polls the virtual mixer only while the flyout is visible, so it costs nothing when hidden.
public class VirtualMixerViewModel : BindableBase
{
    private readonly IVirtualMixerBackend _backend;
    private readonly DispatcherTimer _pollTimer;
    private bool _isVisible;

    public VirtualMixerViewModel(IVirtualMixerBackend backend)
    {
        _backend = backend;
        _backend.StripsChanged += OnBackendStripsChanged;

        _pollTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1000 / 30) };
        _pollTimer.Tick += (_, __) => Poll();
    }

    public event EventHandler StripsChanged;

    public ObservableCollection<VirtualStripViewModel> Strips { get; } = [];
    public string DisplayName => _backend.DisplayName;
    public bool HasStrips => Strips.Count > 0;

    public bool IsVisible
    {
        get => _isVisible;
        set
        {
            if (_isVisible != value)
            {
                _isVisible = value;
                _pollTimer.IsEnabled = value;
                if (value)
                {
                    // Poll synchronously so strips exist before the flyout measures itself.
                    Poll();
                }
            }
        }
    }

    private void Poll()
    {
        _backend.Update(includeLevels: true);
        foreach (var strip in Strips)
        {
            strip.UpdatePeakValueForeground();
        }
    }

    private void OnBackendStripsChanged(object sender, EventArgs e)
    {
        Strips.Clear();
        foreach (var strip in _backend.Strips)
        {
            Strips.Add(new VirtualStripViewModel(strip));
        }
        RaisePropertyChanged(nameof(HasStrips));
        RaisePropertyChanged(nameof(DisplayName));
        StripsChanged?.Invoke(this, EventArgs.Empty);
    }
}

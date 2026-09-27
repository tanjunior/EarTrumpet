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
    private bool _isFlyoutVisible;
    private bool _isFullWindowVisible;

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

    public bool IsFlyoutVisible
    {
        get => _isFlyoutVisible;
        set
        {
            _isFlyoutVisible = value;
            UpdatePolling();
        }
    }

    public bool IsFullWindowVisible
    {
        get => _isFullWindowVisible;
        set
        {
            _isFullWindowVisible = value;
            UpdatePolling();
        }
    }

    private void UpdatePolling()
    {
        var shouldPoll = _isFlyoutVisible || _isFullWindowVisible;
        if (shouldPoll && !_pollTimer.IsEnabled)
        {
            // Poll synchronously so strips exist before the window measures itself.
            Poll();
        }
        _pollTimer.IsEnabled = shouldPoll;
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

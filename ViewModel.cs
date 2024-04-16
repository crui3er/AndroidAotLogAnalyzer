using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AndroidAotLogAnalyzer;

public sealed class ViewModel : INotifyPropertyChanged
{
    private AotLogData _data = new(false, [], new AotLogStat(0, 0, 0));
    public event PropertyChangedEventHandler? PropertyChanged;

    public AotLogData Data
    {
        get => _data;
        set => SetField(ref _data, value);
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
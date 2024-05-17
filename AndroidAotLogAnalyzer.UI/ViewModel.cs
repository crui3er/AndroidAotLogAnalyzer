using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AndroidAotLogAnalyzer.UI;

public sealed class ViewModel : INotifyPropertyChanged
{
    private AotLogDataOverview _overview = new (AotLogData.Empty(true), AotLogData.Empty(false), 0);
    private bool _isAotFoundViewSelected = true;
    private string _fileName = "";
    public event PropertyChangedEventHandler? PropertyChanged;

    public AotLogData Data
        => IsAotFoundViewSelected ? Overview.Found : Overview.NotFound;

    public AotLogDataOverview Overview
    {
        get => _overview;
        set {
            if (SetField(ref _overview, value))
                OnPropertyChanged(nameof(Data));
        }
    }

    public bool IsAotFoundViewSelected
    {
        get => _isAotFoundViewSelected;
        set {
            if (SetField(ref _isAotFoundViewSelected, value))
                OnPropertyChanged(nameof(Data));
        }
    }

    public string FileName
    {
        get => _fileName;
        set => SetField(ref _fileName, value);
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
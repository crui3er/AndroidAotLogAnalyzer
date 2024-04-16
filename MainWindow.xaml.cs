using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace AndroidAotLogAnalyzer;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void OnOpenFileClick(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            DefaultExt = ".txt", // Default file extension
            Filter = "Text documents (.txt)|*.txt" // Filter files by extension
        };

        // Show open file dialog box
        bool? result = dialog.ShowDialog();

// Process open file dialog box results
        if (result != true)
            return;

        using var stream = dialog.OpenFile();
        var data = new LogFileReader().Process(stream, !RadioButtonAotNotFound.IsChecked.GetValueOrDefault());
        this.ViewModel.Data = data;
    }
}

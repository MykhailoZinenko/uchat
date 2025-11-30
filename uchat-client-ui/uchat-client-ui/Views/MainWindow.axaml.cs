using Avalonia.Controls;
using uchat_client.ViewModels;

namespace uchat_client;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainWindowViewModel();
    }
}
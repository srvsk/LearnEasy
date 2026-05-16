using System.Windows;
using LearnEasy.Wpf.ViewModels;

namespace LearnEasy.Wpf.Views;

public partial class MainWindow : Window
{
    public MainWindow(ShellViewModel shell)
    {
        InitializeComponent();
        DataContext = shell;
    }
}

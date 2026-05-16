using LearnEasy.Wpf.Mvvm;

namespace LearnEasy.Wpf.ViewModels;

/// <summary>Hosts whichever screen is active; bound by the main window.</summary>
public sealed class ShellViewModel : ObservableObject
{
    private object? _currentViewModel;

    public object? CurrentViewModel
    {
        get => _currentViewModel;
        set => SetProperty(ref _currentViewModel, value);
    }
}

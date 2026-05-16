namespace LearnEasy.Wpf.Navigation;

/// <summary>Swaps the shell's active view model.</summary>
public interface INavigationService
{
    void NavigateToStart();
    void NavigateToLesson(int learnerId);
}

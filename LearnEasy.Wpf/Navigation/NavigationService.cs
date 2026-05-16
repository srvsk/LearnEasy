using LearnEasy.Wpf.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace LearnEasy.Wpf.Navigation;

/// <summary>
/// Resolves view models from DI and pushes them into the shell. Kept tiny —
/// this app only has two screens (pick learner → lesson).
/// </summary>
public sealed class NavigationService(ShellViewModel shell, IServiceProvider provider) : INavigationService
{
    public void NavigateToStart()
        => shell.CurrentViewModel = provider.GetRequiredService<StartViewModel>();

    public void NavigateToLesson(int learnerId)
    {
        var lesson = provider.GetRequiredService<LessonViewModel>();
        shell.CurrentViewModel = lesson;
        // Fire-and-forget init: the view shows a "getting ready" state until
        // the first word is spoken.
        _ = lesson.InitializeAsync(learnerId);
    }
}

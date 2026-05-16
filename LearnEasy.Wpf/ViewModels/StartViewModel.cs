using System.Collections.ObjectModel;
using System.Windows.Input;
using LearnEasy.Core.Abstractions;
using LearnEasy.Core.Models;
using LearnEasy.Wpf.Mvvm;
using LearnEasy.Wpf.Navigation;
using Microsoft.Extensions.DependencyInjection;

namespace LearnEasy.Wpf.ViewModels;

/// <summary>
/// "Who's learning today?" — pick an existing learner or create a new one,
/// then start the lesson.
/// </summary>
public sealed class StartViewModel : ObservableObject
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly INavigationService _nav;

    public StartViewModel(IServiceScopeFactory scopeFactory, INavigationService nav)
    {
        _scopeFactory = scopeFactory;
        _nav = nav;

        StartCommand = new AsyncRelayCommand(StartAsync, () => SelectedLearner is not null);
        CreateCommand = new AsyncRelayCommand(CreateAsync,
            () => !string.IsNullOrWhiteSpace(NewLearnerName));

        _ = LoadAsync();
    }

    /// <summary>Avatar keys map to emoji/icons in the theme.</summary>
    public IReadOnlyList<string> Avatars { get; } = ["fox", "robot", "panda", "rocket", "owl"];

    public ObservableCollection<LearnerProfile> Learners { get; } = [];

    private LearnerProfile? _selectedLearner;
    public LearnerProfile? SelectedLearner
    {
        get => _selectedLearner;
        set => SetProperty(ref _selectedLearner, value);
    }

    private string _newLearnerName = string.Empty;
    public string NewLearnerName
    {
        get => _newLearnerName;
        set => SetProperty(ref _newLearnerName, value);
    }

    private string _selectedAvatar = "fox";
    public string SelectedAvatar
    {
        get => _selectedAvatar;
        set => SetProperty(ref _selectedAvatar, value);
    }

    public ICommand StartCommand { get; }
    public ICommand CreateCommand { get; }

    private async Task LoadAsync()
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var progress = scope.ServiceProvider.GetRequiredService<IProgressRepository>();

        Learners.Clear();
        foreach (var p in await progress.GetAllProfilesAsync())
            Learners.Add(p);

        SelectedLearner = Learners.FirstOrDefault();
    }

    private async Task CreateAsync()
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var progress = scope.ServiceProvider.GetRequiredService<IProgressRepository>();

        var created = await progress.CreateProfileAsync(NewLearnerName.Trim(), SelectedAvatar);
        Learners.Add(created);
        SelectedLearner = created;
        NewLearnerName = string.Empty;
    }

    private Task StartAsync()
    {
        if (SelectedLearner is not null)
            _nav.NavigateToLesson(SelectedLearner.Id);
        return Task.CompletedTask;
    }
}

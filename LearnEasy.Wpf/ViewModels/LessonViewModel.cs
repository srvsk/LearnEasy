using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Input;
using LearnEasy.Core.Abstractions;
using LearnEasy.Core.Models;
using LearnEasy.Wpf.Mvvm;
using LearnEasy.Wpf.Navigation;
using Microsoft.Extensions.DependencyInjection;

namespace LearnEasy.Wpf.ViewModels;

/// <summary>
/// Drives the teacher loop in the UI: speak → child types → feedback → next.
/// All lesson logic lives in <see cref="ISpellingLessonService"/>; this class
/// only adapts it to bindable state and commands.
/// </summary>
public sealed class LessonViewModel : ObservableObject
{
    private readonly ISpellingLessonService _lesson;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly INavigationService _nav;

    private int _learnerId;
    private readonly Stopwatch _timer = new();

    public LessonViewModel(
        ISpellingLessonService lesson,
        IServiceScopeFactory scopeFactory,
        INavigationService nav)
    {
        _lesson = lesson;
        _scopeFactory = scopeFactory;
        _nav = nav;

        SpeakAgainCommand = new AsyncRelayCommand(() => _lesson.RepeatWordAsync(), () => !IsBusy && !ShowResult);
        HintCommand = new AsyncRelayCommand(HintAsync, () => !IsBusy && !ShowResult);
        SubmitCommand = new AsyncRelayCommand(SubmitAsync,
            () => !IsBusy && !ShowResult && !string.IsNullOrWhiteSpace(TypedAnswer));
        NextCommand = new AsyncRelayCommand(NextAsync, () => !IsBusy);
        BackCommand = new RelayCommand(() => _nav.NavigateToStart());
    }

    // --- header (learner stats) --------------------------------------------
    private string _learnerName = "";
    public string LearnerName { get => _learnerName; private set => SetProperty(ref _learnerName, value); }

    private string _avatarKey = "fox";
    public string AvatarKey { get => _avatarKey; private set => SetProperty(ref _avatarKey, value); }

    private int _points;
    public int Points { get => _points; private set => SetProperty(ref _points, value); }

    private int _level = 1;
    public int Level { get => _level; private set => SetProperty(ref _level, value); }

    private int _streak;
    public int Streak { get => _streak; private set => SetProperty(ref _streak, value); }

    private string _difficulty = "Starter";
    public string Difficulty { get => _difficulty; private set => SetProperty(ref _difficulty, value); }

    // --- current turn ------------------------------------------------------
    private bool _isBusy = true;
    public bool IsBusy { get => _isBusy; private set => SetProperty(ref _isBusy, value); }

    private string _prompt = "Getting your words ready…";
    public string Prompt { get => _prompt; private set => SetProperty(ref _prompt, value); }

    private string _typedAnswer = "";
    public string TypedAnswer { get => _typedAnswer; set => SetProperty(ref _typedAnswer, value); }

    private string? _hintText;
    public string? HintText { get => _hintText; private set => SetProperty(ref _hintText, value); }

    // --- result panel ------------------------------------------------------
    private bool _showResult;
    public bool ShowResult { get => _showResult; private set => SetProperty(ref _showResult, value); }

    private bool _wasCorrect;
    public bool WasCorrect { get => _wasCorrect; private set => SetProperty(ref _wasCorrect, value); }

    private string _feedback = "";
    public string Feedback { get => _feedback; private set => SetProperty(ref _feedback, value); }

    private string? _correctSpelling;
    public string? CorrectSpelling { get => _correctSpelling; private set => SetProperty(ref _correctSpelling, value); }

    public ObservableCollection<string> NewBadges { get; } = [];

    // --- commands ----------------------------------------------------------
    public ICommand SpeakAgainCommand { get; }
    public ICommand HintCommand { get; }
    public ICommand SubmitCommand { get; }
    public ICommand NextCommand { get; }
    public ICommand BackCommand { get; }

    public async Task InitializeAsync(int learnerId)
    {
        _learnerId = learnerId;
        IsBusy = true;
        await _lesson.StartSessionAsync(learnerId);
        await RefreshHeaderAsync();
        await NextAsync();
    }

    private async Task NextAsync()
    {
        IsBusy = true;
        ShowResult = false;
        HintText = null;
        TypedAnswer = "";
        NewBadges.Clear();

        var step = await _lesson.NextWordAsync();
        Prompt = "Listen carefully, then type the word.";
        _ = step; // the spelling is intentionally NOT shown
        _timer.Restart();
        IsBusy = false;
    }

    private async Task HintAsync()
    {
        IsBusy = true;
        var step = await _lesson.RequestHintAsync();
        HintText = step.HintText;
        IsBusy = false;
    }

    private async Task SubmitAsync()
    {
        IsBusy = true;
        _timer.Stop();

        var result = await _lesson.SubmitAsync(TypedAnswer, (int)_timer.ElapsedMilliseconds);

        WasCorrect = result.IsCorrect;
        Feedback = result.Feedback;
        CorrectSpelling = result.CorrectSpelling;
        Difficulty = result.CurrentDifficulty.ToString();

        NewBadges.Clear();
        foreach (var b in result.NewBadges)
            NewBadges.Add($"{b.Name} — {b.Description}");

        await RefreshHeaderAsync();
        ShowResult = true;
        IsBusy = false;
    }

    /// <summary>Reload the learner card from storage so stats stay truthful.</summary>
    private async Task RefreshHeaderAsync()
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var progress = scope.ServiceProvider.GetRequiredService<IProgressRepository>();
        var profile = await progress.GetProfileAsync(_learnerId);
        if (profile is null) return;

        LearnerName = profile.DisplayName;
        AvatarKey = profile.AvatarKey;
        Points = profile.TotalPoints;
        Level = profile.Level;
        Streak = profile.CurrentStreak;
        Difficulty = profile.CurrentDifficulty.ToString();
    }
}

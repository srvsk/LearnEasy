# LearnEasy 🐥 — A Friendly Spelling Teacher for Kids

LearnEasy is a Windows desktop app that teaches children (ages ~6–9) English
spelling. It behaves like a patient teacher: it **says a word out loud**, the
child **types it**, and the app gives **warm, spoken feedback**, **progressive
hints**, **points & badges**, and quietly **adapts the difficulty** to each
learner.

- **UI:** WPF (.NET 10), MVVM, kid-friendly theme
- **Database:** PostgreSQL via EF Core (words, learners, attempts, badges)
- **Speech (TTS):** [piper1-gpl](https://github.com/OHF-Voice/piper1-gpl) (open-source neural TTS) as an Aspire-orchestrated container, with a Windows `System.Speech` fallback
- **NLP:** local [Ollama](https://ollama.com) running **`phi4-mini`** (hints, encouragement, explanations) — with deterministic fallbacks so a lesson never blocks on the model
- **Orchestration:** .NET Aspire (spins up Postgres + Ollama + Piper, injects connection strings)

---

## Solution layout

```
LearnEasy.slnx
├─ src/
│  ├─ LearnEasy.Core       # Domain models + abstractions (no infra deps)
│  ├─ LearnEasy.Data       # EF Core + Npgsql, repositories, seed data
│  ├─ LearnEasy.Services   # Piper TTS, Ollama NLP, adaptive, gamification, "teacher"
│  └─ LearnEasy.Wpf        # Kid-friendly MVVM UI (composition root)
└─ aspire/
   ├─ LearnEasy.AppHost        # Orchestrates Postgres + Ollama + the WPF app
   └─ LearnEasy.ServiceDefaults# Aspire telemetry/health defaults (for future API)
```

**Dependency flow:** `Wpf → Services → Core ← Data`. The UI only ever talks to
`ISpellingLessonService`; everything else is swappable behind interfaces.

### Key example code (where to look)

| Requirement | File |
|---|---|
| Connect to Postgres & retrieve words | `LearnEasy.Data/Repositories/WordRepository.cs`, `LearnEasyDbContext.cs` |
| Call Piper for speech synthesis | `LearnEasy.Services/Speech/HttpPiperTextToSpeechService.cs` (+ `PiperTextToSpeechService.cs` CLI fallback) |
| Use Ollama `phi4-mini` for NLP | `LearnEasy.Services/Nlp/OllamaNlpService.cs` |
| Teacher flow (ask → respond → feedback) | `LearnEasy.Services/Learning/SpellingLessonService.cs`, `LearnEasy.Wpf/ViewModels/LessonViewModel.cs` |
| Adaptive difficulty | `LearnEasy.Services/Learning/AdaptiveDifficultySelector.cs` |
| Gamification (points/levels/badges) | `LearnEasy.Services/Learning/GamificationEngine.cs` |

---

## Prerequisites

- **.NET 10 SDK**
- **.NET Aspire workload:** `dotnet workload install aspire`
- **Docker Desktop** (Aspire runs Postgres, Ollama and Piper as containers)
- **Windows 10/11** (WPF + `System.Speech`)
- Piper needs no manual setup — Aspire builds & runs it (see Speech section)

> ⚠️ **NuGet versions:** package versions in the `.csproj` files target the
> .NET 10 / Aspire 13.1 wave. If `dotnet restore` can't resolve an exact
> version, run `dotnet restore` and let it float, or
> `dotnet add <project> package <name>` to pull the latest compatible build.
> The community Ollama integration in particular versions independently.

---

## Run it (recommended: one click via Aspire)

```bash
dotnet restore
dotnet run --project LearnEasy.AppHost
```

This will:

1. Start **PostgreSQL** (data persisted in a Docker volume) and open pgAdmin.
2. Start **Ollama** and pull **`phi4-mini`** on first run (one-time download).
3. Launch the **WPF app** with `ConnectionStrings__learneasydb` and
   `ConnectionStrings__ollama` injected automatically.
4. Open the **Aspire dashboard** so you can watch logs/health of each piece.

On startup the app creates the schema and seeds ~24 starter words + 6 badges,
so you can play immediately.

### Run the WPF app standalone (no Aspire)

You can also `F5` `LearnEasy.Wpf` directly. It falls back to
`appsettings.json`:

```jsonc
"ConnectionStrings": {
  "learneasydb": "Host=localhost;Port=5432;Database=learneasy;Username=postgres;Password=postgres",
  "ollama": "http://localhost:11434"
}
```

Bring your own Postgres and `ollama serve` (with `ollama pull phi4-mini`).
If Postgres is unreachable the app shows a friendly message and exits;
if Ollama is unreachable, hints/feedback gracefully use built-in phrasing.

---

## Speech (Piper, containerized)

Speech uses **[piper1-gpl](https://github.com/OHF-Voice/piper1-gpl)** (the
current home of Piper) running as an **HTTP server in a container that Aspire
builds and orchestrates** — no manual install.

**With Aspire (default, recommended):** nothing to do. The AppHost builds
[`piper/Dockerfile`](piper/Dockerfile) (`pip install piper-tts flask`, with the
voice **baked into the image** so it runs fully offline), starts it, waits for
`GET /voices` to go healthy, and injects its URL into the app as
`ConnectionStrings:piper`. The app then synthesizes via `POST /` and caches
every clip on disk (content-hashed), so repeated words are instant.

Change the voice via the Dockerfile build arg
(`--build-arg VOICE=en_US-amy-medium`); browse voices in the
[VOICES.md](https://github.com/OHF-Voice/piper1-gpl/blob/main/docs/VOICES.md).

**Standalone (no Aspire):** run the server yourself and point the app at it:

```bash
docker run --rm -p 5000:5000 <piper-image> server -m en_US-lessac-medium
```

```jsonc
"Piper": { "Endpoint": "http://localhost:5000", "FallbackToSystemSpeech": true }
```

**No Piper at all?** Leave `Endpoint` empty — the app uses the on-disk piper
CLI if configured, and ultimately the Windows `System.Speech` engine, so audio
always works. Speech is best-effort and never blocks a lesson.

Implementations: [`HttpPiperTextToSpeechService.cs`](LearnEasy.Services/Speech/HttpPiperTextToSpeechService.cs)
(container/HTTP, default) and [`PiperTextToSpeechService.cs`](LearnEasy.Services/Speech/PiperTextToSpeechService.cs)
(local CLI fallback).

---

## Database migrations

A fresh clone runs immediately (`EnsureCreated` fallback). For the proper
**production / versioned-schema path**, generate the initial migration once:

```bash
dotnet tool install --global dotnet-ef
dotnet ef migrations add InitialCreate --project LearnEasy.Data --startup-project LearnEasy.Wpf
```

After any migration exists, the app applies migrations on startup
(`EnsureDatabaseAsync` in `LearnEasy.Data/DataServiceCollectionExtensions.cs`).
For design-time, `DesignTimeDbContextFactory` uses
`Host=localhost;...;Username=postgres;Password=postgres` — override with the
`LEARNEASY_DESIGN_CONNECTION` environment variable.

---

## Deploying with Aspire

For local/team use, the AppHost **is** the deployment unit (`dotnet run
--project LearnEasy.AppHost`). To publish infrastructure manifests:

```bash
# Generate the Aspire manifest (infra topology + connection wiring)
dotnet run --project LearnEasy.AppHost -- --publisher manifest --output-path ./aspire-manifest.json

# Or use the Aspire CLI for container/cloud targets
aspire publish
```

**Notes & caveats**

- A **WPF desktop app is a GUI process**, not a web service. Aspire happily
  launches it locally and injects connection strings, but it is *not* something
  you'd "deploy to the cloud". The cloud-deployable units here are **Postgres**
  and **Ollama**; the WPF client is distributed to end-user machines (e.g.
  MSIX/ClickOnce) configured to point at those endpoints.
- `LearnEasy.ServiceDefaults` is kept ready so you can later add a thin
  **`LearnEasy.Api`** (ASP.NET) between the app and the database for a hosted
  multiplayer "spelling bee" mode without restructuring.

---

## How the "teacher" works

```
StartSessionAsync(learner)
        │
        ▼
NextWordAsync ──► pick word in learner's band ──► Piper speaks "Can you spell… rabbit?"
        │
        ├─ RequestHintAsync ──► progressive hint (letters → first letter → syllables → phonetic via phi4-mini)
        ├─ RepeatWordAsync  ──► speaks the word again
        │
        ▼
SubmitAsync(text) ──► grade ──► score + streak + badges (GamificationEngine)
                              └─► adapt band from last 8 attempts (AdaptiveDifficultySelector)
                              └─► spoken encouragement (phi4-mini, with fallback)
```

Adaptive rule (pure, unit-testable): of the last 4 attempts, 3+ confident
corrects → move up a band; 3+ wrong → move down.

---

## Suggested open-source libraries / tools

| Need | Recommended | Why |
|---|---|---|
| MVVM boilerplate | **CommunityToolkit.Mvvm** | `[ObservableProperty]` / `[RelayCommand]` source generators — drop-in replacement for the hand-rolled `Mvvm/` base classes |
| Ollama client | **OllamaSharp** | Typed streaming client if you outgrow the raw `/api/generate` call in `OllamaNlpService` |
| Audio playback | **NAudio** | Volume control, MP3, overlapping sounds (rewards) — implement `IAudioPlayer` |
| Richer voices (stay local/offline) | Swap the Piper voice via the `VOICE` build arg (many medium/high voices), or **[Kokoro TTS](https://github.com/hexgrad/kokoro)** (Apache-2.0, small, CPU-friendly) | Better expression without leaving the open-source/offline model — implement `ITextToSpeechService` |
| Richer voices (cloud, paid) | **Azure AI Speech** or **ElevenLabs** | Most expressive neural voices; trades the offline guarantee for quality — implement `ITextToSpeechService`. *(Microsoft VibeVoice is no longer a good fit: its official TTS code was pulled in 2025; only unofficial community forks remain, and it's GPU-heavy research-grade.)* |
| Modern WPF visuals | **WPF-UI** or **MaterialDesignInXAML** | Polished kid-friendly controls/animations |
| Resilience | **Polly** (already via `Microsoft.Extensions.Http.Resilience`) | Retry/timeout around Ollama/Piper |
| Handwriting input (future) | **Windows.Media.Ink** + **ML.NET / ONNX Runtime** | Let kids *write* letters instead of typing |
| Testing | **xUnit + FluentAssertions + Testcontainers** | `Testcontainers` spins real Postgres for repository tests |

---

## Extensibility hooks built in

- **Swap speech/NLP** without touching the UI — implement `ITextToSpeechService`
  / `INlpService` and re-register in `ServicesServiceCollectionExtensions`.
- **Multiplayer spelling bee** — add `LearnEasy.Api` (uses `ServiceDefaults`),
  move `SpellingLessonService` server-side, keep WPF as a thin client.
- **Handwriting recognition** — add an `IInkRecognitionService`, feed its text
  into the same `SubmitAsync` path.
- **More content** — extend the seed list in `LearnEasy.Data/SeedData.cs`
  (ids are explicit, so migrations stay clean) or load from a CSV/admin tool.

---

## Known limitations

- Versions in `.csproj` may need a `dotnet restore` refresh for your exact SDK
  feed (see warning above). This scaffold was authored, not compiled, against a
  live feed.
- First Ollama run downloads `phi4-mini` (~2–3 GB) — expect a one-time wait;
  lessons still work meanwhile via fallbacks.
- Windows-only (WPF + `System.Speech`). Core/Data/Services are cross-platform
  and ready for a non-Windows front end later.
- The Piper image is pinned to `piper:learneasy`, which prevents the per-run
  ~533 MB image churn (Aspire would otherwise tag each Dockerfile build with a
  unique hash). The container uses the default Session lifetime, so it is torn
  down when the AppHost stops and recreated next run from the Docker-cached
  build (fast; only rebuilds if `piper/Dockerfile` changes). To clear old
  hash-tagged images left over from before this fix:
  `docker images piper -q | % { docker rmi -f $_ }` (PowerShell).

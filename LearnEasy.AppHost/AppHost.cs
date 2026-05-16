// LearnEasy orchestration.
//
// Aspire spins up the infrastructure the desktop app needs — a PostgreSQL
// database, a local Ollama model and the Piper TTS server — and injects their
// connection details into the WPF client as environment variables. Run THIS
// project to get the full stack with one F5 (the Aspire dashboard shows
// logs/health for each).

var builder = DistributedApplication.CreateBuilder(args);

// --- PostgreSQL --------------------------------------------------------------
// WithDataVolume() keeps learner progress between runs.
// WithPgAdmin() gives a browser DB explorer (optional, handy for debugging).
var postgres = builder.AddPostgres("postgres")
    .WithDataVolume("learneasy-pgdata")
    .WithPgAdmin();

var database = postgres.AddDatabase("learneasydb");

// --- Ollama (local NLP model) ------------------------------------------------
// The model is pulled into a persisted volume on first run, then reused.
// Resource name "ollama" → the WPF app receives ConnectionStrings__ollama.
var ollama = builder.AddOllama("ollama-server")
    .WithDataVolume("learneasy-ollama");

var phi4Mini = ollama.AddModel("ollama", "phi4-mini");

// --- Piper TTS (piper1-gpl HTTP server) --------------------------------------
// Built from ../piper/Dockerfile (no prebuilt image is published upstream).
// The voice is baked into the image, so no runtime download / volume needed.
// /voices is a cheap GET that proves the server is actually ready.
//
// WithImageTag pins a STABLE tag (piper:learneasy) — without it Aspire tags
// every build with a unique hash, so each AppHost run leaves behind a fresh
// ~533 MB image. The tag is what prevents image churn; the container itself
// uses the default Session lifetime, so it is torn down when the AppHost
// stops (like Postgres/Ollama) and recreated next run from the Docker-cached
// image build (fast — no rebuild unless piper/Dockerfile changes).
var piper = builder.AddDockerfile("piper", "../piper")
    .WithImageTag("learneasy")
    .WithHttpEndpoint(targetPort: 5000, name: "http")
    .WithHttpHealthCheck("/voices", endpointName: "http");

// --- WPF client --------------------------------------------------------------
// Launched as a project resource; waits for its dependencies to be healthy
// so the first lesson never races a cold database, model or TTS server.
// Piper's resolved URL is passed as the "piper" connection string to match how
// the app reads Postgres/Ollama (ConnectionStrings__piper).
builder.AddProject<Projects.LearnEasy_Wpf>("learneasy-app")
    .WithReference(database).WaitFor(database)
    .WithReference(phi4Mini).WaitFor(phi4Mini)
    .WithEnvironment("ConnectionStrings__piper", piper.GetEndpoint("http"))
    .WaitFor(piper);

builder.Build().Run();

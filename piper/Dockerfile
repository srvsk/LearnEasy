# LearnEasy — Piper TTS HTTP server (piper1-gpl).
#
# The upstream project (https://github.com/OHF-Voice/piper1-gpl) ships a
# Dockerfile but publishes NO prebuilt image (its release pipeline only pushes
# PyPI wheels). So we build a tiny self-contained image from the published
# `piper-tts` wheel — no source checkout or C/CMake toolchain needed.
#
# The chosen voice is downloaded at build time and baked in, so the running
# container needs no network and start-up is instant (good for a kids' app and
# offline classrooms). Change the voice via the VOICE build arg.

FROM python:3.12-slim

ENV PIP_BREAK_SYSTEM_PACKAGES=1 \
    PIP_NO_CACHE_DIR=1

# piper-tts = the engine; flask = required by piper.http_server.
RUN pip install "piper-tts" "flask>=3,<4"

# Bake a voice into the image (no runtime download).
# Browse voices: https://github.com/OHF-Voice/piper1-gpl/blob/main/docs/VOICES.md
ARG VOICE=en_GB-jenny_dioco-medium
RUN python -m piper.download_voices ${VOICE} --data-dir /data

EXPOSE 5000

# http_server: POST / {"text":"..."} -> WAV ; GET /voices -> JSON list.
# --host 0.0.0.0 so Aspire's port mapping can reach it.
ENV PIPER_VOICE=${VOICE}
ENTRYPOINT ["sh", "-c", "exec python -m piper.http_server --host 0.0.0.0 --port 5000 --data-dir /data -m \"$PIPER_VOICE\""]

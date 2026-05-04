#!/bin/sh
# Container entrypoint for ConfluenceSynkMD.
#
# Starts a single Xvfb instance for the container's lifetime so that
# Electron-based renderers (drawio-desktop) can run headlessly without
# starting a fresh display server per invocation. DISPLAY=:99 is set in
# the Dockerfile; child processes inherit it.

set -e

Xvfb :99 -screen 0 1024x768x24 -nolisten tcp >/dev/null 2>&1 &
XVFB_PID=$!

# Wait up to 5 seconds for Xvfb to be ready before exec'ing the app.
# Closes the startup race where drawio spawns before the display listens.
i=0
while [ "$i" -lt 5 ]; do
    if xdpyinfo -display :99 >/dev/null 2>&1; then
        break
    fi
    i=$((i + 1))
    sleep 1
done

if ! xdpyinfo -display :99 >/dev/null 2>&1; then
    echo "entrypoint: Xvfb failed to start on :99 within 5s" >&2
    exit 1
fi

cleanup() {
    kill "$XVFB_PID" 2>/dev/null || true
    wait "$XVFB_PID" 2>/dev/null || true
}
trap cleanup EXIT INT TERM

exec dotnet /app/ConfluenceSynkMD.dll "$@"

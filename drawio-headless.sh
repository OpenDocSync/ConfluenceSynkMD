#!/bin/sh
# /usr/local/bin/drawio-headless — invocation wrapper for drawio-desktop
# inside the published Docker image.
#
# drawio-desktop is an Electron app. It expects:
#   1. A display server. Provided by entrypoint.sh's Xvfb on :99,
#      inherited via DISPLAY=:99 in the Dockerfile ENV.
#   2. A session-level dbus. Without it, parts of Electron's IPC fail to
#      initialize and drawio's commander parser falls into a fallback path
#      that emits "Error: input file/directory not found" and exits 0.
#      `dbus-run-session` (from the dbus-x11 package) creates a private
#      session bus for the duration of the wrapped command and tears it
#      down on exit.
#   3. Sandbox + GPU disabled. Both must appear BEFORE drawio's own CLI
#      flags so Electron's main process consumes them before commander
#      starts parsing the export options.

exec dbus-run-session -- \
  /usr/bin/drawio \
    --no-sandbox \
    --disable-gpu \
    --disable-dev-shm-usage \
    "$@"

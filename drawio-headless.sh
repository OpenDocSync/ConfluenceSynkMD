#!/bin/sh
# /usr/local/bin/drawio-headless — invocation wrapper for drawio-desktop
# inside the published Docker image.
#
# drawio-desktop's CLI parser (commander.js) does NOT consume Electron flags
# like --no-sandbox / --disable-gpu before its own option parsing — empirically
# they bleed into commander's positional-argument list and trigger
# "Error: input file/directory not found" when the renderer also passes a
# real input path. Fix: wrap drawio so the Electron flags appear BEFORE any
# user args via shell exec, and the renderer just calls `drawio-headless`
# with its export options. Eliminates the multi-token DRAWIO_CMD path.
#
# Display server: the container's entrypoint.sh starts a single Xvfb on
# :99 for the container's lifetime; this script inherits DISPLAY=:99 from
# the Dockerfile's ENV.

exec /usr/bin/drawio \
  --no-sandbox \
  --disable-gpu \
  --disable-dev-shm-usage \
  "$@"

#!/usr/bin/env bash
set -euo pipefail

# Run from the repository directory on a Docker Compose host with a private .env.
: "${API_IMAGE:?Set API_IMAGE to an immutable published image tag}"
docker compose pull api migrate
docker compose up -d --no-build
api_address="$(docker compose port api 8080)"
for attempt in {1..30}; do
  if curl --fail --silent "http://${api_address}/health/ready"; then
    exit 0
  fi
  sleep 2
done
echo "API readiness check failed; inspect docker compose logs api migrate." >&2
exit 1

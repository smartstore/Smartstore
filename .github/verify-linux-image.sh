#!/bin/bash
set -euo pipefail

image=$1
platform=$2
[[ $(docker image inspect "$image" --format '{{.Os}}/{{.Architecture}}') == "$platform" ]]

# Exercise native dependencies in the unmodified production image, before test tools are added.
docker run --rm --platform "$platform" --entrypoint /bin/bash "$image" -euo pipefail -c '
    dotnet --info
    test -x /app/Smartstore.Web
    case "$(uname -m)" in x86_64) rid=linux-x64 ;; aarch64) rid=linux-arm64 ;; *) exit 1 ;; esac
    test -x "/app/runtimes/$rid/native/lightningcss"
    "/app/runtimes/$rid/native/lightningcss" --version
    [[ $(wkhtmltopdf --version) == "wkhtmltopdf 0.12.6.1 (with patched qt)" ]]
    dependencies=$(ldd /usr/local/bin/wkhtmltopdf)
    [[ "$dependencies" != *"not found"* ]]
'

# Starting the real apphost catches wrong-RID assets that a dotnet --info check cannot.
container=$(docker run -d --platform "$platform" -p 127.0.0.1::80 "$image")
cleanup() {
    docker logs "$container" || true
    docker rm -f "$container" >/dev/null || true
}
trap cleanup EXIT
port=$(docker port "$container" 80/tcp)
for ((attempt=0; attempt<90; attempt++)); do
    if curl --fail --silent --connect-timeout 2 --max-time 2 --output /dev/null "http://$port/"; then
        echo "Production image startup passed ($platform)."
        exit 0
    fi
    [[ $(docker inspect "$container" --format '{{.State.Running}}') == true ]] || break
    sleep 2
done
echo "Production image did not become ready ($platform)." >&2
exit 1

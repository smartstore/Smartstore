#!/usr/bin/env bash
set -euo pipefail
if [[ $# != 2 ]]; then
    echo "Usage: $0 <wkhtmltopdf package directory> <trusted HTTPS PNG/JPEG URL>" >&2
    exit 1
fi
repository=$(cd -- "$(dirname -- "$0")/../.." && pwd)
package_source=$(cd -- "$1" && pwd)
https_image=$2
[[ "$https_image" == https://* ]] || { echo 'An HTTPS image URL is required.' >&2; exit 1; }
scratch="$repository/.temp/wkhtml-tests"
mkdir -p "$scratch/packages" "$scratch/results"
for rid in linux-x64 linux-arm64; do
    package="Smartstore.wkhtmltopdf.Native.$rid.0.12.6.1.nupkg"
    test -f "$package_source/$rid/$package"
    cp -- "$package_source/$rid/$package" "$scratch/packages/$package"
done
cd "$repository"
docker info --format '{{.Architecture}}' | tee "$scratch/results/docker-host-architecture.txt"
for architecture in ${WKHTML_TEST_ARCHES:-amd64 arm64}; do
    case "$architecture" in amd64|arm64) ;; *) exit 1 ;; esac
    for mode in system lazy; do
        image="smartstore-wkhtml-test:$architecture-$mode"
        docker build --platform "linux/$architecture" --build-arg "MODE=$mode" \
            -f build/wkhtml-tests/Tests.Dockerfile -t "$image" . \
            2>&1 | tee "$scratch/results/build-$architecture-$mode.log"
        docker run --rm --platform "linux/$architecture" \
            --mount "type=bind,source=$scratch/results,target=/work/.temp/results" \
            -e "WKHTML_HTTPS_IMAGE_URL=$https_image" "$image" \
            2>&1 | tee "$scratch/results/run-$architecture-$mode.log"
    done
done
echo "Technical checks finished. Review all page PNGs and test real shop documents; see build/wkhtml-tests/README.md."

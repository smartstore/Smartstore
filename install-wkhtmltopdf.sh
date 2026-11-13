#!/bin/bash
set -euo pipefail

# Keep the runtime libraries from one distribution; never mix Debian into Ubuntu.
source /etc/os-release
if [[ "$ID" != ubuntu || "$VERSION_ID" != 24.04 ]]; then
    echo "wkhtmltopdf provisioning requires Ubuntu 24.04 (Noble)." >&2
    exit 1
fi

architecture=$(dpkg --print-architecture)
case "$architecture" in
    amd64)
        package_sha=98ba0d157b50d36f23bd0dedf4c0aa28c7b0c50fcdcdc54aa5b6bbba81a3941d
        binary_sha=eafc8a76b3e9912d20fb358f3296362fde76030fcaeec1cefcef30bc0db81e4b
        ;;
    arm64)
        package_sha=b6606157b27c13e044d0abbe670301f88de4e1782afca4f9c06a5817f3e03a9c
        binary_sha=55127a0e37fb2d2bde47a658de6ebff4e57088ae66e45f7dbe909f6384971301
        ;;
    *) echo "Unsupported wkhtmltopdf architecture: $architecture" >&2; exit 1 ;;
esac

mode=${1:-install}
if [[ "$mode" != install && "$mode" != --dependencies-only ]]; then
    echo "Usage: $0 [--dependencies-only]" >&2
    exit 1
fi

# Bookworm's binary needs JPEG ABI 62 and OpenSSL 3, not Ubuntu's JPEG ABI 8.
apt-get update
apt-get -y install --no-install-recommends \
    wget ca-certificates libc6 libgcc-s1 libstdc++6 zlib1g \
    libjpeg62 libpng16-16t64 libfreetype6 libfontconfig1 fontconfig \
    libx11-6 libxcb1 libxext6 libxrender1 libssl3t64 \
    fonts-liberation xfonts-75dpi xfonts-base
apt-get clean

# The lazy-deployment test uses exactly the same dependencies, without a system tool.
[[ "$mode" == --dependencies-only ]] && exit 0

scratch_root=${WKHTML_WORK_ROOT:-$PWD/.temp/wkhtmltopdf}
case "$scratch_root" in
    /*/.temp/*) ;;
    *) echo "WKHTML_WORK_ROOT must be an absolute path below .temp." >&2; exit 1 ;;
esac
mkdir -p "$scratch_root"
scratch=$(mktemp -d "$scratch_root/install.XXXXXX")
trap 'rm -rf -- "$scratch"' EXIT
package="wkhtmltox_0.12.6.1-3.bookworm_${architecture}.deb"
wget -q --https-only --timeout=30 --tries=3 \
    "https://github.com/wkhtmltopdf/packaging/releases/download/0.12.6.1-3/${package}" \
    -O "$scratch/$package"
echo "$package_sha  $scratch/$package" | sha256sum --check --status

# Extract only our executable; do not force-install Debian package metadata.
dpkg-deb --extract "$scratch/$package" "$scratch/extracted"
echo "$binary_sha  $scratch/extracted/usr/local/bin/wkhtmltopdf" | sha256sum --check --status
install -m 0755 "$scratch/extracted/usr/local/bin/wkhtmltopdf" /usr/local/bin/wkhtmltopdf
printf '%s\n' '0.12.6.1' > /usr/local/bin/wkhtmltopdf.version

dependencies=$(ldd /usr/local/bin/wkhtmltopdf)
if [[ "$dependencies" == *'not found'* ]]; then
    printf '%s\n' "$dependencies" >&2
    exit 1
fi
version=$(/usr/local/bin/wkhtmltopdf --version)
[[ "$version" == 'wkhtmltopdf 0.12.6.1 (with patched qt)' ]] || {
    echo "Unexpected wkhtmltopdf build: $version" >&2
    exit 1
}
printf 'Installed %s (%s), SHA-256 %s\n' "$version" "$architecture" "$binary_sha"

# Catch headless startup/rendering failures during the image build, not in production.
printf '<html><body><h1>Smartstore PDF installation check</h1></body></html>\n' > "$scratch/input.html"
timeout 30 /usr/local/bin/wkhtmltopdf --quiet "$scratch/input.html" "$scratch/output.pdf"
[[ $(head -c 5 "$scratch/output.pdf") == '%PDF-' ]] || {
    echo 'wkhtmltopdf did not produce a PDF.' >&2
    exit 1
}

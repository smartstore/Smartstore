#!/bin/bash
set -e

# Install wkhtmltopdf dependencies for Debian Trixie (.NET 10)
echo "deb [trusted=yes] http://deb.debian.org/debian bookworm main" > /etc/apt/sources.list.d/bookworm.list
apt-get update
apt-get -y install --no-install-recommends \
    wget \
    ca-certificates \
    libjpeg62-turbo \
    libxrender1 \
    libfontconfig1 \
    libx11-6 \
    libxext6 \
    libssl3t64 \
    fonts-liberation \
    xfonts-75dpi \
    xfonts-base

# Download and install wkhtmltopdf for the container architecture (amd64, arm64, ...)
ARCH="$(dpkg --print-architecture)"
WKHTMLTOPDF_PACKAGE="wkhtmltox_0.12.6.1-3.bookworm_${ARCH}.deb"
wget "https://github.com/wkhtmltopdf/packaging/releases/download/0.12.6.1-3/${WKHTMLTOPDF_PACKAGE}"
dpkg --force-depends -i "./${WKHTMLTOPDF_PACKAGE}"
apt-get -y --fix-broken install

# Cleanup
rm "./${WKHTMLTOPDF_PACKAGE}"
rm /etc/apt/sources.list.d/bookworm.list
apt-get update
apt-get clean
rm -rf /var/lib/apt/lists/*
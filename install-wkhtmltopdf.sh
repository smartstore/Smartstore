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

# Download and install wkhtmltopdf
wget https://github.com/wkhtmltopdf/packaging/releases/download/0.12.6.1-3/wkhtmltox_0.12.6.1-3.bookworm_amd64.deb
dpkg --force-depends -i ./wkhtmltox_0.12.6.1-3.bookworm_amd64.deb
apt-get -y --fix-broken install

# Cleanup
rm ./wkhtmltox_0.12.6.1-3.bookworm_amd64.deb
rm /etc/apt/sources.list.d/bookworm.list
apt-get update
apt-get clean
rm -rf /var/lib/apt/lists/*
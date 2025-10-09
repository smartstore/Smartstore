#!/bin/bash
set -e

DOTNET_VERSION="9.0"
APP_DIR="/app"
GLOBAL_TOOLS=(
    "dotnet-ef"
    "dotnet-aspnet-codegenerator"
    "dotnet-dev-certs"
)

export DEBIAN_FRONTEND=noninteractive
sudo apt-get update -qq
sudo apt-get install -y -qq --no-install-recommends \
    wget ca-certificates libc6 libgcc1 libgssapi-krb5-2 \
    libicu-dev libssl-dev libstdc++6 zlib1g

ARCH=$(dpkg --print-architecture)
case $ARCH in
    amd64) DOTNET_ARCH="x64" ;;
    arm64) DOTNET_ARCH="arm64" ;;
    armhf) DOTNET_ARCH="arm" ;;
    *) exit 1 ;;
esac

wget -q https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh
chmod +x dotnet-install.sh
./dotnet-install.sh --channel "$DOTNET_VERSION" --architecture "$DOTNET_ARCH" \
    --install-dir "$HOME/.dotnet" --no-path
rm dotnet-install.sh

export DOTNET_ROOT="$HOME/.dotnet"
export PATH="$PATH:$DOTNET_ROOT:$HOME/.dotnet/tools"
grep -qF 'export DOTNET_ROOT=' ~/.bashrc || echo 'export DOTNET_ROOT="$HOME/.dotnet"' >> ~/.bashrc
grep -qF 'export PATH="$PATH:$DOTNET_ROOT' ~/.bashrc || echo 'export PATH="$PATH:$DOTNET_ROOT:$HOME/.dotnet/tools"' >> ~/.bashrc

for tool in "${GLOBAL_TOOLS[@]}"; do
    dotnet tool install --global "$tool" --verbosity quiet
done

if [ -d "$APP_DIR" ]; then
    (cd "$APP_DIR" && dotnet restore Smartstore.sln --verbosity quiet)
fi
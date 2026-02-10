# -----------------------------------------------------------
# Creates a Docker image from an existing build artifact
# -----------------------------------------------------------

ARG ASPNET_TAG=10.0

FROM mcr.microsoft.com/dotnet/aspnet:${ASPNET_TAG}
EXPOSE 80
EXPOSE 443
ENV ASPNETCORE_URLS="http://+:80;https://+:443"

# Copy
ARG EDITION=Community
ARG VERSION=6.3.0
ARG RUNTIME=linux-x64
ARG SOURCE=build/artifacts/${EDITION}.${VERSION}.${RUNTIME}

WORKDIR /app
COPY ${SOURCE} ./

# Install wkhtmltopdf dependencies for Debian Trixie (.NET 10)
# Install Trixie-compatible libraries and fonts for proper PDF rendering
RUN apt-get update && \
    apt-get -y install --no-install-recommends \
    wget \
    ca-certificates \
    # Updated package name for Trixie
    libjpeg62-turbo \
    libxrender1 \
    libfontconfig1 \
    libx11-6 \
    libxext6 \
    # Trixie uses t64 suffix for time64 compatibility
    libssl3t64 \
    # Essential fonts for Smartstore PDFs (Invoices, etc.)
    fonts-liberation \
    xfonts-75dpi \
    xfonts-base && \
    wget https://github.com/wkhtmltopdf/packaging/releases/download/0.12.6.1-3/wkhtmltox_0.12.6.1-3.bookworm_amd64.deb && \
    # English comment: Force install due to 'libssl3' naming mismatch in the Bookworm deb package
    dpkg --force-depends -i ./wkhtmltox_0.12.6.1-3.bookworm_amd64.deb && \
    apt-get -y --fix-broken install && \
    rm ./wkhtmltox_0.12.6.1-3.bookworm_amd64.deb && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/*

ENTRYPOINT ["./Smartstore.Web", "--urls", "http://0.0.0.0:80"]

# -----------------------------------------------------------
# Creates a Docker image by building and publishing 
# the source within the container
# -----------------------------------------------------------

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

# Copy solution and source
ARG SOLUTION=Smartstore.sln
WORKDIR /app
COPY $SOLUTION ./
COPY src/ ./src
COPY test/ ./test
COPY nuget.config ./

# Create Modules dir if missing
RUN mkdir /app/src/Smartstore.Web/Modules -p -v

# Build
RUN dotnet build $SOLUTION -c Release

# Publish
WORKDIR /app/src/Smartstore.Web
RUN dotnet publish Smartstore.Web.csproj -c Release -o /app/release/publish \
	--no-self-contained \
	--no-restore

# Build Docker image
FROM mcr.microsoft.com/dotnet/aspnet:10.0
EXPOSE 80
EXPOSE 443
ENV ASPNETCORE_URLS="http://+:80;https://+:443"
WORKDIR /app
COPY --from=build /app/release/publish .

# Install wkhtmltopdf dependencies for Debian Trixie (.NET 10)
# Install Trixie-compatible libraries and fonts for proper PDF rendering
RUN echo "deb [trusted=yes] http://deb.debian.org/debian bookworm main" > /etc/apt/sources.list.d/bookworm.list && \
    apt-get update && \
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
    xfonts-base && \
    # Download and install wkhtmltopdf
    wget https://github.com/wkhtmltopdf/packaging/releases/download/0.12.6.1-3/wkhtmltox_0.12.6.1-3.bookworm_amd64.deb && \
    dpkg --force-depends -i ./wkhtmltox_0.12.6.1-3.bookworm_amd64.deb && \
    apt-get -y --fix-broken install && \
    # Cleanup to keep image small and remove temporary repo
    rm ./wkhtmltox_0.12.6.1-3.bookworm_amd64.deb && \
    rm /etc/apt/sources.list.d/bookworm.list && \
    apt-get update && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/*

ENTRYPOINT ["./Smartstore.Web", "--urls", "http://0.0.0.0:80"]

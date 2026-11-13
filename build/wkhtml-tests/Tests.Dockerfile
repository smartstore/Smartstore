# Build on the host CPU; only the runtime stage needs target-platform emulation.
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0-noble AS build
ARG TARGETARCH
WORKDIR /src
COPY src/ ./src/
COPY build/wkhtml-tests/ ./build/wkhtml-tests/
COPY .temp/wkhtml-tests/packages/ /packages/
ENV NUGET_PACKAGES=/packages/.temp/nuget
RUN case "$TARGETARCH" in amd64) rid=linux-x64 ;; arm64) rid=linux-arm64 ;; *) exit 1 ;; esac && \
    dotnet publish build/wkhtml-tests/Probe/Probe.csproj -c Release -o /probe \
        --runtime "$rid" --self-contained false -p:UseAppHost=false
RUN case "$TARGETARCH" in amd64) rid=linux-x64 ;; arm64) rid=linux-arm64 ;; *) exit 1 ;; esac && \
    dotnet restore build/wkhtml-tests/seed.csproj -p:ProbeRID=$rid --source /packages --source https://api.nuget.org/v3/index.json

# A test image, not the production image: Poppler/Python are test dependencies only.
FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble
ARG MODE=system
WORKDIR /work
COPY install-wkhtmltopdf.sh /work/install-wkhtmltopdf.sh
RUN if [ "$MODE" = system ]; then bash /work/install-wkhtmltopdf.sh; \
    elif [ "$MODE" = lazy ]; then bash /work/install-wkhtmltopdf.sh --dependencies-only; \
    else exit 1; fi
RUN apt-get update && apt-get install -y --no-install-recommends python3 poppler-utils && apt-get clean
COPY --from=build /probe/ /work/.temp/probe/
COPY build/wkhtml-tests/ /tests/
COPY src/Smartstore.Web/wwwroot/admin/images/bg-mobile.jpg /tests/fixture.jpg
COPY --from=build /packages/.temp/nuget/ /packages/.temp/nuget/
ENV NUGET_PACKAGES=/packages/.temp/nuget
ENV WKHTML_PROBE_MODE=$MODE
ENTRYPOINT ["python3", "/tests/verify.py"]

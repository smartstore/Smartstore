FROM mcr.microsoft.com/dotnet/sdk:10.0-noble AS build
WORKDIR /src
COPY src/ ./src/
COPY build/wkhtml-tests/ ./build/wkhtml-tests/
COPY .temp/wkhtml-tests/packages/ /packages/
ENV NUGET_PACKAGES=/packages/.temp/nuget
RUN dotnet publish build/wkhtml-tests/Probe/Probe.csproj -c Release -o /probe
RUN architecture=$(dpkg --print-architecture) && \
    if [ "$architecture" = amd64 ]; then rid=linux-x64; else rid=linux-arm64; fi && \
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

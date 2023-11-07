# -----------------------------------------------------------
# Creates a Windows Docker image from an existing
# build artifact. Please ensure that Docker
# is running Windows containers.
# -----------------------------------------------------------

ARG ASPNET_TAG=7.0

FROM mcr.microsoft.com/dotnet/aspnet:${ASPNET_TAG}
EXPOSE 80
ENV ASPNETCORE_URLS "http://+:80"

# Copy
ARG EDITION=Community
ARG VERSION=5.1.0
ARG RUNTIME=win-x64
ARG SOURCE=build/artifacts/${EDITION}.${VERSION}.${RUNTIME}

WORKDIR /app
COPY ${SOURCE} ./

ENTRYPOINT ["dotnet", "./Smartstore.Web.dll"]

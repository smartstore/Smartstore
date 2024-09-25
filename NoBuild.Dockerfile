# -----------------------------------------------------------
# Creates a Docker image from an existing build artifact
# -----------------------------------------------------------

ARG ASPNET_TAG=8.0

FROM mcr.microsoft.com/dotnet/aspnet:${ASPNET_TAG}
EXPOSE 80
EXPOSE 443
ENV ASPNETCORE_URLS "http://+:80;https://+:443"

# Copy
ARG EDITION=Community
ARG VERSION=5.1.0
ARG RUNTIME=linux-x64
ARG SOURCE=build/artifacts/${EDITION}.${VERSION}.${RUNTIME}

WORKDIR /app
COPY ${SOURCE} ./

# Install wkhtmltopdf
RUN apt update && \
    apt -y install wkhtmltopdf=0.12.6-1 && \
    apt clean

ENTRYPOINT ["./Smartstore.Web", "--urls", "http://0.0.0.0:80"]

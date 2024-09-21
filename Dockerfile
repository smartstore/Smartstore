# -----------------------------------------------------------
# Creates a Docker image by building and publishing 
# the source within the container
# -----------------------------------------------------------

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

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
FROM mcr.microsoft.com/dotnet/aspnet:8.0
EXPOSE 80
EXPOSE 443
ENV ASPNETCORE_URLS "http://+:80;https://+:443"
WORKDIR /app
COPY --from=build /app/release/publish .

# Install wkhtmltopdf
RUN apt update && \
	apt -y install wkhtmltopdf && \
        apt clean

ENTRYPOINT ["./Smartstore.Web", "--urls", "http://0.0.0.0:80"]

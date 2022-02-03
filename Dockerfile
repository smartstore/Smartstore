FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build

# Build application
WORKDIR /app
COPY Smartstore.Min.sln ./
COPY src/ ./src
#RUN dotnet restore Smartstore.sln
RUN dotnet build Smartstore.Min.sln -c Release -o ./release
WORKDIR /app/src/Smartstore.Web
RUN dotnet publish Smartstore.Web.csproj -c Release -o /app/release/publish --runtime linux-x64 --no-self-contained

# Build docker image
FROM mcr.microsoft.com/dotnet/aspnet:6.0
EXPOSE 80
EXPOSE 443
ENV ASPNETCORE_URLS http://+:80
WORKDIR /app
COPY --from=build /app/release/publish .

# Install wkhtmltopdf
RUN apt update &&\
	apt -y install wget &&\
	wget https://github.com/wkhtmltopdf/packaging/releases/download/0.12.6-1/wkhtmltox_0.12.6-1.buster_amd64.deb &&\ 
	apt -y install ./wkhtmltox_0.12.6-1.buster_amd64.deb &&\
	rm ./wkhtmltox_0.12.6-1.buster_amd64.deb

ENTRYPOINT ["./Smartstore.Web", "--urls", "http://0.0.0.0:80"]
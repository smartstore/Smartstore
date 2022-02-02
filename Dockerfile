FROM mcr.microsoft.com/dotnet/runtime-deps:6.0-alpine

# Add some libs required by .NET runtime 
# https://github.com/dotnet/core/blob/master/Documentation/build-and-install-rhel6-prerequisites.md#troubleshooting
# RUN apk add --no-cache libstdc++ libintl

# Expose
EXPOSE 80

# Globalization support
RUN apk add --no-cache icu-libs
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=False

# Copy
WORKDIR /app
COPY --chown=$USER artifacts/Community.5.0.0.alpine-x64/ ./

# Webserver als Gruppen-Besitzer setzen
# RUN chgrp -R www-data /app

ENTRYPOINT ["./Smartstore.Web", "--urls", "http://0.0.0.0:5001"]

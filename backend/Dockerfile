FROM debian:bookworm-slim AS base

# Install libssl, libicu, and libcurl dependencies
RUN apt-get update && apt-get install -y --no-install-recommends \
    libssl3 \
    libicu72 \
    libcurl4 \
    && rm -rf /var/lib/apt/lists/*

RUN mkdir -p /app/bin
RUN mkdir -p /app/data

WORKDIR /app/bin
COPY publish/backend .
COPY publish/appsettings.json .
EXPOSE 65000

ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1

ENTRYPOINT ["/app/bin/backend"]

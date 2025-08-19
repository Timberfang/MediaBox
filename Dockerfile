FROM docker.io/library/alpine:3 AS builder
WORKDIR /build
RUN apk add --no-cache dotnet9-sdk git clang build-base zlib-dev \
    && git clone https://code.wolfden.diskstation.me/Timberfang/MediaBox.git . \
    && dotnet publish /build/MediaBox/MediaBox.csproj
FROM docker.io/library/alpine:3
WORKDIR /workspace
RUN apk add --no-cache ffmpeg \
    && adduser -S app
COPY --from=builder /build/artifacts/publish/MediaBox/release/* /usr/local/bin/
USER app

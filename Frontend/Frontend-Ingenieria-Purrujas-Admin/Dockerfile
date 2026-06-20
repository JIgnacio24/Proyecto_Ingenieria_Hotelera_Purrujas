FROM oven/bun:1.2.15-slim AS bun

FROM node:22.12-bookworm-slim
COPY --from=bun /usr/local/bin/bun /usr/local/bin/bun

WORKDIR /app

COPY package.json bun.lock ./
RUN bun install --frozen-lockfile

COPY . .

ENV CHOKIDAR_USEPOLLING=true
EXPOSE 4203

CMD ["bun", "run", "ng", "serve", "--host", "0.0.0.0", "--port", "4203", "--configuration", "development"]

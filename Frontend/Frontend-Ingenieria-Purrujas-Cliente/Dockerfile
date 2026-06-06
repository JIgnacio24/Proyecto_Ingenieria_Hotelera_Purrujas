FROM node:22.12-bookworm-slim

WORKDIR /app

COPY package.json package-lock.json ./
RUN npm ci

COPY . .

ENV CHOKIDAR_USEPOLLING=true
EXPOSE 4204

CMD ["npm", "run", "ng", "--", "serve", "--host", "0.0.0.0", "--port", "4204", "--configuration", "development", "--proxy-config", "proxy.docker.conf.json"]

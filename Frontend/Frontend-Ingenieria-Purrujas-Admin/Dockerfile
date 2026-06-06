FROM node:22.12-bookworm-slim

WORKDIR /app

COPY package.json package-lock.json ./
RUN npm ci

COPY . .

ENV CHOKIDAR_USEPOLLING=true
EXPOSE 4203

CMD ["npm", "run", "ng", "--", "serve", "--host", "0.0.0.0", "--port", "4203", "--configuration", "development"]

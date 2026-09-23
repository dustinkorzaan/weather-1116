FROM node:24-slim AS base
WORKDIR /app

COPY mcp-srv-node/package.json mcp-srv-node/package-lock.json ./
RUN npm ci --omit=dev && npm cache clean --force

COPY mcp-srv-node/mcp/src mcp/src

ENV NODE_ENV=production
ENV MCP_SRV_NODE_HOST=0.0.0.0
ENV MCP_SRV_NODE_PORT=8080
EXPOSE 8080

USER node
ENTRYPOINT ["node", "mcp/src/index.ts"]

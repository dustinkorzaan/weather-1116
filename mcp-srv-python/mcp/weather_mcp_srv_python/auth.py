"""Bearer token auth for the MCP HTTP endpoint."""

import hmac

from starlette.middleware.base import BaseHTTPMiddleware
from starlette.requests import Request
from starlette.responses import PlainTextResponse
from starlette.types import ASGIApp

_BEARER_PREFIX = "Bearer "


class BearerTokenMiddleware(BaseHTTPMiddleware):
    """Requires a valid `Authorization: Bearer <token>` header on every /mcp request."""

    def __init__(self, app: ASGIApp, *, token: str, protected_path_prefix: str = "/mcp") -> None:
        super().__init__(app)
        self._token = token
        self._protected_path_prefix = protected_path_prefix

    async def dispatch(self, request: Request, call_next):
        if not request.url.path.startswith(self._protected_path_prefix):
            return await call_next(request)

        if not self._token:
            return PlainTextResponse("Unauthorized", status_code=401)

        header = request.headers.get("Authorization", "")
        if not header.startswith(_BEARER_PREFIX):
            return PlainTextResponse("Unauthorized", status_code=401)

        presented = header[len(_BEARER_PREFIX):].strip()
        if not hmac.compare_digest(presented, self._token):
            return PlainTextResponse("Unauthorized", status_code=401)

        return await call_next(request)

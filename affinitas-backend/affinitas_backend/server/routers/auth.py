import logging
from uuid import uuid4

from fastapi import status
from fastapi.requests import Request
from fastapi.routing import APIRouter

from affinitas_backend.models.schemas.auth import UUIDResponse
from affinitas_backend.server.dependencies import XClientUUIDHeader
from affinitas_backend.server.limiter import limiter

router = APIRouter(prefix="/auth", tags=["auth"])


@router.post(
    "/uuid",
    response_model=UUIDResponse,
    summary="Generate or validate a client UUID",
    description="Returns a UUID for client identification. If the `X-Client-UUID` header is provided "
                "and is valid, it is returned. If invalid, a 400 status is returned. Generates and returns "
                " a new UUID if the header is missing.",
    status_code=status.HTTP_200_OK
)
@limiter.limit("100/minute")
async def auth(request: Request, x_client_uuid: XClientUUIDHeader = None):
    if x_client_uuid:
        return UUIDResponse(uuid=x_client_uuid)

    uuid = uuid4()
    logging.info(f"Created UUID: {uuid!r}")

    return UUIDResponse(uuid=uuid)

import logging
from typing import Annotated
from uuid import uuid4, UUID

from fastapi import HTTPException, Header
from fastapi.requests import Request
from fastapi.routing import APIRouter
from starlette import status

from affinitas_backend.models.schemas.auth import UUIDResponse
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
@limiter.limit("5/minute")
async def auth(request: Request, x_client_uuid: Annotated[str, Header()] = None):
    try:
        if x_client_uuid:
            uuid = UUID(x_client_uuid)
        else:
            uuid = uuid4()
            logging.info(f"Created UUID: {uuid!r}")
    except ValueError:
        raise HTTPException(status_code=status.HTTP_400_BAD_REQUEST,
                            detail=f"Invalid value in X-Client-UUID: {x_client_uuid!r}")

    return UUIDResponse(uuid=str(uuid))

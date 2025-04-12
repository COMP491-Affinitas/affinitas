import logging
from uuid import uuid4, UUID

from fastapi import HTTPException
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
async def auth(request: Request):
    uuid = request.headers.get("X-Client-UUID")

    try:
        if uuid:
            uuid = UUID(uuid)
        else:
            uuid = uuid4()
            logging.info(f"Created UUID: {uuid!r}")
    except ValueError:
        return HTTPException(status_code=400, detail=f"Invalid value in X-Client-UUID: {uuid}")

    return UUIDResponse(uuid=str(uuid))

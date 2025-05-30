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
    status_code=status.HTTP_200_OK,
    summary="Generate or validate a client UUID",
    description=(
            "Retrieves a UUID for client identification.\n\n"
            "**Behavior:**\n"
            "- If the `X-Client-UUID` header is provided and valid, returns it unchanged.\n"
            "- If the header is invalid, returns `400 Bad Request`.\n"
            "- If the header is missing, generates, logs, and returns a new UUID.\n\n"
            "**Rate Limit:** 100 requests per minute per client."
    ),
    responses={
        status.HTTP_200_OK: {
            "description": "UUID successfully generated or validated.",
            "content": {
                "application/json": {
                    "example": {"uuid": "c8c5d7a4-df9e-4b8a-8cf4-04844a56f5df"}
                }
            }
        },
        status.HTTP_400_BAD_REQUEST: {
            "description": "Invalid UUID provided in the X-Client-UUID header.",
            "content": {
                "application/json": {
                    "example": {"detail": "Invalid UUID format."}
                }
            }
        }
    }
)
@limiter.limit("100/minute")
async def auth(request: Request, x_client_uuid: XClientUUIDHeader = None):
    """
    Generate or validate a UUID for identifying the client.

    - Returns the provided `X-Client-UUID` if it is valid.
    - Generates a new UUID if no header is provided.
    """
    if x_client_uuid:
        return UUIDResponse(uuid=x_client_uuid)

    uuid = uuid4()
    logging.info(f"Created UUID: {uuid!r}")

    return UUIDResponse(uuid=uuid)

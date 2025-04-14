import logging
from uuid import UUID

from fastapi import HTTPException


def validate_uuid(uuid: str | None) -> UUID:
    try:
        if uuid:
            return UUID(uuid)
        else:
            raise HTTPException(status_code=400, detail="Missing X-Client-UUID")
    except ValueError:
        logging.info(f"Invalid value in X-Client-UUID: {uuid!r}")
        raise HTTPException(status_code=400, detail="Invalid value in X-Client-UUID")

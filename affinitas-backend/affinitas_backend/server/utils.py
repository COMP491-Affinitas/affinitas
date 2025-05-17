import logging

from fastapi import HTTPException, status


def throw_500(detail: str, *msgs: str):
    logging.error(detail)
    for msg in msgs:
        logging.error(msg)

    raise HTTPException(status_code=status.HTTP_500_INTERNAL_SERVER_ERROR, detail=detail)

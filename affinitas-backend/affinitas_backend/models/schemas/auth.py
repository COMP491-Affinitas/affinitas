from pydantic import BaseModel


class UUIDResponse(BaseModel):
    uuid: str

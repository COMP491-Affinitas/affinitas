from pydantic import BaseModel, UUID4


class UUIDResponse(BaseModel):
    uuid: UUID4

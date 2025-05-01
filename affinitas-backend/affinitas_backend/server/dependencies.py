from typing import Annotated

from fastapi import Header
from pydantic import UUID4

XClientUUIDHeader = Annotated[
    UUID4, Header(description="Unique identifier assigned to the client by the server. Uses UUID4 format.",
                  alias="X-Client-UUID")]

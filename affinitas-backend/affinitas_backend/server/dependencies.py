from typing import Annotated

from fastapi import Header

XClientUUIDHeader = Annotated[str, Header(description="Unique identifier assigned to the client by the server")]

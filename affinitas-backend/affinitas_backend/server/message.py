from typing import Optional

from pydantic import BaseModel


class Message(BaseModel):
    message: str
    error: Optional[str] = None
    id: str

    def __str__(self):
        if self.error:
            return f"Message[\n\tmessage={self.message!r},\n\terror={self.error!r},\n\tid={self.id!r}\n]"

        return f"Message[\n\tmessage={self.message!r},\n\tid={self.id!r}\n]"

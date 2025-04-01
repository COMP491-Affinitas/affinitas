from typing import Optional

from pydantic import BaseModel

class Message(BaseModel):
    message: str
    error: Optional[str] = None
    id: int

    def __str__(self):
        if self.error:
            return "Message[\n\tmessage={self.message},\n\t{self.error},\n\tid={self.id}\n]"

        return f"Message[\n\tmessage={self.message},\n\tid={self.id}\n]"
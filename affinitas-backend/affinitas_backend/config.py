from pydantic_settings import BaseSettings


class Config(BaseSettings):
    mongodb_uri: str
    mongodb_dbname: str

    openai_api_key: str
    openai_model_name: str = "gpt-4.1"

    langsmith_tracing: bool = True
    langsmith_endpoint: str
    langsmith_api_key: str
    langsmith_project: str

    env: str = "production"
    default_save_version: int
    langchain_max_tokens: int = 30000

    class Config:
        env_file = ".env"

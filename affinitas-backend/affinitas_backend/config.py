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
    default_save_version: int = 9
    langchain_max_tokens: int = 30000

    daily_ap_limit: int = 15

    log_level: str = "WARNING"

    class Config:
        env_file = ".env"

import pytest
from config import config
from client import HEAppEClient

@pytest.fixture(scope="session")
def heappe_url():
    return config.HEAPPE_URL

@pytest.fixture(scope="session")
def api_key():
    return config.HEAPPE_API_KEY

@pytest.fixture(scope="session")
def client(heappe_url, api_key):
    """
    Session-wide HEAppE HTTP client with X-Api-Key header.
    """
    api_client = HEAppEClient(base_url=heappe_url, api_key=api_key)
    yield api_client

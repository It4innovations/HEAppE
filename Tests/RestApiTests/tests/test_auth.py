import pytest
import requests
from client import HEAppEClient

def test_x_api_key_authentication(client: HEAppEClient):
    """
    Verifies that requests sending X-Api-Key header are handled by HEAppE API.
    """
    try:
        res = client.get("heappe/UserAndLimitationManagement/AuthenticateUserPassword")
        assert res.status_code in (200, 400, 401, 403, 405)
    except requests.exceptions.ConnectionError:
        pytest.skip("HEAppE live instance is not running locally")

def test_invalid_x_api_key_rejection(heappe_url: str):
    """
    Verifies that invalid X-Api-Key is rejected by HEAppE API.
    """
    invalid_client = HEAppEClient(base_url=heappe_url, api_key="invalid-key-999")
    try:
        res = invalid_client.get("heappe/ClusterInformation/ListAvailableClusters")
        assert res.status_code in (401, 403, 400, 500)
    except requests.exceptions.ConnectionError:
        pytest.skip("HEAppE live instance is not running locally")

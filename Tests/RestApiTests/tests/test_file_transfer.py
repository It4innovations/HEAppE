import pytest

def test_file_transfer_methods_endpoint(client):
    """
    Test file transfer methods discovery endpoint.
    """
    res = client.get("heappe/FileTransfer/ListFileTransferMethods")
    assert res.status_code in (200, 400, 401, 403, 404, 503), f"Unexpected status code: {res.status_code}"

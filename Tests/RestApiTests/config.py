import os
import subprocess
from pathlib import Path

def discover_dynamic_heappe_url() -> str:
    """
    Dynamically discovers the host port bound to container port 80 of ci_heappe_core if HEAPPE_URL is not set.
    """
    if "HEAPPE_URL" in os.environ:
        return os.environ["HEAPPE_URL"].rstrip("/")
    
    # Attempt to query Docker for dynamically assigned host port of ci_heappe_core (port 80)
    try:
        res = subprocess.run(
            ["docker", "port", "ci_heappe_core", "80"],
            capture_output=True, text=True, timeout=2
        )
        if res.returncode == 0 and res.stdout.strip():
            # Format: 0.0.0.0:54321 or :::54321
            port = res.stdout.strip().split(":")[-1]
            return f"http://localhost:{port}"
    except Exception:
        pass
    
    # Fallback default
    return "http://localhost:5001"

class Config:
    # Base URL of HEAppE REST API (Dynamically discovered or configured)
    HEAPPE_URL: str = discover_dynamic_heappe_url()
    
    # X-Api-Key Authentication
    HEAPPE_API_KEY: str = os.getenv("HEAPPE_API_KEY", "test-api-key")
    
    # User Credentials for Legacy Session Code Auth (Fallback)
    USERNAME: str = os.getenv("USERNAME", "default_username")
    PASSWORD: str = os.getenv("PASSWORD", "default_password")
    
    # Slurm Cluster & Project Configuration
    CLUSTER_NAME: str = os.getenv("CLUSTER_NAME", "SlurmTestCluster")
    PROJECT_NAME: str = os.getenv("PROJECT_NAME", "TestProject")
    COMMAND_TEMPLATE_NAME: str = os.getenv("COMMAND_TEMPLATE_NAME", "SlurmTestTemplate")
    ADAPTOR_USER_GROUP_NAME: str = os.getenv("ADAPTOR_USER_GROUP_NAME", "default_group")
    NODE_TYPE_NAME: str = os.getenv("NODE_TYPE_NAME", "default_node")
    
    # Path to static Swagger Spec (Fallback if live swagger.json is unreachable)
    STATIC_SWAGGER_PATH: Path = Path(__file__).parent.parent.parent / "SwaggerReference" / "RestApi-py4heappe.json"
    
    # Live Swagger URL for py4heappe
    @property
    def LIVE_SWAGGER_URL(self) -> str:
        return f"{self.HEAPPE_URL}/swagger/py4heappe/swagger.json"
    
    def get_swagger_location(self) -> str:
        """
        Returns live Swagger URL if reachable, otherwise falls back to static JSON path.
        """
        import requests
        try:
            res = requests.get(self.LIVE_SWAGGER_URL, timeout=3)
            if res.status_code == 200:
                return self.LIVE_SWAGGER_URL
        except Exception:
            pass
        return str(self.STATIC_SWAGGER_PATH)

config = Config()

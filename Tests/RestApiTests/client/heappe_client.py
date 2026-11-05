import time
import logging
import requests
from typing import Any, Dict, Optional, Set, Tuple
from config import config

# Configure logger for REST API tests
logger = logging.getLogger("heappe.restapi")
if not logger.handlers:
    handler = logging.StreamHandler()
    formatter = logging.Formatter("[%(asctime)s] [%(levelname)s] %(message)s", "%Y-%m-%d %H:%M:%S")
    handler.setFormatter(formatter)
    logger.addHandler(handler)
    logger.setLevel(logging.INFO)

class HEAppEClient:
    """
    Python HTTP Client for HEAppE REST API supporting authentication,
    tracking invoked endpoints for coverage verification, and full lifecycle execution.
    """
    def __init__(self, base_url: Optional[str] = None, api_key: Optional[str] = None):
        from config import discover_dynamic_heappe_url
        self.base_url = (base_url or discover_dynamic_heappe_url()).rstrip("/")
        self.session = requests.Session()
        self.session.headers.update({
            "Content-Type": "application/json",
            "Accept": "application/json",
            "X-Api-Key": api_key or config.HEAPPE_API_KEY
        })
        self.session_code: Optional[str] = None
        self.executed_routes: Set[Tuple[str, str]] = set()

    def _record_route(self, method: str, endpoint: str):
        normalized_endpoint = "/" + endpoint.lstrip("/").split("?")[0]
        self.executed_routes.add((method.upper(), normalized_endpoint))

    def request(self, method: str, endpoint: str, **kwargs) -> requests.Response:
        url = f"{self.base_url}/{endpoint.lstrip('/')}"
        self._record_route(method, endpoint)
        logger.info(f"HTTP Request: {method.upper()} {url}")
        try:
            response = self.session.request(method=method, url=url, **kwargs)
            logger.info(f"HTTP Response Status: {response.status_code} [{len(response.content)} bytes]")
            try:
                body = response.json()
                logger.info(f"Response Payload: {body}")
            except Exception:
                logger.info(f"Response Payload: {response.text[:500]}")
            return response
        except Exception as e:
            logger.error(f"HTTP Request Failed ({method.upper()} {url}): {e}")
            raise

    def get(self, endpoint: str, params: Optional[Dict[str, Any]] = None, **kwargs) -> requests.Response:
        return self.request("GET", endpoint, params=params, **kwargs)

    def post(self, endpoint: str, json: Optional[Dict[str, Any]] = None, **kwargs) -> requests.Response:
        return self.request("POST", endpoint, json=json, **kwargs)

    def put(self, endpoint: str, json: Optional[Dict[str, Any]] = None, **kwargs) -> requests.Response:
        return self.request("PUT", endpoint, json=json, **kwargs)

    def delete(self, endpoint: str, params: Optional[Dict[str, Any]] = None, **kwargs) -> requests.Response:
        return self.request("DELETE", endpoint, params=params, **kwargs)

    # --- Domain Helper Methods ---

    def authenticate_password(self, username: Optional[str] = None, password: Optional[str] = None) -> str:
        """Authenticate using username and password to receive session code."""
        payload = {
            "credentials": {
                "Username": username or config.USERNAME,
                "Password": password or config.PASSWORD
            }
        }
        res = self.post("heappe/UserAndLimitationManagement/AuthenticateUserPassword", json=payload)
        res.raise_for_status()
        self.session_code = res.json()
        return self.session_code

    def list_available_clusters(self) -> list:
        res = self.get("heappe/ClusterInformation/ListAvailableClusters", params={"SessionCode": self.session_code} if self.session_code else None)
        res.raise_for_status()
        return res.json()

    def create_cluster(self, name: str, master_node: str = "slurmctl", scheduler_type: int = 4) -> Dict[str, Any]:
        payload = {
            "SessionCode": self.session_code,
            "Name": name,
            "Description": "Automated Test Cluster",
            "MasterNodeName": master_node,
            "SchedulerType": scheduler_type,  # 4 = Slurm
            "ConnectionProtocol": 2,
            "TimeZone": "CET",
            "UpdateJobStateByServiceAccount": True,
            "DomainName": "domain.com"
        }
        res = self.post("heappe/Management/Cluster", json=payload)
        res.raise_for_status()
        return res.json()

    def create_project(self, name: str, accounting_string: Optional[str] = None, is_one_to_one: bool = True) -> Dict[str, Any]:
        acc_str = accounting_string or f"acc_{int(time.time_ns())}"
        payload = {
            "SessionCode": self.session_code,
            "Name": name,
            "Description": "Test Project",
            "AccountingString": acc_str,
            "UsageType": 2,
            "PIEmail": "pi@domain.com",
            "IsOneToOneMapping": is_one_to_one,
            "StartDate": "2020-01-01T00:00:00Z",
            "EndDate": "2030-01-01T00:00:00Z"
        }
        res = self.post("heappe/Management/Project", json=payload)
        res.raise_for_status()
        return res.json()

    def assign_project_to_cluster(self, project_id: int, cluster_id: int, scratch_path: str = "/tmp/scratch", project_path: str = "/tmp/project") -> Dict[str, Any]:
        payload = {
            "SessionCode": self.session_code,
            "ProjectId": project_id,
            "ClusterId": cluster_id,
            "ScratchStoragePath": scratch_path,
            "ProjectStoragePath": project_path
        }
        res = self.post("heappe/Management/ProjectAssignmentToCluster", json=payload)
        res.raise_for_status()
        return res.json()

    def create_file_transfer_method(self, cluster_id: int, hostname: str = "slurmctl", port: int = 22, protocol: int = 1) -> Dict[str, Any]:
        payload = {
            "SessionCode": self.session_code,
            "ServerHostname": hostname,
            "Protocol": protocol,
            "ClusterId": cluster_id,
            "Port": port
        }
        res = self.post("heappe/Management/FileTransferMethod", json=payload)
        res.raise_for_status()
        return res.json()

    def create_credential(self, project_id: int, username: str = "xenon", password: str = "javaclient", auth_type: int = 1) -> Dict[str, Any]:
        payload = {
            "SessionCode": self.session_code,
            "ProjectId": project_id,
            "Username": username,
            "Password": password,
            "AuthType": auth_type
        }
        res = self.post("heappe/Credentials/CreateCredential", json=payload)
        res.raise_for_status()
        return res.json()

    def create_node_type(self, cluster_id: int, file_transfer_method_id: int, name: str = "slurm_node", nodes: int = 10, cores: int = 4) -> Dict[str, Any]:
        payload = {
            "SessionCode": self.session_code,
            "Name": name,
            "Description": "Slurm Node Type",
            "NumberOfNodes": nodes,
            "CoresPerNode": cores,
            "ClusterId": cluster_id,
            "FileTransferMethodId": file_transfer_method_id
        }
        res = self.post("heappe/Management/ClusterNodeType", json=payload)
        res.raise_for_status()
        return res.json()

    def create_command_template(self, project_id: int, node_type_id: int, name: str = "SlurmTemplate", executable: str = "/bin/true") -> Dict[str, Any]:
        payload = {
            "SessionCode": self.session_code,
            "Name": name,
            "Description": "Slurm Command Template",
            "ExecutableFile": executable,
            "ClusterNodeTypeId": node_type_id,
            "ProjectId": project_id,
            "TemplateParameters": []
        }
        res = self.post("heappe/Management/CommandTemplate", json=payload)
        res.raise_for_status()
        return res.json()

    def create_job(self, project_id: int, cluster_id: int, file_transfer_method_id: int, command_template_id: int, node_type_id: int, job_name: str = "E2E_Slurm_Job") -> Dict[str, Any]:
        payload = {
            "SessionCode": self.session_code,
            "JobSpecification": {
                "Name": job_name,
                "ProjectId": project_id,
                "ClusterId": cluster_id,
                "FileTransferMethodId": file_transfer_method_id,
                "Tasks": [{
                    "Name": "Task1",
                    "CommandTemplateId": command_template_id,
                    "ClusterNodeTypeId": node_type_id,
                    "MinCores": 1,
                    "MaxCores": 1,
                    "WalltimeLimit": 3600,
                    "StandardOutputFile": "stdout.txt",
                    "StandardErrorFile": "stderr.txt",
                    "ProgressFile": "progress.txt",
                    "LogFile": "log.txt"
                }]
            }
        }
        res = self.post("heappe/JobManagement/CreateJob", json=payload)
        res.raise_for_status()
        return res.json()

    def submit_job(self, job_id: int) -> Dict[str, Any]:
        payload = {
            "SessionCode": self.session_code,
            "CreatedJobInfoId": job_id
        }
        res = self.put("heappe/JobManagement/SubmitJob", json=payload)
        res.raise_for_status()
        return res.json()

    def get_job_state(self, job_id: int) -> int:
        params = {"submittedJobInfoId": job_id}
        if self.session_code:
            params["sessionCode"] = self.session_code
        res = self.get("heappe/JobManagement/CurrentInfoForJob", params=params)
        res.raise_for_status()
        return res.json().get("State", 0)

    def delete_job(self, job_id: int, archive_logs: bool = False) -> bool:
        payload = {
            "SessionCode": self.session_code,
            "SubmittedJobInfoId": job_id,
            "ArchiveLogs": archive_logs
        }
        res = self.delete("heappe/JobManagement/DeleteJob", json=payload)
        return res.status_code == 200

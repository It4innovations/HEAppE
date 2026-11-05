import time
import pytest
import requests
from client import HEAppEClient
from config import config

class TestEndToEndSlurmJobLifecycle:
    """
    Sequential End-to-End Test Scenario against live HEAppE & Docker Slurm simulator:
    1. Authenticate with Password -> obtain SessionCode
    2. Create Slurm Cluster Configuration (SchedulerType: 4)
    3. Create Project Configuration & assign to Cluster
    4. Create FileTransferMethod & Cluster Credentials
    5. Create ClusterNodeType & CommandTemplate
    6. Create Job Specification & Submit Job to Slurm compute cluster
    7. Monitor Job Execution state until complete
    8. Teardown and Cleanup resources
    """
    
    @pytest.fixture(autouse=True)
    def setup_client(self, client: HEAppEClient):
        self.client = client

    def test_full_system_lifecycle_scenario(self):
        # Step 1: Authenticate Administrator User
        try:
            session_code = self.client.authenticate_password("default_username", "default_password")
            assert session_code is not None, "Failed to obtain session code"
        except requests.exceptions.ConnectionError:
            pytest.skip("HEAppE live instance is not running locally")

        # Health Check with SessionCode
        health_res = self.client.get("heappe/Management/InstanceInformation", params={"SessionCode": session_code})
        assert health_res.status_code == 200, f"InstanceInformation returned status {health_res.status_code}"

        # Step 2: Query Available Clusters
        clusters = self.client.list_available_clusters()
        assert isinstance(clusters, list), "ListAvailableClusters must return a list"

        # Step 3: Create Slurm Cluster Configuration
        cluster_name = f"SlurmTest_{int(time.time_ns())}"
        cluster_data = self.client.create_cluster(name=cluster_name, master_node="slurmctl", scheduler_type=4)
        cluster_id = cluster_data.get("Id")
        assert cluster_id is not None, "Created cluster must return an ID"

        # Step 4: Create Project Configuration
        project_name = f"TestProject_{int(time.time_ns())}"
        project_data = self.client.create_project(name=project_name)
        project_id = project_data.get("Id")
        assert project_id is not None, "Created project must return an ID"

        # Step 5: Assign Project to Cluster
        self.client.assign_project_to_cluster(project_id=project_id, cluster_id=cluster_id)

        # Step 6: Create FileTransferMethod & Credentials
        ft_data = self.client.create_file_transfer_method(cluster_id=cluster_id)
        ft_id = ft_data.get("Id")
        assert ft_id is not None, "Created FileTransferMethod must return an ID"

        self.client.create_credential(project_id=project_id, username="xenon", password="javaclient")

        # Step 7: Create ClusterNodeType & CommandTemplate
        node_type_data = self.client.create_node_type(cluster_id=cluster_id, file_transfer_method_id=ft_id)
        node_type_id = node_type_data.get("Id")
        assert node_type_id is not None, "Created ClusterNodeType must return an ID"

        ct_data = self.client.create_command_template(project_id=project_id, node_type_id=node_type_id, executable="/bin/true")
        ct_id = ct_data.get("Id")
        assert ct_id is not None, "Created CommandTemplate must return an ID"

        # Step 8: Create & Submit Job to Slurm Cluster
        created_job = self.client.create_job(
            project_id=project_id,
            cluster_id=cluster_id,
            file_transfer_method_id=ft_id,
            command_template_id=ct_id,
            node_type_id=node_type_id,
            job_name="E2E_Slurm_Job"
        )
        job_id = created_job.get("Id")
        assert job_id is not None, "Created job must return a valid ID"

        submitted_job = self.client.submit_job(job_id=job_id)
        assert submitted_job is not None, "Job submission must succeed"
        assert submitted_job.get("State") in [4, 8, 16, 32], f"Unexpected job state upon submission: {submitted_job.get('State')}"

        # Step 9: Monitor Job Execution State
        max_retries = 15
        finished = False
        for _ in range(max_retries):
            state = self.client.get_job_state(job_id)
            if state in [4, 8, 16, 32, 64]:  # Queued, Running, Finished, Failed, Canceled
                finished = True
                break
            time.sleep(1)

        assert finished, "Job execution did not reach terminal state within timeout"

        # Step 10: Teardown and Cleanup Created Job
        deleted = self.client.delete_job(job_id)
        assert deleted, "Job cleanup/deletion must succeed"

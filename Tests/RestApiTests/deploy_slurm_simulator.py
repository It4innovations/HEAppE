#!/usr/bin/env python3
import argparse
import subprocess
import time
import uuid
import requests

def build_and_run_docker(container_name: str, port: int, slurm_user: str, acct_name: str) -> int:
    print(f"Deploying Slurm Docker container '{container_name}' (Port mapping: {port}:22)...")
    # Remove existing container if it exists
    subprocess.run(["docker", "rm", "-f", container_name], stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
    
    port_arg = f"{port}:22" if port != 0 else "0:22"
    cmd = [
        "docker", "run", "-d",
        "--name", container_name,
        "-p", port_arg,
        "xenonmiddleware/slurm:latest"
    ]
    subprocess.run(cmd, check=True)
    
    # Query assigned host port if dynamic port 0 was requested
    if port == 0:
        res = subprocess.run(["docker", "port", container_name, "22"], capture_output=True, text=True, check=True)
        assigned_port = int(res.stdout.strip().split(":")[-1])
        print(f"Slurm container '{container_name}' started on dynamic host SSH port {assigned_port}.")
        return assigned_port
    else:
        print(f"Slurm container '{container_name}' started on host SSH port {port}.")
        return port

def configure_heappe(heappe_url: str, user: str, password: str, port: int, uid: str, slurm_user: str, acct_name: str):
    print(f"Configuring HEAppE at {heappe_url} for Slurm cluster...")
    base_url = heappe_url.rstrip("/")
    session = requests.Session()
    
    # 1. Authenticate
    auth_payload = {
        "credentials": {
            "Username": user,
            "Password": password
        }
    }
    res = session.post(f"{base_url}/heappe/UserAndLimitationManagement/AuthenticateUserPassword", json=auth_payload)
    res.raise_for_status()
    session_code = res.json()
    print("Authenticated successfully with HEAppE API.")
    
    # 2. Create Cluster
    cluster_payload = {
        "SessionCode": session_code,
        "Name": f"SlurmCluster_{uid}",
        "Description": "Automated Slurm Simulator Cluster",
        "MasterNodeName": "localhost",
        "SchedulerType": 2,
        "ConnectionProtocol": 2,
        "TimeZone": "CET",
        "UpdateJobStateByServiceAccount": True,
        "DomainName": "domain.com"
    }
    res = session.post(f"{base_url}/heappe/Management/Cluster", json=cluster_payload)
    res.raise_for_status()
    cluster_id = res.json().get("Id")
    
    # 3. Create Project
    project_payload = {
        "SessionCode": session_code,
        "Name": f"SlurmProject_{uid}",
        "Description": "Automated Slurm Test Project",
        "AccountingString": acct_name
    }
    res = session.post(f"{base_url}/heappe/Management/Project", json=project_payload)
    res.raise_for_status()
    project_id = res.json().get("Id")
    
    print(f"Successfully configured Slurm Cluster (ID: {cluster_id}) and Project (ID: {project_id}).")

if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="Deploy and configure a test Slurm cluster simulator for HEAppE.")
    
    parser.add_argument("--heappe-url", default="http://localhost:5001", help="URL of the HEAppE instance API")
    parser.add_argument("--heappe-user", default="default_username", help="HEAppE API Administrator login")
    parser.add_argument("--heappe-pass", default="default_password", help="HEAppE API Administrator password")
    
    parser.add_argument("--port", type=int, default=0, help="Host SSH port to map (default: 0 for random free dynamic port)")
    parser.add_argument("--container-name", default="slurm-simulator-local", help="Docker container name (default: slurm-simulator-local)")
    
    parser.add_argument("--skip-docker", action="store_true", help="Skip Docker build and execution step")
    parser.add_argument("--skip-heappe", action="store_true", help="Skip HEAppE cluster registration & configuration step")
    
    args = parser.parse_args()
    uid = str(uuid.uuid4())[:8]
    acct_name = f"a{uid}"
    slurm_user = f"u{uid}"
    
    assigned_ssh_port = args.port
    if not args.skip_docker:
        assigned_ssh_port = build_and_run_docker(args.container_name, args.port, slurm_user, acct_name)
    else:
        acct_name = "atest"
        slurm_user = "heappeslurm"
        print("Skipping Docker deployment step as requested.")
        
    if not args.skip_heappe:
        configure_heappe(args.heappe_url, args.heappe_user, args.heappe_pass, assigned_ssh_port, uid, slurm_user, acct_name)
    else:
        print("Skipping HEAppE configuration step as requested.")
        
    print("\n=== ALL DONE ===")

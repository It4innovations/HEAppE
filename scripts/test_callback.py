import json
import urllib.request
import sys

base_url = "https://p01-183.cloud.it4i.cz"

def post(path, data):
    req = urllib.request.Request(
        f"{base_url}{path}",
        data=json.dumps(data).encode("utf-8"),
        headers={"Content-Type": "application/json"}
    )
    try:
        with urllib.request.urlopen(req) as res:
            return json.loads(res.read().decode("utf-8"))
    except Exception as e:
        print(f"HTTP Request to {path} failed: {e}")
        if hasattr(e, "read"):
            print(e.read().decode("utf-8"))
        sys.exit(1)

print("=== HEAppE Callback End-to-End Test ===")
username = input("Username: ")
password = input("Password: ")
project_id = int(input("Project ID (integer): "))
cluster_id = int(input("Cluster ID (integer): "))
template_id = int(input("Command Template ID (integer): "))

# 1. Authenticate
print("\n[1/3] Authenticating...")
session_code = post("/heappe/UserAndLimitationManagement/AuthenticateUserPassword", {
    "Username": username,
    "Password": password
})
print(f"Success! SessionCode: {session_code}")

# 2. Create Job
print("\n[2/3] Creating job...")
job_spec = {
    "SessionCode": session_code,
    "JobSpecification": {
        "Name": "CallbackTestJob",
        "ProjectId": project_id,
        "ClusterId": cluster_id,
        "Tasks": [
            {
                "Name": "TestTask",
                "MinCores": 1,
                "MaxCores": 1,
                "WalltimeLimit": 60,
                "CommandTemplateId": template_id,
                "CommandParameterValues": []
            }
        ]
    }
}
job_info = post("/heappe/JobManagement/CreateJob", job_spec)
job_id = job_info["Id"]
print(f"Success! Job created with ID: {job_id}")

# 3. Submit Job
print("\n[3/3] Submitting job...")
submit_info = post("/heappe/JobManagement/SubmitJob", {
    "SessionCode": session_code,
    "CreatedJobInfoId": job_id
})
print(f"Success! Job submitted to cluster.")

print("\n=== Monitoring status (Press Ctrl+C to exit) ===")
import time
try:
    while True:
        status_info = post("/heappe/JobManagement/CurrentInfoForJob", {
            "SessionCode": session_code,
            "SubmittedJobInfoId": job_id
        })
        state = status_info.get("State", "Unknown")
        print(f"[{time.strftime('%H:%M:%S')}] Job State: {state}")
        if state in [4, 5, "Completed", "Failed"]: # Finished/Failed states
            break
        time.sleep(5)
except KeyboardInterrupt:
    print("\nMonitoring stopped by user.")

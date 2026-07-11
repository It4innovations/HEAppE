#!/bin/bash
# task_wrapper.sh <callback_url> <scheduler_type>

CALLBACK_URL="$1"
SCHEDULER_TYPE="$2"

# Save original stdout (1) and stderr (2) to file descriptors 3 and 4
exec 3>&1
exec 4>&2

# Redirect the wrapper script's own stdout and stderr to a callback log file
# This prevents callback logs and curl/python output from polluting the user's stdout/stderr files
exec 1>> .heappe_callback.log
exec 2>> .heappe_callback.log

# 1. Load the token from the protected file and DELETE it immediately
if [ -f .callback_token ]; then
    CALLBACK_TOKEN=$(cat .callback_token)
    rm -f .callback_token # Permanently remove the token from disk
else
    echo "Error: .callback_token file not found!" >&2
    exit 1
fi

# Helper function to get the current job status directly from the scheduler (Slurm/PBS/HQ)
get_scheduler_job_info() {
    if [ "$SCHEDULER_TYPE" = "slurm" ]; then
        scontrol show JobId="$SLURM_JOB_ID" -o 2>/dev/null
    elif [ "$SCHEDULER_TYPE" = "pbs" ]; then
        qstat -f -x "$PBS_JOBID" 2>/dev/null
    elif [ "$SCHEDULER_TYPE" = "hq" ]; then
        hq job info "$HQ_JOB_ID" --output-mode=json 2>/dev/null
    fi
}

# Helper function to send an HTTP POST request with a JSON body.
# Sequentially detects and tries: curl, wget, python3, python.
send_http_post() {
    local url="$1"
    local payload="$2"
    
    # 1. Try using curl
    if command -v curl >/dev/null 2>&1; then
        curl -s -X POST -H "Content-Type: application/json" -d "$payload" "$url"
        return $?
        
    # 2. Try using wget
    elif command -v wget >/dev/null 2>&1; then
        wget -qO- --post-data="$payload" --header="Content-Type: application/json" "$url"
        return $?
        
    # 3. Try using python3
    elif command -v python3 >/dev/null 2>&1; then
        python3 -c '
import sys, urllib.request
req = urllib.request.Request(sys.argv[1], data=sys.argv[2].encode("utf-8"), headers={"Content-Type": "application/json"})
try:
    with urllib.request.urlopen(req, timeout=10) as r: pass
except Exception as e:
    sys.stderr.write(f"Failed: {e}\n")
    sys.exit(1)
' "$url" "$payload"
        return $?
        
    # 4. Try using python (Python 2 or Python 3 fallback)
    elif command -v python >/dev/null 2>&1; then
        python -c '
import sys
try:
    import urllib.request as urllib_req
except ImportError:
    import urllib2 as urllib_req
req = urllib_req.Request(sys.argv[1], data=sys.argv[2].encode("utf-8"), headers={"Content-Type": "application/json"})
try:
    urllib_req.urlopen(req)
except Exception as e:
    sys.stderr.write("Failed: {}\n".format(e))
    sys.exit(1)
' "$url" "$payload"
        return $?
        
    else
        echo "Error: No HTTP client found on compute node (curl, wget, python not available)!" >&2
        return 1
    fi
}

# Helper function to format the JSON payload and send the callback
send_callback() {
    local state_override="$1"
    local exit_code_override="$2"
    
    local raw_info
    raw_info=$(get_scheduler_job_info)
    
    # If the scheduler query fails (e.g. job is already disappearing), construct a fallback status info
    if [ -z "$raw_info" ]; then
        if [ "$SCHEDULER_TYPE" = "slurm" ]; then
            raw_info="JobId=$SLURM_JOB_ID JobState=${state_override:-RUNNING} ExitCode=${exit_code_override:-0:0}"
        elif [ "$SCHEDULER_TYPE" = "pbs" ]; then
            raw_info="<record><Job_Id>$PBS_JOBID</Job_Id><job_state>${state_override:-R}</job_state><exit_status>${exit_code_override:-0}</exit_status></record>"
        elif [ "$SCHEDULER_TYPE" = "hq" ]; then
            raw_info="JobId=$HQ_JOB_ID JobState=${state_override:-RUNNING} ExitCode=${exit_code_override:-0}"
        fi
    else
        # If we have the live scheduler status, dynamically override the state inside the output
        # to match how the task finished, since the scheduler still sees the wrapper process running.
        if [ -n "$state_override" ]; then
            if [ "$SCHEDULER_TYPE" = "slurm" ]; then
                # Override Slurm state (JobState=...) and exit code (ExitCode=...)
                raw_info=$(echo "$raw_info" | sed -E "s/JobState=[^ ]*/JobState=$state_override/g")
                raw_info=$(echo "$raw_info" | sed -E "s/ExitCode=[0-9:]*/ExitCode=$exit_code_override:0/g")
            elif [ "$SCHEDULER_TYPE" = "pbs" ]; then
                local pbs_state="R"
                if [ "$state_override" = "COMPLETED" ] || [ "$state_override" = "FAILED" ]; then
                    pbs_state="F"
                fi
                # Override PBS state (<job_state>...</job_state>) and exit status (<Exit_status>...</Exit_status>)
                raw_info=$(echo "$raw_info" | sed -E "s|<job_state>[^<]*</job_state>|<job_state>$pbs_state</job_state>|g")
                raw_info=$(echo "$raw_info" | sed -E "s|<Exit_status>[^<]*</Exit_status>|<Exit_status>$exit_code_override</Exit_status>|g")
            elif [ "$SCHEDULER_TYPE" = "hq" ]; then
                local hq_state="Running"
                if [ "$state_override" = "COMPLETED" ]; then
                    hq_state="Finished"
                    raw_info=$(echo "$raw_info" | sed -E 's/"finished": *[0-9]+/"finished": 1/g' | sed -E 's/"running": *[0-9]+/"running": 0/g')
                elif [ "$state_override" = "FAILED" ]; then
                    hq_state="Failed"
                    raw_info=$(echo "$raw_info" | sed -E 's/"failed": *[0-9]+/"failed": 1/g' | sed -E 's/"running": *[0-9]+/"running": 0/g')
                fi
                raw_info=$(echo "$raw_info" | sed -E "s/\"state\": *\"[^\"]*\"/\"state\": \"$hq_state\"/g")
            fi
        fi
    fi

    # Escape quotes and backslashes safely for the JSON payload
    local escaped_info
    escaped_info=$(echo "$raw_info" | sed 's/\\/\\\\/g' | sed 's/"/\\"/g' | sed 's/$/\\n/' | tr -d '\n')
    
    # Retrieve the job ID from the scheduler variables
    local job_id=""
    if [ "$SCHEDULER_TYPE" = "slurm" ]; then
        job_id="$SLURM_JOB_ID"
    elif [ "$SCHEDULER_TYPE" = "pbs" ]; then
        job_id="$PBS_JOBID"
    else
        job_id="$HQ_JOB_ID"
    fi

    local payload
    payload="{\"task_id\": \"$job_id\", \"token\": \"$CALLBACK_TOKEN\", \"raw_response\": \"$escaped_info\"}"

    send_callback_to_all_endpoints "$payload"
}

# Send the callback to all configured endpoints (comma-separated URL support)
send_callback_to_all_endpoints() {
    local payload="$1"
    
    IFS=',' read -ra ADDR <<< "$CALLBACK_URL"
    for url in "${ADDR[@]}"; do
        if [ -n "$url" ]; then
            send_http_post "$url" "$payload"
        fi
    done
}

# Trap handler to intercept cancellation (SIGTERM/SIGINT) or timeouts (SIGXCPU) from the scheduler
handle_shutdown() {
    echo "Signal received, job is being terminated by the scheduler." >&2
    # Exit status 143 represents termination by signal 15 (SIGTERM)
    send_callback "FAILED" "143"
    exit 143
}
# Register trap handler for scheduler signals
trap 'handle_shutdown' SIGTERM SIGINT SIGXCPU

# A. Send initial callback (State: RUNNING)
send_callback "RUNNING" "0"

# B. Execute the user's task script using the saved descriptors 3 and 4
./heappe_user_task.sh >&3 2>&4
USER_EXIT_CODE=$?

# C. Send final callback based on the exit code
if [ $USER_EXIT_CODE -eq 0 ]; then
    send_callback "COMPLETED" "0"
else
    send_callback "FAILED" "$USER_EXIT_CODE"
fi

exit $USER_EXIT_CODE

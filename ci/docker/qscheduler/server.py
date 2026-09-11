#!/usr/bin/env python3
"""
Lightweight high-fidelity QScheduler service conforming strictly to
https://github.com/It4innovations/qscheduler API.
Includes the 'test' backend with instant / timed state transitions.
"""

from http.server import HTTPServer, BaseHTTPRequestHandler
import json
import urllib.parse
import time
import threading
import sys

PORT = 4300

# State
projects = {}
sessions = {}
tasks = {}
machines = {
    "TestMachine": {
        "backend": "test",
        "arch": {"qubits": 5, "topology": "star", "basis_gates": ["cz", "rz", "rx"]},
        "calibration": {"t1_us": 45.2, "t2_us": 32.1, "readout_error": 0.015}
    },
    "TestMachine2": {
        "backend": "test",
        "arch": {"qubits": 20, "topology": "grid", "basis_gates": ["cz", "rz", "rx"]},
        "calibration": {"t1_us": 55.0, "t2_us": 40.0, "readout_error": 0.010}
    },
    "SimulatorMachine": {
        "backend": "test",
        "arch": {"qubits": 32, "topology": "all-to-all"},
        "calibration": {"fidelity": 0.999}
    }
}

lock = threading.Lock()
task_counter = 100
session_counter = 50

class QSchedulerHandler(BaseHTTPRequestHandler):
    def _send_json(self, status, data):
        self.send_response(status)
        self.send_header('Content-Type', 'application/json')
        self.end_headers()
        self.wfile.write(json.dumps(data).encode('utf-8'))

    def _send_text(self, status, text, content_type='text/plain'):
        self.send_response(status)
        self.send_header('Content-Type', content_type)
        self.end_headers()
        self.wfile.write(text.encode('utf-8'))

    def do_GET(self):
        parsed = urllib.parse.urlparse(self.path)
        path = parsed.path.rstrip('/')

        if path == '/version':
            self._send_text(200, 'qscheduler v1.0.0 (ci-mock)')
            return

        if path == '/health':
            self._send_json(200, {"status": "ok"})
            return

        if path == '/projects':
            with lock:
                self._send_json(200, list(projects.values()))
            return

        if path.startswith('/projects/'):
            name = path[len('/projects/'):]
            with lock:
                if name in projects:
                    self._send_json(200, projects[name])
                else:
                    self._send_json(404, {"error": "project not found"})
            return

        if path.startswith('/sessions/'):
            try:
                sid = int(path[len('/sessions/'):])
            except ValueError:
                self._send_json(400, {"error": "invalid session id"})
                return
            with lock:
                if sid in sessions:
                    self._send_json(200, sessions[sid])
                else:
                    self._send_json(404, {"error": "session not found"})
            return

        if path.startswith('/tasks/'):
            sub = path[len('/tasks/'):].split('/')
            try:
                tid = int(sub[0])
            except ValueError:
                self._send_json(400, {"error": "invalid task id"})
                return

            with lock:
                if tid not in tasks:
                    self._send_json(404, {"error": "task not found"})
                    return
                task = tasks[tid]

            if len(sub) == 1:
                # GET /tasks/{id}
                self._send_json(200, task)
            elif len(sub) == 2 and sub[1] == 'result':
                # GET /tasks/{id}/result
                res = task.get("result", '{"result": "42", "outcome": {"type": "Ok"}}')
                self._send_text(200, res, 'application/json')
            elif len(sub) == 3 and sub[1] == 'artifacts':
                # GET /tasks/{id}/artifacts/{name}
                art_name = sub[2]
                artifacts = task.get("artifacts", {"measurements": "00: 50, 11: 50"})
                if art_name in artifacts:
                    self._send_text(200, str(artifacts[art_name]), 'application/octet-stream')
                else:
                    self._send_text(200, f"mock-artifact-content-for-{art_name}", 'application/octet-stream')
            else:
                self._send_json(404, {"error": "not found"})
            return

        # GET /machine/{machine}/arch
        if path.startswith('/machine/') and path.endswith('/arch'):
            mname = path.split('/')[2]
            if mname in machines:
                self._send_json(200, machines[mname]["arch"])
            else:
                self._send_json(404, {"error": "machine not found"})
            return

        # GET /machine/{machine}/calibration/{calibration}/{endpoint}
        if path.startswith('/machine/') and '/calibration/' in path:
            mname = path.split('/')[2]
            if mname in machines:
                self._send_json(200, machines[mname]["calibration"])
            else:
                self._send_json(404, {"error": "machine not found"})
            return

        self._send_json(404, {"error": "not found"})

    def do_POST(self):
        parsed = urllib.parse.urlparse(self.path)
        path = parsed.path.rstrip('/')
        query = urllib.parse.parse_qs(parsed.query)

        content_length = int(self.headers.get('Content-Length', 0))
        body = self.rfile.read(content_length)

        if path == '/projects':
            try:
                data = json.loads(body.decode('utf-8'))
            except Exception:
                data = {}
            name = data.get("name")
            if not name:
                self._send_json(400, {"error": "name required"})
                return
            with lock:
                if name in projects:
                    self._send_text(409, "Project already exists", "text/plain")
                    return
                projects[name] = {
                    "name": name,
                    "consumed_ms": 0,
                    "limit_ms": data.get("limit_ms", 3600000),
                    "active": data.get("active", True)
                }
            self._send_text(201, "Created")
            return

        if path == '/sessions':
            machine = query.get("machine", [None])[0]
            project = query.get("project", [None])[0]
            time_limit = int(query.get("time_limit_ms", [3600000])[0])

            global session_counter
            with lock:
                session_counter += 1
                sid = session_counter
                sessions[sid] = {
                    "id": sid,
                    "state": "open",
                    "machine": machine or "TestMachine",
                    "project": project or "default",
                    "time_limit_ms": time_limit,
                    "created_at": time.strftime("%Y-%m-%dT%H:%M:%SZ", time.gmtime()),
                    "opened_at": time.strftime("%Y-%m-%dT%H:%M:%SZ", time.gmtime()),
                    "exectime_ms": 0
                }
            self._send_text(201, str(sid))
            return

        if path == '/tasks':
            machine = query.get("machine", [None])[0]
            project = query.get("project", [None])[0]
            session_id = query.get("session_id", [None])[0]
            user = query.get("user", ["heappe"])[0]

            global task_counter
            with lock:
                task_counter += 1
                tid = task_counter
                now_str = time.strftime("%Y-%m-%dT%H:%M:%SZ", time.gmtime())
                tasks[tid] = {
                    "id": tid,
                    "machine": machine or "TestMachine",
                    "project": project,
                    "session": int(session_id) if session_id else None,
                    "user": user,
                    "backend_id": f"bk-{tid}",
                    "state": "finished",
                    "created_at": now_str,
                    "started_at": now_str,
                    "finished_at": now_str,
                    "exectime_ms": 1200,
                    "allocated_time": 1.2,
                    "result": '{"outcome": {"type": "Ok"}, "counts": {"0": 512, "1": 512}}',
                    "artifacts": {"measurements": '{"0": 512, "1": 512}'}
                }
            self._send_text(201, str(tid))
            return

        self._send_json(404, {"error": "not found"})

    def do_PATCH(self):
        parsed = urllib.parse.urlparse(self.path)
        path = parsed.path.rstrip('/')

        content_length = int(self.headers.get('Content-Length', 0))
        body = self.rfile.read(content_length)

        if path.startswith('/projects/'):
            name = path[len('/projects/'):]
            try:
                data = json.loads(body.decode('utf-8'))
            except Exception:
                data = {}
            with lock:
                if name in projects:
                    if "limit_ms" in data:
                        projects[name]["limit_ms"] = data["limit_ms"]
                    if "active" in data:
                        projects[name]["active"] = data["active"]
                    self._send_json(200, projects[name])
                else:
                    self._send_json(404, {"error": "project not found"})
            return

        self._send_json(404, {"error": "not found"})

    def do_DELETE(self):
        parsed = urllib.parse.urlparse(self.path)
        path = parsed.path.rstrip('/')

        if path.startswith('/sessions/'):
            try:
                sid = int(path[len('/sessions/'):])
            except ValueError:
                self._send_json(400, {"error": "invalid session id"})
                return
            with lock:
                if sid in sessions:
                    sessions[sid]["state"] = "closed"
                    sessions[sid]["closed_at"] = time.strftime("%Y-%m-%dT%H:%M:%SZ", time.gmtime())
                    self._send_json(200, {"status": "closed"})
                else:
                    self._send_json(404, {"error": "session not found"})
            return

        if path.startswith('/tasks/'):
            try:
                tid = int(path[len('/tasks/'):])
            except ValueError:
                self._send_json(400, {"error": "invalid task id"})
                return
            with lock:
                if tid in tasks:
                    tasks[tid]["state"] = "cancelled"
                    self._send_json(202, {"status": "cancelled"})
                else:
                    self._send_json(404, {"error": "task not found"})
            return

        self._send_json(404, {"error": "not found"})

    def log_message(self, format, *args):
        sys.stderr.write(f"[QScheduler-CI] {self.address_string()} - {format % args}\n")

if __name__ == '__main__':
    server = HTTPServer(('0.0.0.0', PORT), QSchedulerHandler)
    print(f"QScheduler service running on http://0.0.0.0:{PORT}")
    server.serve_forever()

#!/usr/bin/env python3
"""Exercise the running API against a disposable development database.

Run from the repository root:
  BASE_URL=http://127.0.0.1:5080 mise exec -- python scripts/smoke_test.py

This script creates test accounts, assignments, submissions and grades.
"""

import concurrent.futures
import http.cookiejar
import json
import os
import sys
import threading
import time
import urllib.error
import urllib.parse
import urllib.request
import uuid
from dataclasses import dataclass
from datetime import datetime, timedelta, timezone


BASE_URL = os.environ.get("BASE_URL", "http://127.0.0.1:5080").rstrip("/")
API = "/api/v1"
DEMO_PASSWORD = "Demo123!"


class NoRedirects(urllib.request.HTTPRedirectHandler):
    def redirect_request(self, req, fp, code, msg, headers, newurl):
        return None


@dataclass(frozen=True)
class Response:
    status: int
    body: object
    headers: object

    def check_location(self, path):
        location = self.headers.get("Location")
        assert location, "201 response did not contain a Location header"
        actual = urllib.parse.urljoin(BASE_URL, location)
        assert actual == f"{BASE_URL}{path}", f"Unexpected Location: {actual}"


class Client:
    def __init__(self):
        self.opener = urllib.request.build_opener(
            NoRedirects(),
            urllib.request.HTTPCookieProcessor(http.cookiejar.CookieJar()),
        )

    def request(
        self, method, path, *, body=None, expect=200, marker=True, problem_code=None,
        json_body=True, extra_headers=None,
    ):
        url = f"{BASE_URL}{path}"
        headers = {"Accept": "application/json, application/problem+json"}
        if marker and method in {"POST", "PUT", "DELETE"}:
            headers["X-Homework-Request"] = "1"
        if body is not None:
            headers["Content-Type"] = "application/json"
        if extra_headers:
            headers.update(extra_headers)
        payload = json.dumps(body).encode("utf-8") if body is not None else None
        request = urllib.request.Request(url, data=payload, headers=headers, method=method)
        try:
            result = self.opener.open(request, timeout=15)
        except urllib.error.HTTPError as error:
            result = error
        with result:
            raw = result.read()
            status = result.code
            mime_type = result.headers.get_content_type()
            response_headers = result.headers

        accepted = (expect,) if isinstance(expect, int) else expect
        label = f"{method} {path}"
        assert status in accepted, f"{label}: expected {accepted}, got {status}: {raw[:800]!r}"
        parsed = None
        if status == 204:
            assert not raw, f"{label}: 204 should have no response body"
        elif status >= 400:
            assert mime_type == "application/problem+json", f"{label}: got {mime_type}"
            parsed = json.loads(raw)
            assert parsed.get("status") == status, f"{label}: incorrect problem status"
            for key in ("type", "title", "instance", "code", "traceId"):
                assert isinstance(parsed.get(key), str) and parsed[key], f"{label}: missing {key}"
            if problem_code:
                assert parsed["code"] == problem_code, f"{label}: unexpected code {parsed['code']}"
        elif json_body:
            assert mime_type == "application/json", f"{label}: got {mime_type}"
            parsed = json.loads(raw)
        else:
            parsed = raw
        return Response(status, parsed, response_headers)

    def login(self, email, role):
        user = self.request(
            "POST", f"{API}/auth/login", body={"email": email, "password": DEMO_PASSWORD}
        ).body
        assert user["email"] == email and user["role"] == role
        assert self.request("GET", f"{API}/auth/me").body == user
        return user


def assert_no_grading_material(value):
    if isinstance(value, list):
        for item in value:
            assert_no_grading_material(item)
    if isinstance(value, dict):
        for key, item in value.items():
            assert key not in {"referenceAnswer", "rubric"}, f"Student response exposes {key}"
            assert_no_grading_material(item)


def simultaneously(*operations):
    ready = threading.Barrier(len(operations), timeout=10)

    def run(operation):
        ready.wait()
        return operation()

    with concurrent.futures.ThreadPoolExecutor(max_workers=len(operations)) as pool:
        tasks = [pool.submit(run, operation) for operation in operations]
        return [task.result() for task in tasks]


def main():
    run_id = uuid.uuid4().hex[:12]
    anonymous, teacher, other_teacher, student, duplicate_session, newcomer = (
        Client() for _ in range(6)
    )
    anonymous.request("GET", "/health", json_body=False)
    unauthorized = anonymous.request("GET", f"{API}/assignments", expect=401)
    assert unauthorized.headers.get("WWW-Authenticate", "").startswith("Session ")
    anonymous.request("GET", f"{API}/auth/me", expect=401)
    anonymous.request("GET", f"{API}/unknown-path", expect=404, problem_code="not_found")
    wrong_method = anonymous.request(
        "POST", f"{API}/auth/me", expect=405, problem_code="method_not_allowed"
    )
    assert "GET" in wrong_method.headers["Allow"], "405 did not advertise the supported method"
    preflight = anonymous.request(
        "OPTIONS", f"{API}/auth/login", expect=405, problem_code="method_not_allowed",
        extra_headers={
            "Origin": "https://another.example.test",
            "Access-Control-Request-Method": "POST",
            "Access-Control-Request-Headers": "x-homework-request, content-type",
        },
    )
    assert "Access-Control-Allow-Origin" not in preflight.headers, "Cross-origin browser writes are allowed"
    anonymous.request(
        "POST", f"{API}/auth/login", body={"email": "teacher@example.test", "password": "incorrect"},
        expect=401, problem_code="invalid_credentials",
    )
    teacher.login("teacher@example.test", "teacher")
    other_teacher.login("teacher2@example.test", "teacher")
    student.login("student@example.test", "student")
    duplicate_session.login("student@example.test", "student")
    print("PASS health, authentication, and current-user response", flush=True)

    registration = {
        "name": "集成验证学生",
        "email": f"smoke-{run_id}@example.test",
        "password": DEMO_PASSWORD,
    }
    registered = newcomer.request("POST", f"{API}/auth/register", body=registration, expect=201)
    assert registered.body["role"] == "student"
    registered.check_location(f"{API}/auth/me")
    assert newcomer.request("GET", f"{API}/auth/me").body == registered.body
    newcomer.request(
        "POST", f"{API}/auth/register", body=registration, expect=409,
        problem_code="email_already_registered",
    )
    newcomer.login(registration["email"], "student")

    assignments = f"{API}/assignments"
    assignment = {
        "title": f"算术集成验证 {run_id}",
        "subject": "数学",
        "description": "计算 2 + 3，并写出结果。",
        "referenceAnswer": "5",
        "rubric": "正确结果给满分。",
        "maxScore": 100,
        "dueAt": (datetime.now(timezone.utc) + timedelta(days=2)).isoformat(),
    }
    anonymous.request("POST", assignments, body=assignment, marker=False, expect=401)
    student.request("POST", assignments, body=assignment, expect=403)
    teacher.request("POST", assignments, body=assignment, marker=False, expect=400)
    teacher.request(
        "POST", assignments, body={**assignment, "title": ""}, expect=400, problem_code="invalid_title"
    )
    invalid_body = teacher.request("POST", assignments, body={**assignment, "maxScore": False}, expect=400)
    assert isinstance(invalid_body.body["errors"], dict), "A binding error should include field details"
    teacher.request(
        "POST", assignments,
        body={**assignment, "dueAt": assignment["dueAt"].removesuffix("+00:00")},
        expect=400, problem_code="invalid_request",
    )
    teacher.request(
        "POST", assignments,
        body={**assignment, "dueAt": (datetime.now(timezone.utc) - timedelta(days=1)).isoformat()},
        expect=400,
    )
    created = teacher.request("POST", assignments, body=assignment, expect=201)
    assignment_id = created.body["id"]
    assignment_path = f"{assignments}/{assignment_id}"
    created.check_location(assignment_path)
    assignment["title"] += "（已修改）"
    teacher.request("PUT", assignment_path, body=assignment)
    other_teacher.request("GET", assignment_path, expect=404)
    other_teacher.request("PUT", assignment_path, body=assignment, expect=404)
    other_teacher.request("DELETE", assignment_path, expect=404)
    print("PASS registration, role boundaries, assignment creation, and editing", flush=True)

    student_list = student.request("GET", f"{assignments}?page=1&pageSize=1&status=all").body
    assert student_list["page"] == 1 and student_list["pageSize"] == 1
    assert len(student_list["items"]) == 1 and student_list["totalCount"] >= 1
    assert sum(student_list["summary"][status] for status in ("open", "closed", "pending", "graded")) == (
        student_list["totalCount"]
    )
    assert_no_grading_material(student_list)
    teacher.request("GET", f"{assignments}?page=0", expect=400)
    student_detail = student.request("GET", assignment_path).body
    assert_no_grading_material(student_detail)
    assert all(item["id"] != assignment_id for item in other_teacher.request("GET", assignments).body["items"])
    submission_collection = f"{assignment_path}/submissions"
    student.request("POST", submission_collection, body={"content": " "}, expect=400)

    answer = {"content": "2 + 3 = 5"}
    results = simultaneously(
        lambda: student.request("POST", submission_collection, body=answer, expect=(201, 409)),
        lambda: duplicate_session.request("POST", submission_collection, body=answer, expect=(201, 409)),
    )
    assert sorted(item.status for item in results) == [201, 409], "Expected exactly one submission"
    assert next(item for item in results if item.status == 409).body["code"] == "submission_already_exists"
    accepted = next(item for item in results if item.status == 201)
    submission_id = accepted.body["id"]
    submission_path = f"{API}/submissions/{submission_id}"
    accepted.check_location(submission_path)
    assert student.request("GET", submission_path).body["id"] == submission_id
    teacher_detail = teacher.request("GET", assignment_path).body
    assert teacher_detail["submissions"]["totalCount"] == 1
    assert teacher_detail["submissions"]["items"][0]["id"] == submission_id
    assert student.request("GET", assignment_path).body["mySubmission"]["id"] == submission_id
    pending = teacher.request("GET", f"{assignments}?status=pending").body
    assert pending["totalCount"] == pending["summary"]["pending"]
    assert pending["items"] and all(item["status"] == "pending" for item in pending["items"])
    newcomer.request("GET", submission_path, expect=404)
    other_teacher.request("GET", submission_path, expect=404)
    teacher.request("PUT", assignment_path, body=assignment, expect=409, problem_code="assignment_has_submissions")
    teacher.request("DELETE", assignment_path, expect=409, problem_code="assignment_has_submissions")
    print("PASS list and detail contracts, submission isolation, and duplicate prevention", flush=True)

    grade_path = f"{submission_path}/grade"
    suggestion_path = f"{submission_path}/grade-suggestions"
    grade = {"score": 95.5, "feedback": "计算正确，表达清楚。"}
    student.request("PUT", grade_path, body=grade, expect=403)
    other_teacher.request("PUT", grade_path, body=grade, expect=404)
    other_teacher.request("POST", suggestion_path, expect=404)
    for score in (-1, 101, 95.123):
        teacher.request("PUT", grade_path, body={**grade, "score": score}, expect=400)
    teacher.request("POST", suggestion_path, expect=503)
    teacher.request("PUT", grade_path, body=grade)
    saved_grade = {"score": 98, "feedback": "复核后调整。"}
    saved = teacher.request("PUT", grade_path, body=saved_grade).body
    assert teacher.request("PUT", grade_path, body=saved_grade).body == saved, "Identical PUT changed the grade"
    result = student.request("GET", assignment_path).body["mySubmission"]
    assert result["score"] == saved_grade["score"] and result["feedback"] == saved_grade["feedback"]
    student.request("POST", submission_collection, body=answer, expect=409)
    print("PASS grade validation, model disabled, grading, and student results", flush=True)

    disposable = teacher.request(
        "POST", assignments, body={**assignment, "title": f"删除验证 {run_id}"}, expect=201
    ).body
    disposable_path = f"{assignments}/{disposable['id']}"
    teacher.request("DELETE", disposable_path, expect=204)
    teacher.request("GET", disposable_path, expect=404)

    concurrent_assignment = teacher.request(
        "POST", assignments, body={**assignment, "title": f"删除和提交并发验证 {run_id}"}, expect=201
    ).body
    concurrent_path = f"{assignments}/{concurrent_assignment['id']}"
    deleted, submitted = simultaneously(
        lambda: teacher.request("DELETE", concurrent_path, expect=(204, 409)),
        lambda: student.request("POST", f"{concurrent_path}/submissions", body=answer, expect=(201, 404)),
    )
    assert (deleted.status, submitted.status) in {(204, 404), (409, 201)}, "Deletion lost a submission"
    if submitted.status == 201:
        kept_submission = f"{API}/submissions/{submitted.body['id']}"
        assert student.request("GET", kept_submission).body["assignmentId"] == concurrent_assignment["id"]
    else:
        teacher.request("GET", concurrent_path, expect=404)

    deadline = datetime.now(timezone.utc) + timedelta(seconds=3)
    closing_assignment = teacher.request(
        "POST", assignments,
        body={**assignment, "title": f"截止验证 {run_id}", "dueAt": deadline.isoformat()},
        expect=201,
    ).body
    time.sleep(max((deadline - datetime.now(timezone.utc)).total_seconds() + 0.1, 0))
    student.request(
        "POST", f"{assignments}/{closing_assignment['id']}/submissions",
        body=answer, expect=409, problem_code="assignment_closed",
    )
    student.request("POST", f"{API}/auth/logout", expect=204)
    student.request("GET", f"{API}/auth/me", expect=401)
    student.login("student@example.test", "student")
    assert student.request("GET", assignment_path).body["mySubmission"]["score"] == saved_grade["score"]
    print("PASS delete and submit race, assignment deadline, logout, and grade after login", flush=True)
    print("All API integration checks passed.", flush=True)


if __name__ == "__main__":
    try:
        main()
    except (AssertionError, KeyError, TypeError, ValueError, urllib.error.URLError) as error:
        print(f"FAIL: {error}", file=sys.stderr)
        sys.exit(1)

# RiftScan Offline Workflow Check Report

## Result

```text
OFFLINE WORKFLOW CHECK: PASS
status: pass
failed_check_count: 0
```

## Failed Checks

- None

## Checks

- `pass` `py_compile_helpers` exit=`0`
- `pass` `offline_workflow_check_self_test` exit=`0`
- `pass` `operator_self_test` exit=`0`
- `pass` `post_update_baseline_self_test` exit=`0`
- `pass` `capture_readiness_self_test` exit=`0`
- `pass` `patch_intake_self_test` exit=`0`

## Output Paths

```text
report: handoffs/current/offline-workflow-check/OFFLINE_WORKFLOW_CHECK_REPORT.md
summary: handoffs/current/offline-workflow-check/offline-workflow-check-summary.json
log: handoffs/current/offline-workflow-check/offline-workflow-check-log.jsonl
```

## Safety Boundary

```text
offline_only: true
focus_preflight_started: false
capture_started: false
movement_or_input_sent: false
memory_scan_or_read_started: false
offset_validation_started: false
riftreader_validation_started: false
reloadui_sent: false
```

## Git Snapshot

```text
head: 40bbd1c62c5db71ecbe4d5931643d37619f955b3
```

Git status:

```text
 M handoffs/current/offline-workflow-check/offline-workflow-check-log.jsonl

```

Recent commits:

```text
40bbd1c Refresh blocked post-update baseline artifacts
17d69f5 Refresh handoffs after offline workflow check
b3bb14d Add offline workflow check helper
0125e33 Refresh handoff after operator report wrapper
a2cf481 Add operator report command wrapper
```

## Machine-Readable Summary

```json
{
  "app_version": "riftscan-offline-workflow-check-v1.0.0",
  "checks": [
    {
      "args": [
        "C:\\Users\\mrkoo\\AppData\\Local\\Programs\\Python\\Python314\\python.exe",
        "-m",
        "py_compile",
        "tools/riftscan_operator_app.py",
        "tools/riftscan_post_update_baseline.py",
        "tools/riftscan_capture_readiness.py",
        "tools/riftscan_patch_intake_app.py",
        "tools/riftscan_offline_workflow_check.py"
      ],
      "exit_code": 0,
      "name": "py_compile_helpers",
      "status": "pass",
      "stderr": "",
      "stdout": ""
    },
    {
      "args": [
        "C:\\Users\\mrkoo\\AppData\\Local\\Programs\\Python\\Python314\\python.exe",
        "tools/riftscan_offline_workflow_check.py",
        "--self-test"
      ],
      "exit_code": 0,
      "name": "offline_workflow_check_self_test",
      "status": "pass",
      "stderr": "",
      "stdout": "{\n  \"app_version\": \"riftscan-offline-workflow-check-v1.0.0\",\n  \"case_count\": 2,\n  \"created_utc\": \"2026-05-06T04:20:19Z\",\n  \"safety\": {\n    \"capture_started\": false,\n    \"memory_scan_or_read_started\": false,\n    \"movement_or_input_sent\": false,\n    \"reloadui_sent\": false,\n    \"runs_focus_preflight\": false,\n    \"writes_artifacts\": false\n  },\n  \"schema_version\": \"riftscan.offline_workflow_check_self_test.v1\",\n  \"status\": \"PASS\",\n  \"tests\": [\n    {\n      \"actual\": \"pass\",\n      \"expected\": \"pass\",\n      \"failed_checks\": [],\n      \"name\": \"all pass\",\n      \"pass\": true\n    },\n    {\n      \"actual\": \"fail\",\n      \"expected\": \"fail\",\n      \"failed_checks\": [\n        \"b\"\n      ],\n      \"name\": \"one fail\",\n      \"pass\": true\n    }\n  ]\n}\n"
    },
    {
      "args": [
        "C:\\Users\\mrkoo\\AppData\\Local\\Programs\\Python\\Python314\\python.exe",
        "tools/riftscan_operator_app.py",
        "--self-test"
      ],
      "exit_code": 0,
      "name": "operator_self_test",
      "status": "pass",
      "stderr": "",
      "stdout": "{\n  \"schema_version\": \"riftscan.operator_self_test.v1\",\n  \"created_utc\": \"2026-05-06T04:20:20Z\",\n  \"app_version\": \"riftscan-operator-app-v3.8.17\",\n  \"status\": \"PASS\",\n  \"case_count\": 6,\n  \"tests\": [\n    {\n      \"name\": \"all gates pass\",\n      \"expected_gate\": \"PASS\",\n      \"actual_gate\": \"PASS\",\n      \"next_action\": \"Refresh the metadata-only capture plan; live collection/discovery still requires an explicit future gate.\",\n      \"blockers\": [],\n      \"live_collection_allowed\": false,\n      \"old_offsets_trusted\": false,\n      \"pass\": true\n    },\n    {\n      \"name\": \"baseline blocks even when preflight passes\",\n      \"expected_gate\": \"BLOCKED\",\n      \"actual_gate\": \"BLOCKED\",\n      \"next_action\": \"Run Post-Update Baseline after the current updated RIFT client is confirmed stable in-world.\",\n      \"blockers\": [\n        \"Post-Update Baseline is not PASS for the current updated client.\",\n        \"Stable in-world state is not confirmed.\",\n        \"Capture Readiness was generated from an older/different Post-Update Baseline; rerun Capture Readiness.\"\n      ],\n      \"live_collection_allowed\": false,\n      \"old_offsets_trusted\": false,\n      \"pass\": true\n    },\n    {\n      \"name\": \"readiness blocks after baseline pass\",\n      \"expected_gate\": \"BLOCKED\",\n      \"actual_gate\": \"BLOCKED\",\n      \"next_action\": \"Run Capture Readiness and resolve any blockers before capture-plan refresh.\",\n      \"blockers\": [\n        \"Capture Readiness is not PASS.\",\n        \"Post-update baseline is not PASS for the current client.\"\n      ],\n      \"live_collection_allowed\": false,\n      \"old_offsets_trusted\": false,\n      \"pass\": true\n    },\n    {\n      \"name\": \"stale readiness baseline link blocks metadata plan\",\n      \"expected_gate\": \"BLOCKED\",\n      \"actual_gate\": \"BLOCKED\",\n      \"next_action\": \"Run Capture Readiness again against the latest Post-Update Baseline.\",\n      \"blockers\": [\n        \"Capture Readiness was generated from an older/different Post-Update Baseline; rerun Capture Readiness.\"\n      ],\n      \"live_collection_allowed\": false,\n      \"old_offsets_trusted\": false,\n      \"pass\": true\n    },\n    {\n      \"name\": \"full live preflight blocks metadata plan\",\n      \"expected_gate\": \"BLOCKED\",\n      \"actual_gate\": \"BLOCKED\",\n      \"next_action\": \"Run Full Live Preflight before metadata-only capture-plan refresh.\",\n      \"blockers\": [\n        \"Focus status is not foreground_verified.\"\n      ],\n      \"live_collection_allowed\": false,\n      \"old_offsets_trusted\": false,\n      \"pass\": true\n    },\n    {\n      \"name\": \"focus preflight blocks metadata plan\",\n      \"expected_gate\": \"BLOCKED\",\n      \"actual_gate\": \"BLOCKED\",\n      \"next_action\": \"Run Full Live Preflight before metadata-only capture-plan refresh.\",\n      \"blockers\": [\n        \"Focus preflight is not foreground_verified.\"\n      ],\n      \"live_collection_allowed\": false,\n      \"old_offsets_trusted\": false,\n      \"pass\": true\n    }\n  ],\n  \"safety\": {\n    \"writes_artifacts\": false,\n    \"launches_gui\": false,\n    \"runs_focus_preflight\": false,\n    \"capture_started\": false,\n    \"movement_or_input_sent\": false,\n    \"memory_scan_or_read_started\": false,\n    \"reloadui_sent\": false\n  }\n}\n"
    },
    {
      "args": [
        "C:\\Users\\mrkoo\\AppData\\Local\\Programs\\Python\\Python314\\python.exe",
        "tools/riftscan_post_update_baseline.py",
        "--self-test"
      ],
      "exit_code": 0,
      "name": "post_update_baseline_self_test",
      "status": "pass",
      "stderr": "",
      "stdout": "{\n  \"app_version\": \"riftscan-post-update-baseline-v1.0.1\",\n  \"case_count\": 6,\n  \"created_utc\": \"2026-05-06T04:20:20Z\",\n  \"safety\": {\n    \"capture_started\": false,\n    \"memory_scan_or_read_started\": false,\n    \"movement_or_input_sent\": false,\n    \"reloadui_sent\": false,\n    \"runs_focus_preflight\": false,\n    \"writes_artifacts\": false\n  },\n  \"schema_version\": \"riftscan.post_update_baseline_self_test.v1\",\n  \"status\": \"PASS\",\n  \"tests\": [\n    {\n      \"actual_display_status\": \"PASS\",\n      \"actual_status\": \"pass\",\n      \"blockers\": [],\n      \"expected_blocker_substrings\": [],\n      \"expected_display_status\": \"PASS\",\n      \"expected_status\": \"pass\",\n      \"name\": \"pass baseline\",\n      \"pass\": true\n    },\n    {\n      \"actual_display_status\": \"BLOCKED\",\n      \"actual_status\": \"blocked_waiting_for_game_or_focus\",\n      \"blockers\": [\n        \"Maintenance is not confirmed over.\",\n        \"Login is not confirmed successful.\",\n        \"Stable in-world state is not confirmed.\"\n      ],\n      \"expected_blocker_substrings\": [\n        \"Maintenance is not confirmed over\",\n        \"Login is not confirmed successful\",\n        \"Stable in-world state is not confirmed\"\n      ],\n      \"expected_display_status\": \"BLOCKED\",\n      \"expected_status\": \"blocked_waiting_for_game_or_focus\",\n      \"name\": \"blocked manual state\",\n      \"pass\": true\n    },\n    {\n      \"actual_display_status\": \"BLOCKED\",\n      \"actual_status\": \"blocked_waiting_for_game_or_focus\",\n      \"blockers\": [\n        \"Focus preflight command did not complete successfully.\"\n      ],\n      \"expected_blocker_substrings\": [\n        \"Focus preflight command did not complete successfully\"\n      ],\n      \"expected_display_status\": \"BLOCKED\",\n      \"expected_status\": \"blocked_waiting_for_game_or_focus\",\n      \"name\": \"blocked focus command failure\",\n      \"pass\": true\n    },\n    {\n      \"actual_display_status\": \"PASS\",\n      \"actual_status\": \"pass\",\n      \"blockers\": [],\n      \"expected_blocker_substrings\": [],\n      \"expected_display_status\": \"PASS\",\n      \"expected_status\": \"pass\",\n      \"name\": \"skip focus command failure for offline check\",\n      \"pass\": true\n    },\n    {\n      \"actual_display_status\": \"BLOCKED\",\n      \"actual_status\": \"blocked_waiting_for_game_or_focus\",\n      \"blockers\": [\n        \"selected_window is missing or null.\"\n      ],\n      \"expected_blocker_substrings\": [\n        \"selected_window is missing or null\"\n      ],\n      \"expected_display_status\": \"BLOCKED\",\n      \"expected_status\": \"blocked_waiting_for_game_or_focus\",\n      \"name\": \"blocked missing selected window\",\n      \"pass\": true\n    },\n    {\n      \"actual_display_status\": \"BLOCKED\",\n      \"actual_status\": \"blocked_waiting_for_game_or_focus\",\n      \"blockers\": [\n        \"windows.json has no window entries.\"\n      ],\n      \"expected_blocker_substrings\": [\n        \"windows.json has no window entries\"\n      ],\n      \"expected_display_status\": \"BLOCKED\",\n      \"expected_status\": \"blocked_waiting_for_game_or_focus\",\n      \"name\": \"blocked empty windows list\",\n      \"pass\": true\n    }\n  ]\n}\n"
    },
    {
      "args": [
        "C:\\Users\\mrkoo\\AppData\\Local\\Programs\\Python\\Python314\\python.exe",
        "tools/riftscan_capture_readiness.py",
        "--self-test"
      ],
      "exit_code": 0,
      "name": "capture_readiness_self_test",
      "status": "pass",
      "stderr": "",
      "stdout": "{\n  \"app_version\": \"riftscan-capture-readiness-v1.0.1\",\n  \"case_count\": 7,\n  \"created_utc\": \"2026-05-06T04:20:20Z\",\n  \"safety\": {\n    \"capture_started\": false,\n    \"memory_scan_or_read_started\": false,\n    \"movement_or_input_sent\": false,\n    \"reloadui_sent\": false,\n    \"runs_focus_preflight\": false,\n    \"writes_artifacts\": false\n  },\n  \"schema_version\": \"riftscan.capture_readiness_self_test.v1\",\n  \"status\": \"PASS\",\n  \"tests\": [\n    {\n      \"actual_display_status\": \"PASS\",\n      \"actual_status\": \"pass\",\n      \"blockers\": [],\n      \"capture_planning_allowed\": true,\n      \"expected_blocker_substrings\": [],\n      \"expected_display_status\": \"PASS\",\n      \"expected_status\": \"pass\",\n      \"name\": \"pass gate\",\n      \"pass\": true\n    },\n    {\n      \"actual_display_status\": \"BLOCKED\",\n      \"actual_status\": \"blocked_waiting_for_current_baseline\",\n      \"blockers\": [\n        \"Post-update baseline is not PASS for the current client.\",\n        \"Post-update baseline display_status is not PASS.\"\n      ],\n      \"capture_planning_allowed\": false,\n      \"expected_blocker_substrings\": [\n        \"Post-update baseline is not PASS\",\n        \"display_status is not PASS\"\n      ],\n      \"expected_display_status\": \"BLOCKED\",\n      \"expected_status\": \"blocked_waiting_for_current_baseline\",\n      \"name\": \"blocked baseline status\",\n      \"pass\": true\n    },\n    {\n      \"actual_display_status\": \"BLOCKED\",\n      \"actual_status\": \"blocked_waiting_for_current_baseline\",\n      \"blockers\": [\n        \"Safety field baseline.safety.old_offsets_trusted is not false.\"\n      ],\n      \"capture_planning_allowed\": false,\n      \"expected_blocker_substrings\": [\n        \"baseline.safety.old_offsets_trusted\"\n      ],\n      \"expected_display_status\": \"BLOCKED\",\n      \"expected_status\": \"blocked_waiting_for_current_baseline\",\n      \"name\": \"blocked unsafe baseline safety\",\n      \"pass\": true\n    },\n    {\n      \"actual_display_status\": \"BLOCKED\",\n      \"actual_status\": \"blocked_waiting_for_current_baseline\",\n      \"blockers\": [\n        \"Current focus status is not foreground_verified.\"\n      ],\n      \"capture_planning_allowed\": false,\n      \"expected_blocker_substrings\": [\n        \"Current focus status is not foreground_verified\"\n      ],\n      \"expected_display_status\": \"BLOCKED\",\n      \"expected_status\": \"blocked_waiting_for_current_baseline\",\n      \"name\": \"blocked current focus lost\",\n      \"pass\": true\n    },\n    {\n      \"actual_display_status\": \"BLOCKED\",\n      \"actual_status\": \"blocked_waiting_for_current_baseline\",\n      \"blockers\": [\n        \"Current RIFT PID differs from the post-update baseline; rerun Post-Update Baseline.\"\n      ],\n      \"capture_planning_allowed\": false,\n      \"expected_blocker_substrings\": [\n        \"Current RIFT PID differs\"\n      ],\n      \"expected_display_status\": \"BLOCKED\",\n      \"expected_status\": \"blocked_waiting_for_current_baseline\",\n      \"name\": \"blocked pid drift\",\n      \"pass\": true\n    },\n    {\n      \"actual_display_status\": \"BLOCKED\",\n      \"actual_status\": \"blocked_waiting_for_current_baseline\",\n      \"blockers\": [\n        \"Current focus preflight command did not complete successfully.\"\n      ],\n      \"capture_planning_allowed\": false,\n      \"expected_blocker_substrings\": [\n        \"Current focus preflight command did not complete successfully\"\n      ],\n      \"expected_display_status\": \"BLOCKED\",\n      \"expected_status\": \"blocked_waiting_for_current_baseline\",\n      \"name\": \"blocked focus command failure\",\n      \"pass\": true\n    },\n    {\n      \"actual_display_status\": \"PASS\",\n      \"actual_status\": \"pass\",\n      \"blockers\": [],\n      \"capture_planning_allowed\": true,\n      \"expected_blocker_substrings\": [],\n      \"expected_display_status\": \"PASS\",\n      \"expected_status\": \"pass\",\n      \"name\": \"skip focus command failure for offline check\",\n      \"pass\": true\n    }\n  ]\n}\n"
    },
    {
      "args": [
        "C:\\Users\\mrkoo\\AppData\\Local\\Programs\\Python\\Python314\\python.exe",
        "tools/riftscan_patch_intake_app.py",
        "--self-test"
      ],
      "exit_code": 0,
      "name": "patch_intake_self_test",
      "status": "pass",
      "stderr": "",
      "stdout": "{\n  \"app_version\": \"riftscan-patch-intake-v1.2.5\",\n  \"created_utc\": \"2026-05-06T04:20:25Z\",\n  \"schema_version\": \"riftscan.patch_intake_self_test.v1\",\n  \"status\": \"PASS\",\n  \"tests\": [\n    {\n      \"actual\": \"FAIL_BAD_HEADER\",\n      \"expected\": \"FAIL_BAD_HEADER\",\n      \"issues\": [\n        \"Payload must start with RIFTSCAN_CLIPBOARD_PATCH_V1 or RIFTSCAN_CHUNKED_PATCH_V1.\"\n      ],\n      \"name\": \"empty payload\",\n      \"pass\": true\n    },\n    {\n      \"actual\": \"FAIL_BAD_HEADER\",\n      \"expected\": \"FAIL_BAD_HEADER\",\n      \"issues\": [\n        \"Payload must start with RIFTSCAN_CLIPBOARD_PATCH_V1 or RIFTSCAN_CHUNKED_PATCH_V1.\"\n      ],\n      \"name\": \"wrong header\",\n      \"pass\": true\n    },\n    {\n      \"actual\": \"FAIL_BAD_MANIFEST\",\n      \"expected\": \"FAIL_BAD_MANIFEST\",\n      \"issues\": [\n        \"JSONDecodeError: Expecting property name enclosed in double quotes: line 1 column 2 (char 1)\"\n      ],\n      \"name\": \"bad json\",\n      \"pass\": true\n    },\n    {\n      \"actual\": \"FAIL_MISSING_PAYLOAD\",\n      \"expected\": \"FAIL_MISSING_PAYLOAD\",\n      \"issues\": [\n        \"Payload block markers are missing.\"\n      ],\n      \"name\": \"missing payload\",\n      \"pass\": true\n    },\n    {\n      \"actual\": \"FAIL_HASH_MISMATCH\",\n      \"expected\": \"FAIL_HASH_MISMATCH\",\n      \"issues\": [],\n      \"name\": \"hash mismatch\",\n      \"pass\": true\n    },\n    {\n      \"actual\": \"FAIL_STALE_PATCH\",\n      \"expected\": \"FAIL_STALE_PATCH\",\n      \"issues\": [\n        \"Patch timestamp is not newer than last accepted patch.\"\n      ],\n      \"name\": \"stale timestamp\",\n      \"pass\": true\n    },\n    {\n      \"actual\": \"FAIL_WRONG_REPO\",\n      \"expected\": \"FAIL_WRONG_REPO\",\n      \"issues\": [\n        \"target_repo_root does not match selected repo root.\"\n      ],\n      \"name\": \"wrong repo\",\n      \"pass\": true\n    },\n    {\n      \"actual\": \"PASS_DRY_RUN\",\n      \"expected\": \"PASS_DRY_RUN\",\n      \"issues\": [],\n      \"name\": \"valid dry run\",\n      \"pass\": true\n    },\n    {\n      \"actual\": \"FAIL_COMMIT_WITHOUT_APPLY\",\n      \"expected\": \"FAIL_COMMIT_WITHOUT_APPLY\",\n      \"issues\": [\n        \"No successful process/apply result exists.\"\n      ],\n      \"name\": \"commit without apply\",\n      \"pass\": true\n    },\n    {\n      \"actual\": \"PASS_PROCESSED\",\n      \"expected\": \"PASS_PROCESSED\",\n      \"issues\": [],\n      \"name\": \"process payload without commit metadata\",\n      \"pass\": true\n    },\n    {\n      \"actual\": \"FAIL_COMMIT_MISSING_METADATA\",\n      \"expected\": \"FAIL_COMMIT_MISSING_METADATA\",\n      \"issues\": [\n        \"Manifest commit block is required.\"\n      ],\n      \"name\": \"commit missing metadata\",\n      \"pass\": true\n    },\n    {\n      \"actual\": \"FAIL_COMMIT_UNSAFE_STAGE_PATH\",\n      \"expected\": \"FAIL_COMMIT_UNSAFE_STAGE_PATH\",\n      \"issues\": [\n        \"Unsafe commit.stage_paths entry: .\"\n      ],\n      \"name\": \"unsafe commit stage path validation\",\n      \"pass\": true\n    },\n    {\n      \"actual\": \"PASS_PROCESSED\",\n      \"expected\": \"PASS_PROCESSED\",\n      \"issues\": [],\n      \"name\": \"process payload with capture readiness checks\",\n      \"pass\": true\n    },\n    {\n      \"actual\": \"PASS_PROCESSED\",\n      \"expected\": \"PASS_PROCESSED\",\n      \"issues\": [],\n      \"name\": \"process payload\",\n      \"pass\": true\n    },\n    {\n      \"actual\": \"PASS_COMMITTED\",\n      \"expected\": \"PASS_COMMITTED\",\n      \"issues\": [],\n      \"name\": \"commit in temp repo\",\n      \"pass\": true\n    },\n    {\n      \"actual\": \"PASS_PUSH_VERIFY_SIMULATED_OR_SKIPPED\",\n      \"expected\": \"PASS_PUSH_VERIFY_SIMULATED_OR_SKIPPED\",\n      \"issues\": [],\n      \"name\": \"push verify simulated\",\n      \"pass\": true\n    },\n    {\n      \"actual\": \"PASS_DRY_RUN\",\n      \"expected\": \"PASS_DRY_RUN\",\n      \"issues\": [],\n      \"name\": \"chunked dry run\",\n      \"pass\": true\n    },\n    {\n      \"actual\": \"FAIL_CHUNK_HASH_MISMATCH\",\n      \"expected\": \"FAIL_CHUNK_HASH_MISMATCH\",\n      \"issues\": [\n        \"chunk 1 hash mismatch\"\n      ],\n      \"name\": \"chunked bad chunk hash\",\n      \"pass\": true\n    },\n    {\n      \"actual\": \"FAIL_MISSING_CHUNK\",\n      \"expected\": \"FAIL_MISSING_CHUNK\",\n      \"issues\": [\n        \"missing chunks: 1\"\n      ],\n      \"name\": \"chunked missing chunk\",\n      \"pass\": true\n    }\n  ]\n}\n"
    }
  ],
  "created_utc": "2026-05-06T04:20:25Z",
  "display_status": "PASS",
  "failed_check_count": 0,
  "failed_checks": [],
  "git": {
    "command_status": {
      "head": "pass",
      "log": "pass",
      "status": "pass"
    },
    "head": "40bbd1c62c5db71ecbe4d5931643d37619f955b3",
    "log_oneline_5": "40bbd1c Refresh blocked post-update baseline artifacts\n17d69f5 Refresh handoffs after offline workflow check\nb3bb14d Add offline workflow check helper\n0125e33 Refresh handoff after operator report wrapper\na2cf481 Add operator report command wrapper",
    "status_short": " M handoffs/current/offline-workflow-check/offline-workflow-check-log.jsonl\n"
  },
  "paths": {
    "log": "handoffs/current/offline-workflow-check/offline-workflow-check-log.jsonl",
    "report": "handoffs/current/offline-workflow-check/OFFLINE_WORKFLOW_CHECK_REPORT.md",
    "summary": "handoffs/current/offline-workflow-check/offline-workflow-check-summary.json"
  },
  "safety": {
    "capture_started": false,
    "focus_preflight_started": false,
    "memory_scan_or_read_started": false,
    "movement_or_input_sent": false,
    "offline_only": true,
    "offset_validation_started": false,
    "reloadui_sent": false,
    "riftreader_validation_started": false
  },
  "schema_version": "riftscan.offline_workflow_check.v1",
  "status": "pass"
}
```

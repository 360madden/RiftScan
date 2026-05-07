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
- `pass` `capture_plan_check_self_test` exit=`0`
- `pass` `movement_test_readiness_self_test` exit=`0`
- `pass` `movement_execution_gate_self_test` exit=`0`
- `pass` `discovery_ledger_self_test` exit=`0`
- `pass` `discovery_ledger_refresh` exit=`0`
- `pass` `discovery_ledger_validate_existing` exit=`0`
- `pass` `candidate_ledger_consumer_self_test` exit=`0`
- `pass` `candidate_ledger_consumer_refresh` exit=`0`
- `pass` `ai_workflow_packet_self_test` exit=`0`
- `pass` `patch_intake_self_test` exit=`0`
- `pass` `ai_workflow_packet_contract` exit=`0`

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
riftreader_command_executed: false
reloadui_sent: false
```

## Git Snapshot

```text
head: 60a8e5b1066a4358a91ff0df77eb8604e93f965f
```

Git status:

```text
 M docs/ai-workflow-packet-schema.md
 M docs/helper-tooling-policy.md
 M handoffs/current/ai-workflow/AI_WORKFLOW_PACKET.md
 M handoffs/current/ai-workflow/ai-workflow-log.jsonl
 M handoffs/current/ai-workflow/ai-workflow-summary.json
 M handoffs/current/ai-workflow/history/index.jsonl
 M handoffs/current/candidate-ledger-consumer/CANDIDATE_LEDGER_CONSUMER_REPORT.md
 M handoffs/current/candidate-ledger-consumer/candidate-ledger-consumer-log.jsonl
 M handoffs/current/candidate-ledger-consumer/candidate-ledger-consumer-summary.json
 M handoffs/current/discovery-ledger/DISCOVERY_LEDGER_REPORT.md
 M handoffs/current/discovery-ledger/discovery-ledger-log.jsonl
 M handoffs/current/discovery-ledger/discovery-ledger-summary.json
 M handoffs/current/offline-workflow-check/OFFLINE_WORKFLOW_CHECK_REPORT.md
 M handoffs/current/offline-workflow-check/offline-workflow-check-log.jsonl
 M handoffs/current/offline-workflow-check/offline-workflow-check-summary.json
 M tools/riftscan_ai_workflow_packet.py
 M tools/riftscan_offline_workflow_check.py
?? handoffs/current/ai-workflow/AI_WORKFLOW_HISTORY_INDEX_REPORT.md
?? handoffs/current/ai-workflow/ai-workflow-history-index-summary.json
?? handoffs/current/ai-workflow/history/AI_WORKFLOW_PACKET-2026-05-07T18-03-05Z-riftscan-ai-workflow-packet-v1-9-0.md
?? handoffs/current/ai-workflow/history/ai-workflow-summary-2026-05-07T18-03-05Z-riftscan-ai-workflow-packet-v1-9-0.json

```

Recent commits:

```text
60a8e5b Add AI packet history index view
aad6f11 Validate full AI packet history index
237d9f2 Index AI workflow packet history
d0d0e92 Validate AI packet archive offline
6bef855 Archive AI workflow packet history
```

## Machine-Readable Summary

```json
{
  "app_version": "riftscan-offline-workflow-check-v1.0.12",
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
        "tools/riftscan_offline_workflow_check.py",
        "tools/riftscan_capture_plan_check.py",
        "tools/riftscan_movement_test_readiness.py",
        "tools/riftscan_movement_execution_gate.py",
        "tools/riftscan_discovery_ledger.py",
        "tools/riftscan_ai_workflow_packet.py",
        "tools/riftscan_candidate_ledger_consumer.py"
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
      "stdout": "{\n  \"app_version\": \"riftscan-offline-workflow-check-v1.0.12\",\n  \"case_count\": 5,\n  \"created_utc\": \"2026-05-07T18:11:13Z\",\n  \"safety\": {\n    \"capture_started\": false,\n    \"memory_scan_or_read_started\": false,\n    \"movement_or_input_sent\": false,\n    \"reloadui_sent\": false,\n    \"riftreader_command_executed\": false,\n    \"runs_focus_preflight\": false,\n    \"writes_artifacts\": false\n  },\n  \"schema_version\": \"riftscan.offline_workflow_check_self_test.v1\",\n  \"status\": \"PASS\",\n  \"tests\": [\n    {\n      \"actual\": \"pass\",\n      \"expected\": \"pass\",\n      \"failed_checks\": [],\n      \"name\": \"all pass\",\n      \"pass\": true\n    },\n    {\n      \"actual\": \"fail\",\n      \"expected\": \"fail\",\n      \"failed_checks\": [\n        \"b\"\n      ],\n      \"name\": \"one fail\",\n      \"pass\": true\n    },\n    {\n      \"actual\": \"pass\",\n      \"errors\": [],\n      \"expected\": \"pass\",\n      \"name\": \"ai packet contract pass\",\n      \"pass\": true\n    },\n    {\n      \"actual\": \"fail\",\n      \"errors\": [\n        \"previous_packet_diff_compared_fields missing required field(s): status, blocker_count, warning_count, current_best_stable_id, current_best_address, candidate_consumer_status, safe_candidate_count, rejected_candidate_count, artifact_stale_count, artifact_missing_count, current_best_stale_count, current_best_missing_count, discovery_ledger_contract_status, offline_workflow_status, operator_live_collection_allowed\"\n      ],\n      \"expected\": \"fail\",\n      \"name\": \"ai packet contract blocks missing fields\",\n      \"pass\": true\n    },\n    {\n      \"actual\": \"fail\",\n      \"errors\": [\n        \"previous_packet_archive.history_index is missing or outside the repo\",\n        \"packet_history_index is missing or not an object\",\n        \"previous_packet_archive.artifacts.summary is missing or outside the repo\",\n        \"previous_packet_archive.artifacts.report is missing or outside the repo\"\n      ],\n      \"expected\": \"fail\",\n      \"name\": \"ai packet contract blocks archived packet without artifact paths\",\n      \"pass\": true\n    }\n  ]\n}\n"
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
      "stdout": "{\n  \"schema_version\": \"riftscan.operator_self_test.v1\",\n  \"created_utc\": \"2026-05-07T18:11:14Z\",\n  \"app_version\": \"riftscan-operator-app-v3.8.22\",\n  \"status\": \"PASS\",\n  \"case_count\": 11,\n  \"tests\": [\n    {\n      \"name\": \"all gates pass without latest capture plan\",\n      \"expected_gate\": \"PASS\",\n      \"actual_gate\": \"PASS\",\n      \"next_action\": \"Refresh the metadata-only capture plan; live collection/discovery still requires an explicit future gate.\",\n      \"blockers\": [],\n      \"live_collection_allowed\": false,\n      \"old_offsets_trusted\": false,\n      \"pass\": true\n    },\n    {\n      \"name\": \"all gates pass with latest metadata-only capture plan\",\n      \"expected_gate\": \"PASS\",\n      \"actual_gate\": \"PASS\",\n      \"next_action\": \"Run Capture Plan Check and review the latest metadata-only capture plan; live collection/discovery still requires an explicit future gate.\",\n      \"blockers\": [],\n      \"live_collection_allowed\": false,\n      \"old_offsets_trusted\": false,\n      \"pass\": true\n    },\n    {\n      \"name\": \"all gates pass with capture plan check\",\n      \"expected_gate\": \"PASS\",\n      \"actual_gate\": \"PASS\",\n      \"next_action\": \"Run Movement Test Readiness before staging any live game-world movement test.\",\n      \"blockers\": [],\n      \"live_collection_allowed\": false,\n      \"old_offsets_trusted\": false,\n      \"pass\": true\n    },\n    {\n      \"name\": \"all gates pass with movement test readiness\",\n      \"expected_gate\": \"PASS\",\n      \"actual_gate\": \"PASS\",\n      \"next_action\": \"Run the final current-window Movement Execution Gate; live movement still requires immediate PID/HWND/focus revalidation and abort controls.\",\n      \"blockers\": [],\n      \"live_collection_allowed\": false,\n      \"old_offsets_trusted\": false,\n      \"pass\": true\n    },\n    {\n      \"name\": \"all gates pass with blocked movement execution gate\",\n      \"expected_gate\": \"PASS\",\n      \"actual_gate\": \"PASS\",\n      \"next_action\": \"Resolve Movement Execution Gate blockers and rerun it; do not send movement/input.\",\n      \"blockers\": [],\n      \"live_collection_allowed\": false,\n      \"old_offsets_trusted\": false,\n      \"pass\": true\n    },\n    {\n      \"name\": \"all gates pass with movement execution gate pass\",\n      \"expected_gate\": \"PASS\",\n      \"actual_gate\": \"PASS\",\n      \"next_action\": \"Movement Execution Gate is PASS; if still before expires_utc, run only the exact bounded move_forward command from that gate.\",\n      \"blockers\": [],\n      \"live_collection_allowed\": false,\n      \"old_offsets_trusted\": false,\n      \"pass\": true\n    },\n    {\n      \"name\": \"baseline blocks even when preflight passes\",\n      \"expected_gate\": \"BLOCKED\",\n      \"actual_gate\": \"BLOCKED\",\n      \"next_action\": \"Run Post-Update Baseline after the current updated RIFT client is confirmed stable in-world.\",\n      \"blockers\": [\n        \"Post-Update Baseline is not PASS for the current updated client.\",\n        \"Stable in-world state is not confirmed.\",\n        \"Capture Readiness was generated from an older/different Post-Update Baseline; rerun Capture Readiness.\"\n      ],\n      \"live_collection_allowed\": false,\n      \"old_offsets_trusted\": false,\n      \"pass\": true\n    },\n    {\n      \"name\": \"readiness blocks after baseline pass\",\n      \"expected_gate\": \"BLOCKED\",\n      \"actual_gate\": \"BLOCKED\",\n      \"next_action\": \"Run Capture Readiness and resolve any blockers before capture-plan refresh.\",\n      \"blockers\": [\n        \"Capture Readiness is not PASS.\",\n        \"Post-update baseline is not PASS for the current client.\"\n      ],\n      \"live_collection_allowed\": false,\n      \"old_offsets_trusted\": false,\n      \"pass\": true\n    },\n    {\n      \"name\": \"stale readiness baseline link blocks metadata plan\",\n      \"expected_gate\": \"BLOCKED\",\n      \"actual_gate\": \"BLOCKED\",\n      \"next_action\": \"Run Capture Readiness again against the latest Post-Update Baseline.\",\n      \"blockers\": [\n        \"Capture Readiness was generated from an older/different Post-Update Baseline; rerun Capture Readiness.\"\n      ],\n      \"live_collection_allowed\": false,\n      \"old_offsets_trusted\": false,\n      \"pass\": true\n    },\n    {\n      \"name\": \"full live preflight blocks metadata plan\",\n      \"expected_gate\": \"BLOCKED\",\n      \"actual_gate\": \"BLOCKED\",\n      \"next_action\": \"Run Full Live Preflight before metadata-only capture-plan refresh.\",\n      \"blockers\": [\n        \"Focus status is not foreground_verified.\"\n      ],\n      \"live_collection_allowed\": false,\n      \"old_offsets_trusted\": false,\n      \"pass\": true\n    },\n    {\n      \"name\": \"focus preflight blocks metadata plan\",\n      \"expected_gate\": \"BLOCKED\",\n      \"actual_gate\": \"BLOCKED\",\n      \"next_action\": \"Run Full Live Preflight before metadata-only capture-plan refresh.\",\n      \"blockers\": [\n        \"Focus preflight is not foreground_verified.\"\n      ],\n      \"live_collection_allowed\": false,\n      \"old_offsets_trusted\": false,\n      \"pass\": true\n    }\n  ],\n  \"safety\": {\n    \"writes_artifacts\": false,\n    \"launches_gui\": false,\n    \"runs_focus_preflight\": false,\n    \"capture_started\": false,\n    \"movement_or_input_sent\": false,\n    \"memory_scan_or_read_started\": false,\n    \"reloadui_sent\": false\n  }\n}\n"
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
      "stdout": "{\n  \"app_version\": \"riftscan-post-update-baseline-v1.0.1\",\n  \"case_count\": 6,\n  \"created_utc\": \"2026-05-07T18:11:14Z\",\n  \"safety\": {\n    \"capture_started\": false,\n    \"memory_scan_or_read_started\": false,\n    \"movement_or_input_sent\": false,\n    \"reloadui_sent\": false,\n    \"runs_focus_preflight\": false,\n    \"writes_artifacts\": false\n  },\n  \"schema_version\": \"riftscan.post_update_baseline_self_test.v1\",\n  \"status\": \"PASS\",\n  \"tests\": [\n    {\n      \"actual_display_status\": \"PASS\",\n      \"actual_status\": \"pass\",\n      \"blockers\": [],\n      \"expected_blocker_substrings\": [],\n      \"expected_display_status\": \"PASS\",\n      \"expected_status\": \"pass\",\n      \"name\": \"pass baseline\",\n      \"pass\": true\n    },\n    {\n      \"actual_display_status\": \"BLOCKED\",\n      \"actual_status\": \"blocked_waiting_for_game_or_focus\",\n      \"blockers\": [\n        \"Maintenance is not confirmed over.\",\n        \"Login is not confirmed successful.\",\n        \"Stable in-world state is not confirmed.\"\n      ],\n      \"expected_blocker_substrings\": [\n        \"Maintenance is not confirmed over\",\n        \"Login is not confirmed successful\",\n        \"Stable in-world state is not confirmed\"\n      ],\n      \"expected_display_status\": \"BLOCKED\",\n      \"expected_status\": \"blocked_waiting_for_game_or_focus\",\n      \"name\": \"blocked manual state\",\n      \"pass\": true\n    },\n    {\n      \"actual_display_status\": \"BLOCKED\",\n      \"actual_status\": \"blocked_waiting_for_game_or_focus\",\n      \"blockers\": [\n        \"Focus preflight command did not complete successfully.\"\n      ],\n      \"expected_blocker_substrings\": [\n        \"Focus preflight command did not complete successfully\"\n      ],\n      \"expected_display_status\": \"BLOCKED\",\n      \"expected_status\": \"blocked_waiting_for_game_or_focus\",\n      \"name\": \"blocked focus command failure\",\n      \"pass\": true\n    },\n    {\n      \"actual_display_status\": \"PASS\",\n      \"actual_status\": \"pass\",\n      \"blockers\": [],\n      \"expected_blocker_substrings\": [],\n      \"expected_display_status\": \"PASS\",\n      \"expected_status\": \"pass\",\n      \"name\": \"skip focus command failure for offline check\",\n      \"pass\": true\n    },\n    {\n      \"actual_display_status\": \"BLOCKED\",\n      \"actual_status\": \"blocked_waiting_for_game_or_focus\",\n      \"blockers\": [\n        \"selected_window is missing or null.\"\n      ],\n      \"expected_blocker_substrings\": [\n        \"selected_window is missing or null\"\n      ],\n      \"expected_display_status\": \"BLOCKED\",\n      \"expected_status\": \"blocked_waiting_for_game_or_focus\",\n      \"name\": \"blocked missing selected window\",\n      \"pass\": true\n    },\n    {\n      \"actual_display_status\": \"BLOCKED\",\n      \"actual_status\": \"blocked_waiting_for_game_or_focus\",\n      \"blockers\": [\n        \"windows.json has no window entries.\"\n      ],\n      \"expected_blocker_substrings\": [\n        \"windows.json has no window entries\"\n      ],\n      \"expected_display_status\": \"BLOCKED\",\n      \"expected_status\": \"blocked_waiting_for_game_or_focus\",\n      \"name\": \"blocked empty windows list\",\n      \"pass\": true\n    }\n  ]\n}\n"
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
      "stdout": "{\n  \"app_version\": \"riftscan-capture-readiness-v1.0.1\",\n  \"case_count\": 7,\n  \"created_utc\": \"2026-05-07T18:11:14Z\",\n  \"safety\": {\n    \"capture_started\": false,\n    \"memory_scan_or_read_started\": false,\n    \"movement_or_input_sent\": false,\n    \"reloadui_sent\": false,\n    \"runs_focus_preflight\": false,\n    \"writes_artifacts\": false\n  },\n  \"schema_version\": \"riftscan.capture_readiness_self_test.v1\",\n  \"status\": \"PASS\",\n  \"tests\": [\n    {\n      \"actual_display_status\": \"PASS\",\n      \"actual_status\": \"pass\",\n      \"blockers\": [],\n      \"capture_planning_allowed\": true,\n      \"expected_blocker_substrings\": [],\n      \"expected_display_status\": \"PASS\",\n      \"expected_status\": \"pass\",\n      \"name\": \"pass gate\",\n      \"pass\": true\n    },\n    {\n      \"actual_display_status\": \"BLOCKED\",\n      \"actual_status\": \"blocked_waiting_for_current_baseline\",\n      \"blockers\": [\n        \"Post-update baseline is not PASS for the current client.\",\n        \"Post-update baseline display_status is not PASS.\"\n      ],\n      \"capture_planning_allowed\": false,\n      \"expected_blocker_substrings\": [\n        \"Post-update baseline is not PASS\",\n        \"display_status is not PASS\"\n      ],\n      \"expected_display_status\": \"BLOCKED\",\n      \"expected_status\": \"blocked_waiting_for_current_baseline\",\n      \"name\": \"blocked baseline status\",\n      \"pass\": true\n    },\n    {\n      \"actual_display_status\": \"BLOCKED\",\n      \"actual_status\": \"blocked_waiting_for_current_baseline\",\n      \"blockers\": [\n        \"Safety field baseline.safety.old_offsets_trusted is not false.\"\n      ],\n      \"capture_planning_allowed\": false,\n      \"expected_blocker_substrings\": [\n        \"baseline.safety.old_offsets_trusted\"\n      ],\n      \"expected_display_status\": \"BLOCKED\",\n      \"expected_status\": \"blocked_waiting_for_current_baseline\",\n      \"name\": \"blocked unsafe baseline safety\",\n      \"pass\": true\n    },\n    {\n      \"actual_display_status\": \"BLOCKED\",\n      \"actual_status\": \"blocked_waiting_for_current_baseline\",\n      \"blockers\": [\n        \"Current focus status is not foreground_verified.\"\n      ],\n      \"capture_planning_allowed\": false,\n      \"expected_blocker_substrings\": [\n        \"Current focus status is not foreground_verified\"\n      ],\n      \"expected_display_status\": \"BLOCKED\",\n      \"expected_status\": \"blocked_waiting_for_current_baseline\",\n      \"name\": \"blocked current focus lost\",\n      \"pass\": true\n    },\n    {\n      \"actual_display_status\": \"BLOCKED\",\n      \"actual_status\": \"blocked_waiting_for_current_baseline\",\n      \"blockers\": [\n        \"Current RIFT PID differs from the post-update baseline; rerun Post-Update Baseline.\"\n      ],\n      \"capture_planning_allowed\": false,\n      \"expected_blocker_substrings\": [\n        \"Current RIFT PID differs\"\n      ],\n      \"expected_display_status\": \"BLOCKED\",\n      \"expected_status\": \"blocked_waiting_for_current_baseline\",\n      \"name\": \"blocked pid drift\",\n      \"pass\": true\n    },\n    {\n      \"actual_display_status\": \"BLOCKED\",\n      \"actual_status\": \"blocked_waiting_for_current_baseline\",\n      \"blockers\": [\n        \"Current focus preflight command did not complete successfully.\"\n      ],\n      \"capture_planning_allowed\": false,\n      \"expected_blocker_substrings\": [\n        \"Current focus preflight command did not complete successfully\"\n      ],\n      \"expected_display_status\": \"BLOCKED\",\n      \"expected_status\": \"blocked_waiting_for_current_baseline\",\n      \"name\": \"blocked focus command failure\",\n      \"pass\": true\n    },\n    {\n      \"actual_display_status\": \"PASS\",\n      \"actual_status\": \"pass\",\n      \"blockers\": [],\n      \"capture_planning_allowed\": true,\n      \"expected_blocker_substrings\": [],\n      \"expected_display_status\": \"PASS\",\n      \"expected_status\": \"pass\",\n      \"name\": \"skip focus command failure for offline check\",\n      \"pass\": true\n    }\n  ]\n}\n"
    },
    {
      "args": [
        "C:\\Users\\mrkoo\\AppData\\Local\\Programs\\Python\\Python314\\python.exe",
        "tools/riftscan_capture_plan_check.py",
        "--self-test"
      ],
      "exit_code": 0,
      "name": "capture_plan_check_self_test",
      "status": "pass",
      "stderr": "",
      "stdout": "{\n  \"app_version\": \"riftscan-capture-plan-check-v1.0.0\",\n  \"case_count\": 5,\n  \"created_utc\": \"2026-05-07T18:11:14Z\",\n  \"safety\": {\n    \"capture_started\": false,\n    \"memory_scan_or_read_started\": false,\n    \"movement_or_input_sent\": false,\n    \"reloadui_sent\": false,\n    \"runs_focus_preflight\": false,\n    \"writes_current_artifacts\": false\n  },\n  \"schema_version\": \"riftscan.capture_plan_check_self_test.v1\",\n  \"status\": \"PASS\",\n  \"tests\": [\n    {\n      \"actual_display_status\": \"PASS\",\n      \"actual_status\": \"pass\",\n      \"blockers\": [],\n      \"expected_display_status\": \"PASS\",\n      \"expected_status\": \"pass\",\n      \"name\": \"valid metadata-only plan\",\n      \"pass\": true\n    },\n    {\n      \"actual_display_status\": \"BLOCKED\",\n      \"actual_status\": \"blocked_capture_plan_not_valid\",\n      \"blockers\": [\n        \"capture-plan capture_started is not false.\"\n      ],\n      \"expected_display_status\": \"BLOCKED\",\n      \"expected_status\": \"blocked_capture_plan_not_valid\",\n      \"name\": \"blocked capture_started true\",\n      \"pass\": true\n    },\n    {\n      \"actual_display_status\": \"BLOCKED\",\n      \"actual_status\": \"blocked_capture_plan_not_valid\",\n      \"blockers\": [\n        \"Capture-plan expected_files is missing required metadata outputs: capture-log.jsonl, focus-summary-after.json, focus-summary-before.json, operator-report.md.\"\n      ],\n      \"expected_display_status\": \"BLOCKED\",\n      \"expected_status\": \"blocked_capture_plan_not_valid\",\n      \"name\": \"blocked missing expected files\",\n      \"pass\": true\n    },\n    {\n      \"actual_display_status\": \"BLOCKED\",\n      \"actual_status\": \"blocked_capture_plan_not_valid\",\n      \"blockers\": [\n        \"Capture-plan focus/preflight source artifacts are missing: windows_json=handoffs/current/focus-control-local/windows.json.\"\n      ],\n      \"expected_display_status\": \"BLOCKED\",\n      \"expected_status\": \"blocked_capture_plan_not_valid\",\n      \"name\": \"blocked missing source artifact\",\n      \"pass\": true\n    },\n    {\n      \"actual_display_status\": \"BLOCKED\",\n      \"actual_status\": \"blocked_capture_plan_not_valid\",\n      \"blockers\": [\n        \"Operator gate live_collection_allowed is not false.\"\n      ],\n      \"expected_display_status\": \"BLOCKED\",\n      \"expected_status\": \"blocked_capture_plan_not_valid\",\n      \"name\": \"blocked operator live collection allowed\",\n      \"pass\": true\n    }\n  ]\n}\n"
    },
    {
      "args": [
        "C:\\Users\\mrkoo\\AppData\\Local\\Programs\\Python\\Python314\\python.exe",
        "tools/riftscan_movement_test_readiness.py",
        "--self-test"
      ],
      "exit_code": 0,
      "name": "movement_test_readiness_self_test",
      "status": "pass",
      "stderr": "",
      "stdout": "{\n  \"app_version\": \"riftscan-movement-test-readiness-v1.0.0\",\n  \"case_count\": 5,\n  \"created_utc\": \"2026-05-07T18:11:14Z\",\n  \"safety\": {\n    \"capture_started\": false,\n    \"memory_scan_or_read_started\": false,\n    \"movement_or_input_sent\": false,\n    \"reloadui_sent\": false,\n    \"runs_focus_preflight\": false,\n    \"writes_current_artifacts\": false\n  },\n  \"schema_version\": \"riftscan.movement_test_readiness_self_test.v1\",\n  \"status\": \"PASS\",\n  \"tests\": [\n    {\n      \"actual_display_status\": \"PASS\",\n      \"actual_status\": \"pass\",\n      \"blockers\": [],\n      \"expected_display_status\": \"PASS\",\n      \"expected_status\": \"pass\",\n      \"name\": \"all readiness inputs pass\",\n      \"pass\": true\n    },\n    {\n      \"actual_display_status\": \"BLOCKED\",\n      \"actual_status\": \"blocked_movement_test_not_ready\",\n      \"blockers\": [\n        \"Operator metadata_capture_plan_gate is not PASS.\"\n      ],\n      \"expected_display_status\": \"BLOCKED\",\n      \"expected_status\": \"blocked_movement_test_not_ready\",\n      \"name\": \"blocked operator gate\",\n      \"pass\": true\n    },\n    {\n      \"actual_display_status\": \"BLOCKED\",\n      \"actual_status\": \"blocked_movement_test_not_ready\",\n      \"blockers\": [\n        \"Capture Plan Check is not PASS.\"\n      ],\n      \"expected_display_status\": \"BLOCKED\",\n      \"expected_status\": \"blocked_movement_test_not_ready\",\n      \"name\": \"blocked capture plan check\",\n      \"pass\": true\n    },\n    {\n      \"actual_display_status\": \"BLOCKED\",\n      \"actual_status\": \"blocked_movement_test_not_ready\",\n      \"blockers\": [\n        \"Movement live-test wrapper is missing required guard features: move_forward stimulus, pre-capture wait, ReaderBridge freshness, RiftReader anchor read, RiftScan passive capture, delta summary, movement-proof interpretation.\"\n      ],\n      \"expected_display_status\": \"BLOCKED\",\n      \"expected_status\": \"blocked_movement_test_not_ready\",\n      \"name\": \"blocked wrapper missing movement support\",\n      \"pass\": true\n    },\n    {\n      \"actual_display_status\": \"BLOCKED\",\n      \"actual_status\": \"blocked_movement_test_not_ready\",\n      \"blockers\": [\n        \"RiftReader run-reader.cmd is missing; movement wrapper cannot refresh proof-grade coordinate anchors.\"\n      ],\n      \"expected_display_status\": \"BLOCKED\",\n      \"expected_status\": \"blocked_movement_test_not_ready\",\n      \"name\": \"blocked missing RiftReader\",\n      \"pass\": true\n    }\n  ]\n}\n"
    },
    {
      "args": [
        "C:\\Users\\mrkoo\\AppData\\Local\\Programs\\Python\\Python314\\python.exe",
        "tools/riftscan_movement_execution_gate.py",
        "--self-test"
      ],
      "exit_code": 0,
      "name": "movement_execution_gate_self_test",
      "status": "pass",
      "stderr": "",
      "stdout": "{\n  \"app_version\": \"riftscan-movement-execution-gate-v1.0.0\",\n  \"case_count\": 5,\n  \"created_utc\": \"2026-05-07T18:11:15Z\",\n  \"safety\": {\n    \"capture_started\": false,\n    \"memory_scan_or_read_started_by_riftscan\": false,\n    \"movement_or_input_sent\": false,\n    \"reloadui_sent\": false,\n    \"writes_current_artifacts\": false\n  },\n  \"schema_version\": \"riftscan.movement_execution_gate_self_test.v1\",\n  \"status\": \"PASS\",\n  \"tests\": [\n    {\n      \"actual\": \"pass\",\n      \"blockers\": [],\n      \"expected\": \"pass\",\n      \"name\": \"pass\",\n      \"pass\": true\n    },\n    {\n      \"actual\": \"blocked_movement_execution_not_allowed\",\n      \"blockers\": [\n        \"Movement Test Readiness is not PASS.\"\n      ],\n      \"expected\": \"blocked_movement_execution_not_allowed\",\n      \"name\": \"blocked readiness\",\n      \"pass\": true\n    },\n    {\n      \"actual\": \"blocked_movement_execution_not_allowed\",\n      \"blockers\": [\n        \"Focus summary status is not foreground_verified.\",\n        \"Focus summary selected_window is missing.\"\n      ],\n      \"expected\": \"blocked_movement_execution_not_allowed\",\n      \"name\": \"blocked focus\",\n      \"pass\": true\n    },\n    {\n      \"actual\": \"blocked_movement_execution_not_allowed\",\n      \"blockers\": [\n        \"live-test-riftscan preflight failed for move_forward.\"\n      ],\n      \"expected\": \"blocked_movement_execution_not_allowed\",\n      \"name\": \"blocked wrapper\",\n      \"pass\": true\n    },\n    {\n      \"actual\": \"blocked_movement_execution_not_allowed\",\n      \"blockers\": [],\n      \"expected\": \"blocked_movement_execution_not_allowed\",\n      \"name\": \"blocked skipped wrapper\",\n      \"pass\": true\n    }\n  ]\n}\n"
    },
    {
      "args": [
        "C:\\Users\\mrkoo\\AppData\\Local\\Programs\\Python\\Python314\\python.exe",
        "tools/riftscan_discovery_ledger.py",
        "--self-test"
      ],
      "exit_code": 0,
      "name": "discovery_ledger_self_test",
      "status": "pass",
      "stderr": "",
      "stdout": "{\n  \"app_version\": \"riftscan-discovery-ledger-v1.2.0\",\n  \"contract_validation_issue_count\": 0,\n  \"created_utc\": \"2026-05-07T18:11:15Z\",\n  \"failures\": [],\n  \"safety\": {\n    \"capture_started\": false,\n    \"memory_scan_or_read_started\": false,\n    \"movement_or_input_sent\": false,\n    \"reloadui_sent\": false,\n    \"riftreader_command_executed\": false,\n    \"runs_focus_preflight\": false,\n    \"writes_artifacts\": false\n  },\n  \"schema_version\": \"riftscan.discovery_ledger.self_test.v1\",\n  \"status\": \"PASS\"\n}\n"
    },
    {
      "args": [
        "C:\\Users\\mrkoo\\AppData\\Local\\Programs\\Python\\Python314\\python.exe",
        "tools/riftscan_discovery_ledger.py"
      ],
      "exit_code": 0,
      "name": "discovery_ledger_refresh",
      "status": "pass",
      "stderr": "",
      "stdout": "RIFTSCAN DISCOVERY LEDGER: handoffs/current/discovery-ledger/DISCOVERY_LEDGER_REPORT.md\nSummary: handoffs/current/discovery-ledger/discovery-ledger-summary.json\nCandidate ledger: handoffs/current/discovery-ledger/candidate_ledger.jsonl\nSafety: offline artifact inventory only; no focus, capture, input, movement, memory read, RiftReader command, or /reloadui was run.\n"
    },
    {
      "args": [
        "C:\\Users\\mrkoo\\AppData\\Local\\Programs\\Python\\Python314\\python.exe",
        "tools/riftscan_discovery_ledger.py",
        "--validate-existing"
      ],
      "exit_code": 0,
      "name": "discovery_ledger_validate_existing",
      "status": "pass",
      "stderr": "",
      "stdout": "{\n  \"app_version\": \"riftscan-discovery-ledger-v1.2.0\",\n  \"candidate_count\": 3,\n  \"created_utc\": \"2026-05-07T18:11:15Z\",\n  \"display_status\": \"PASS\",\n  \"error_count\": 0,\n  \"issues\": [],\n  \"line_count\": 3,\n  \"path\": \"handoffs/current/discovery-ledger/candidate_ledger.jsonl\",\n  \"safety\": {\n    \"capture_started\": false,\n    \"ledger_live_movement_authorized\": false,\n    \"memory_scan_or_read_started\": false,\n    \"movement_or_input_sent\": false,\n    \"offline_only\": true,\n    \"process_attach_or_memory_read_started\": false,\n    \"reloadui_sent\": false,\n    \"riftreader_command_executed\": false,\n    \"runs_focus_preflight\": false,\n    \"writes_artifacts\": false\n  },\n  \"schema_version\": \"riftscan.discovery_ledger_validation.v1\",\n  \"status\": \"PASS\",\n  \"warning_count\": 0\n}\n"
    },
    {
      "args": [
        "C:\\Users\\mrkoo\\AppData\\Local\\Programs\\Python\\Python314\\python.exe",
        "tools/riftscan_candidate_ledger_consumer.py",
        "--self-test"
      ],
      "exit_code": 0,
      "name": "candidate_ledger_consumer_self_test",
      "status": "pass",
      "stderr": "",
      "stdout": "{\n  \"app_version\": \"riftscan-candidate-ledger-consumer-v1.1.0\",\n  \"created_utc\": \"2026-05-07T18:11:15Z\",\n  \"failures\": [],\n  \"safety\": {\n    \"capture_started\": false,\n    \"memory_scan_or_read_started\": false,\n    \"movement_or_input_sent\": false,\n    \"process_attach_or_memory_read_started\": false,\n    \"reloadui_sent\": false,\n    \"riftreader_command_executed\": false,\n    \"runs_focus_preflight\": false,\n    \"writes_artifacts\": false\n  },\n  \"schema_version\": \"riftscan.candidate_ledger_consumer.self_test.v1\",\n  \"status\": \"PASS\"\n}\n"
    },
    {
      "args": [
        "C:\\Users\\mrkoo\\AppData\\Local\\Programs\\Python\\Python314\\python.exe",
        "tools/riftscan_candidate_ledger_consumer.py",
        "--strict-exit-code"
      ],
      "exit_code": 0,
      "name": "candidate_ledger_consumer_refresh",
      "status": "pass",
      "stderr": "",
      "stdout": "RIFTSCAN CANDIDATE LEDGER CONSUMER: handoffs/current/candidate-ledger-consumer/CANDIDATE_LEDGER_CONSUMER_REPORT.md\nSummary: handoffs/current/candidate-ledger-consumer/candidate-ledger-consumer-summary.json\nStatus: PASS\nSafety: offline consumer only; no focus, capture, input, movement, memory read, RiftReader command, offset validation, or /reloadui was run.\n"
    },
    {
      "args": [
        "C:\\Users\\mrkoo\\AppData\\Local\\Programs\\Python\\Python314\\python.exe",
        "tools/riftscan_ai_workflow_packet.py",
        "--self-test"
      ],
      "exit_code": 0,
      "name": "ai_workflow_packet_self_test",
      "status": "pass",
      "stderr": "",
      "stdout": "{\n  \"app_version\": \"riftscan-ai-workflow-packet-v1.10.0\",\n  \"created_utc\": \"2026-05-07T18:11:15Z\",\n  \"failures\": [],\n  \"safety\": {\n    \"capture_started\": false,\n    \"memory_scan_or_read_started\": false,\n    \"movement_or_input_sent\": false,\n    \"process_attach_or_memory_read_started\": false,\n    \"reloadui_sent\": false,\n    \"riftreader_command_executed\": false,\n    \"runs_focus_preflight\": false,\n    \"writes_artifacts\": false\n  },\n  \"schema_version\": \"riftscan.ai_workflow_packet.self_test.v1\",\n  \"status\": \"PASS\"\n}\n"
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
      "stdout": "{\n  \"app_version\": \"riftscan-patch-intake-v1.2.5\",\n  \"created_utc\": \"2026-05-07T18:11:20Z\",\n  \"schema_version\": \"riftscan.patch_intake_self_test.v1\",\n  \"status\": \"PASS\",\n  \"tests\": [\n    {\n      \"actual\": \"FAIL_BAD_HEADER\",\n      \"expected\": \"FAIL_BAD_HEADER\",\n      \"issues\": [\n        \"Payload must start with RIFTSCAN_CLIPBOARD_PATCH_V1 or RIFTSCAN_CHUNKED_PATCH_V1.\"\n      ],\n      \"name\": \"empty payload\",\n      \"pass\": true\n    },\n    {\n      \"actual\": \"FAIL_BAD_HEADER\",\n      \"expected\": \"FAIL_BAD_HEADER\",\n      \"issues\": [\n        \"Payload must start with RIFTSCAN_CLIPBOARD_PATCH_V1 or RIFTSCAN_CHUNKED_PATCH_V1.\"\n      ],\n      \"name\": \"wrong header\",\n      \"pass\": true\n    },\n    {\n      \"actual\": \"FAIL_BAD_MANIFEST\",\n      \"expected\": \"FAIL_BAD_MANIFEST\",\n      \"issues\": [\n        \"JSONDecodeError: Expecting property name enclosed in double quotes: line 1 column 2 (char 1)\"\n      ],\n      \"name\": \"bad json\",\n      \"pass\": true\n    },\n    {\n      \"actual\": \"FAIL_MISSING_PAYLOAD\",\n      \"expected\": \"FAIL_MISSING_PAYLOAD\",\n      \"issues\": [\n        \"Payload block markers are missing.\"\n      ],\n      \"name\": \"missing payload\",\n      \"pass\": true\n    },\n    {\n      \"actual\": \"FAIL_HASH_MISMATCH\",\n      \"expected\": \"FAIL_HASH_MISMATCH\",\n      \"issues\": [],\n      \"name\": \"hash mismatch\",\n      \"pass\": true\n    },\n    {\n      \"actual\": \"FAIL_STALE_PATCH\",\n      \"expected\": \"FAIL_STALE_PATCH\",\n      \"issues\": [\n        \"Patch timestamp is not newer than last accepted patch.\"\n      ],\n      \"name\": \"stale timestamp\",\n      \"pass\": true\n    },\n    {\n      \"actual\": \"FAIL_WRONG_REPO\",\n      \"expected\": \"FAIL_WRONG_REPO\",\n      \"issues\": [\n        \"target_repo_root does not match selected repo root.\"\n      ],\n      \"name\": \"wrong repo\",\n      \"pass\": true\n    },\n    {\n      \"actual\": \"PASS_DRY_RUN\",\n      \"expected\": \"PASS_DRY_RUN\",\n      \"issues\": [],\n      \"name\": \"valid dry run\",\n      \"pass\": true\n    },\n    {\n      \"actual\": \"FAIL_COMMIT_WITHOUT_APPLY\",\n      \"expected\": \"FAIL_COMMIT_WITHOUT_APPLY\",\n      \"issues\": [\n        \"No successful process/apply result exists.\"\n      ],\n      \"name\": \"commit without apply\",\n      \"pass\": true\n    },\n    {\n      \"actual\": \"PASS_PROCESSED\",\n      \"expected\": \"PASS_PROCESSED\",\n      \"issues\": [],\n      \"name\": \"process payload without commit metadata\",\n      \"pass\": true\n    },\n    {\n      \"actual\": \"FAIL_COMMIT_MISSING_METADATA\",\n      \"expected\": \"FAIL_COMMIT_MISSING_METADATA\",\n      \"issues\": [\n        \"Manifest commit block is required.\"\n      ],\n      \"name\": \"commit missing metadata\",\n      \"pass\": true\n    },\n    {\n      \"actual\": \"FAIL_COMMIT_UNSAFE_STAGE_PATH\",\n      \"expected\": \"FAIL_COMMIT_UNSAFE_STAGE_PATH\",\n      \"issues\": [\n        \"Unsafe commit.stage_paths entry: .\"\n      ],\n      \"name\": \"unsafe commit stage path validation\",\n      \"pass\": true\n    },\n    {\n      \"actual\": \"PASS_PROCESSED\",\n      \"expected\": \"PASS_PROCESSED\",\n      \"issues\": [],\n      \"name\": \"process payload with capture readiness checks\",\n      \"pass\": true\n    },\n    {\n      \"actual\": \"PASS_PROCESSED\",\n      \"expected\": \"PASS_PROCESSED\",\n      \"issues\": [],\n      \"name\": \"process payload\",\n      \"pass\": true\n    },\n    {\n      \"actual\": \"PASS_COMMITTED\",\n      \"expected\": \"PASS_COMMITTED\",\n      \"issues\": [],\n      \"name\": \"commit in temp repo\",\n      \"pass\": true\n    },\n    {\n      \"actual\": \"PASS_PUSH_VERIFY_SIMULATED_OR_SKIPPED\",\n      \"expected\": \"PASS_PUSH_VERIFY_SIMULATED_OR_SKIPPED\",\n      \"issues\": [],\n      \"name\": \"push verify simulated\",\n      \"pass\": true\n    },\n    {\n      \"actual\": \"PASS_DRY_RUN\",\n      \"expected\": \"PASS_DRY_RUN\",\n      \"issues\": [],\n      \"name\": \"chunked dry run\",\n      \"pass\": true\n    },\n    {\n      \"actual\": \"FAIL_CHUNK_HASH_MISMATCH\",\n      \"expected\": \"FAIL_CHUNK_HASH_MISMATCH\",\n      \"issues\": [\n        \"chunk 1 hash mismatch\"\n      ],\n      \"name\": \"chunked bad chunk hash\",\n      \"pass\": true\n    },\n    {\n      \"actual\": \"FAIL_MISSING_CHUNK\",\n      \"expected\": \"FAIL_MISSING_CHUNK\",\n      \"issues\": [\n        \"missing chunks: 1\"\n      ],\n      \"name\": \"chunked missing chunk\",\n      \"pass\": true\n    }\n  ]\n}\n"
    },
    {
      "args": [],
      "errors": [],
      "exit_code": 0,
      "name": "ai_workflow_packet_contract",
      "required_field_count": 16,
      "schema_doc_path": "docs/ai-workflow-packet-schema.md",
      "status": "pass",
      "stderr": "",
      "stdout": "checked_fields=16\n",
      "summary_path": "handoffs/current/ai-workflow/ai-workflow-summary.json"
    }
  ],
  "created_utc": "2026-05-07T18:11:20Z",
  "display_status": "PASS",
  "failed_check_count": 0,
  "failed_checks": [],
  "git": {
    "command_status": {
      "head": "pass",
      "log": "pass",
      "status": "pass"
    },
    "head": "60a8e5b1066a4358a91ff0df77eb8604e93f965f",
    "log_oneline_5": "60a8e5b Add AI packet history index view\naad6f11 Validate full AI packet history index\n237d9f2 Index AI workflow packet history\nd0d0e92 Validate AI packet archive offline\n6bef855 Archive AI workflow packet history",
    "status_short": " M docs/ai-workflow-packet-schema.md\n M docs/helper-tooling-policy.md\n M handoffs/current/ai-workflow/AI_WORKFLOW_PACKET.md\n M handoffs/current/ai-workflow/ai-workflow-log.jsonl\n M handoffs/current/ai-workflow/ai-workflow-summary.json\n M handoffs/current/ai-workflow/history/index.jsonl\n M handoffs/current/candidate-ledger-consumer/CANDIDATE_LEDGER_CONSUMER_REPORT.md\n M handoffs/current/candidate-ledger-consumer/candidate-ledger-consumer-log.jsonl\n M handoffs/current/candidate-ledger-consumer/candidate-ledger-consumer-summary.json\n M handoffs/current/discovery-ledger/DISCOVERY_LEDGER_REPORT.md\n M handoffs/current/discovery-ledger/discovery-ledger-log.jsonl\n M handoffs/current/discovery-ledger/discovery-ledger-summary.json\n M handoffs/current/offline-workflow-check/OFFLINE_WORKFLOW_CHECK_REPORT.md\n M handoffs/current/offline-workflow-check/offline-workflow-check-log.jsonl\n M handoffs/current/offline-workflow-check/offline-workflow-check-summary.json\n M tools/riftscan_ai_workflow_packet.py\n M tools/riftscan_offline_workflow_check.py\n?? handoffs/current/ai-workflow/AI_WORKFLOW_HISTORY_INDEX_REPORT.md\n?? handoffs/current/ai-workflow/ai-workflow-history-index-summary.json\n?? handoffs/current/ai-workflow/history/AI_WORKFLOW_PACKET-2026-05-07T18-03-05Z-riftscan-ai-workflow-packet-v1-9-0.md\n?? handoffs/current/ai-workflow/history/ai-workflow-summary-2026-05-07T18-03-05Z-riftscan-ai-workflow-packet-v1-9-0.json\n"
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
    "riftreader_command_executed": false,
    "riftreader_validation_started": false
  },
  "schema_version": "riftscan.offline_workflow_check.v1",
  "status": "pass"
}
```

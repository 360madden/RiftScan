# RiftScan Patch Intake Report

Created UTC: `2026-05-04T21:26:38Z`

Kind: `apply`

```json
{
  "app_version": "riftscan-patch-intake-v1.0.0",
  "applier_path": ".riftscan-local/patch-intake/staging/patch-intake-helper-v1-1-0-20260504/apply_patch_intake_helper_v1_1_0.py",
  "apply_exit_code": 0,
  "apply_stderr": "",
  "apply_stdout": "{\n  \"app_version\": \"riftscan-patch-intake-v1.1.0\",\n  \"apply_result\": {\n    \"code\": \"PASS_APPLIED\",\n    \"required_result_markers\": [\n      \"riftscan-patch-intake-v1.1.0\",\n      \"LAST_PROCESS_JSON\",\n      \"PATCH_INTAKE_LOG_JSONL\",\n      \"def process_payload_text\",\n      \"def commit_result\",\n      \"def push_verify_remote\",\n      \"Process Payload\",\n      \"Commit Result\",\n      \"Push + Verify Remote\"\n    ],\n    \"status\": \"pass\",\n    \"target_file\": \"tools/riftscan_patch_intake_app.py\"\n  },\n  \"artifact_paths\": [\n    \"tools/riftscan_patch_intake_app.py\",\n    \"scripts/riftscan-patch-intake.cmd\",\n    \"handoffs/current/patch-intake/last-process-result.json\",\n    \"handoffs/current/patch-intake/patch-intake-log.jsonl\"\n  ],\n  \"code\": \"PASS_PROCESSED\",\n  \"created_utc\": \"2026-05-04T21:26:38Z\",\n  \"manifest\": {\n    \"applier\": \"apply_patch_intake_helper_v1_1_0.py\",\n    \"commit\": {\n      \"message\": \"Implement patch intake helper v1.1\",\n      \"push\": false,\n      \"stage_paths\": [\n        \"tools/riftscan_patch_intake_app.py\",\n        \"scripts/riftscan-patch-intake.cmd\",\n        \"handoffs/current/patch-intake/\"\n      ]\n    },\n    \"component\": \"patch-intake-helper\",\n    \"created_utc\": \"2026-05-04T21:10:07Z\",\n    \"forbidden_actions\": [\n      \"auto_commit\",\n      \"auto_push\",\n      \"force_push\",\n      \"git_add_dot\",\n      \"listener\",\n      \"polling\",\n      \"scheduled_task\",\n      \"service\"\n    ],\n    \"from_version\": \"riftscan-patch-intake-v1.0.0\",\n    \"magic\": \"RIFTSCAN_CLIPBOARD_PATCH_V1\",\n    \"package_id\": \"patch-intake-helper-v1-1-0-20260504\",\n    \"payload_type\": \"python_applier_base64_gzip\",\n    \"post_apply_checks\": [\n      \"py_compile_patch_intake\",\n      \"git_status_check\"\n    ],\n    \"repo\": \"RiftScan\",\n    \"required_existing_markers\": [\n      \"riftscan-patch-intake-v1.0.0\",\n      \"def apply_payload_text\",\n      \"class PatchIntakeApp\",\n      \"Run Self-Test\"\n    ],\n    \"required_result_markers\": [\n      \"riftscan-patch-intake-v1.1.0\",\n      \"LAST_PROCESS_JSON\",\n      \"PATCH_INTAKE_LOG_JSONL\",\n      \"def process_payload_text\",\n      \"def commit_result\",\n      \"def push_verify_remote\",\n      \"Process Payload\",\n      \"Commit Result\",\n      \"Push + Verify Remote\"\n    ],\n    \"schema_version\": \"riftscan.clipboard_patch.v1\",\n    \"target_file\": \"tools/riftscan_patch_intake_app.py\",\n    \"target_repo_root\": \"C:\\\\RIFT MODDING\\\\Riftscan\",\n    \"to_version\": \"riftscan-patch-intake-v1.1.0\"\n  },\n  \"mode\": \"process\",\n  \"notes\": [\n    \"Bootstrap artifact written by the v1.1.0 applier because the pre-upgrade v1.0.0 helper does not know last-process-result.json yet.\",\n    \"Use the reopened v1.1.0 helper's Commit Result button for explicit commit control.\"\n  ],\n  \"package_id\": \"patch-intake-helper-v1-1-0-20260504\",\n  \"schema_version\": \"riftscan.patch_intake_process_result.v1\",\n  \"stage\": \"applier_bootstrap_process_result\",\n  \"status\": \"pass\",\n  \"target_file\": \"tools/riftscan_patch_intake_app.py\"\n}\n",
  "code": "PASS_APPLIED",
  "compile_exit_code": 0,
  "compile_stderr": "",
  "compile_stdout": "",
  "created_utc": "2026-05-04T21:26:37Z",
  "from_version": "riftscan-patch-intake-v1.0.0",
  "git_status_short": " M handoffs/current/patch-intake/PATCH_INTAKE_REPORT.md\n M handoffs/current/patch-intake/last-dry-run-result.json\n M handoffs/current/patch-intake/last-validation-result.json\n",
  "issues": [],
  "mode": "apply",
  "package_id": "patch-intake-helper-v1-1-0-20260504",
  "payload_sha256": "b674d2f8feae66825e043b1a8a192e4d039ec7b94f880e621cb9df64fd48564a",
  "payload_type": "python_applier_base64_gzip",
  "repo_root": "C:\\RIFT MODDING\\Riftscan",
  "report_path": "handoffs/current/patch-intake/PATCH_INTAKE_REPORT.md",
  "schema_version": "riftscan.patch_intake_result.v1",
  "status": "pass",
  "target_file": "tools/riftscan_patch_intake_app.py",
  "target_repo_root": "C:\\RIFT MODDING\\Riftscan",
  "to_version": "riftscan-patch-intake-v1.1.0",
  "warnings": []
}
```

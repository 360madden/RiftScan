# RiftScan AGENTS.md
# YAML-shaped Markdown. Keep this file strict, short, and authoritative.

riftscan_agent_contract:
  metadata:
    document_name: AGENTS.md
    document_version: "0.7.1"
    document_character_count: "027056"
    project: RiftScan
    project_stream: RiftScan01
    purpose: root_machine_readable_contract_for_coding_agents
    primary_goal: fastest_efficient_truth_discovery_for_rift_mmo
    optimization_metric: information_gain_per_memory_read
    execution_style: deterministic_engineering
    default_os: windows
    default_runtime: dotnet_10_lts
    default_language: csharp

  authority:
    role: root_project_instruction
    applies_to:
      - repository_structure
      - implementation_strategy
      - capture_pipeline
      - analyzer_pipeline
      - session_artifacts
      - tests
      - reports
      - completion_claims
    precedence:
      1: this_file
      2: explicit_user_task
      3: repository_docs
      4: local_code_comments
    must:
      - preserve_replayable_session_artifacts
      - prefer_existing_architecture_over_new_helpers
      - implement_small_vertical_slices
      - keep_live_capture_low_pressure
      - keep_expensive_analysis_offline
      - emit_machine_readable_outputs
      - run_available_build_tests_and_format_checks_before_claiming_completion
    must_not:
      - bypass_this_contract_for_speed
      - create_duplicate_single_purpose_scanners
      - claim_discovery_without_artifact_evidence
      - delete_or_overwrite_raw_capture_evidence_to_hide_failed_attempts
      - mix_launcher_input_or_window_control_into_scanner_core
      - handle_credentials_tokens_or_launcher_auth_material

  model_routing:
    purpose: conserve_quota_without_risking_truth_or_integrity
    default:
      low_risk_bounded_work: spark
      medium_or_high_risk_work: stronger_model
    spark_use:
      rule: actively_use_spark_when_task_is_low_risk_bounded_and_reversible
      allowed:
        - read_only_repo_orientation
        - docs_status_summaries
        - simple_file_inventory_status_tables
        - formatting_only_doc_cleanup
        - fixture_review_without_verifier_behavior_changes
    stronger_model_required:
      rule: use_stronger_model_for_correctness_critical_or_behavior_affecting_work
      required_for:
        - live_process_capture_or_process_attachment
        - memory_reader_logic
        - checksum_or_session_integrity_logic
        - cli_behavior_changes
        - analyzer_scoring_or_truth_claim_logic
        - architecture_boundary_decisions
        - live_rift_interaction
    escalation:
      spark_must_stop_and_escalate_on:
        - ambiguity
        - failing_tests
        - behavior_impacting_changes
        - uncertainty
      rule: spark_must_not_guess_or_patch_around_risk
  project_identity:
    name: RiftScan
    stream: RiftScan01
    type: standalone_behavioral_memory_discovery_engine
    primary_domain: RIFT_MMO_client_memory_analysis
    secondary_domain: reusable_process_memory_snapshot_and_offline_analysis_engine
    not_a:
      - bot
      - input_broadcaster
      - launcher_automation_tool
      - one_offset_scanner
      - memory_editor
      - injection_framework
    thesis:
      - memory_contains_truth
      - behavior_over_time_reveals_truth_better_than_single_values
      - capture_once_analyze_many
      - entity_layouts_are_more_durable_than_entity_instances
      - passive_world_activity_is_signal
      - addon_truth_validates_candidates_but_does_not_replace_memory_discovery

  scope_boundary:
    scanner_mode: external_read_only_observation_and_offline_analysis
    in_scope:
      - read_only_process_discovery
      - read_only_region_enumeration
      - selected_region_snapshot_capture
      - immutable_or_append_only_session_storage
      - offline_delta_entropy_float_density_cluster_structure_behavior_analysis
      - optional_rift_adapter_validation_from_addon_truth_or_stimulus_labels
      - reproducible_candidate_ranking
      - cross_session_layout_recovery
    out_of_scope_for_core:
      - target_process_memory_writes
      - target_process_patching
      - code_execution_inside_target_process
      - launcher_auth_or_credential_handling
      - input_broadcasting
      - ui_coordinate_clicking
      - one_off_yaw_position_or_camera_scanners
    boundary_rule: if_the_feature_controls_the_game_or_launcher_it_does_not_belong_in_riftscan_core

  priority_stack:
    P0: preserve_data_process_stability_and_evidence
    P1: fastest_correct_truth_discovery
    P2: maximize_information_gain_per_memory_read
    P3: capture_once_analyze_many
    P4: offline_analysis_over_live_analysis
    P5: entity_structure_first_player_identity_last
    P6: generic_core_with_rift_adapter
    P7: deterministic_reproducible_results
    P8: clean_cli_testable_code_clear_reports
    correct_speed:
      - fewer_live_reads
      - fewer_repeated_scans
      - more_reuse_of_existing_snapshots
      - earlier_elimination_of_noise
      - clearer_next_capture_recommendations
    false_speed:
      - player_first_address_hunting
      - repeated_live_rescan_per_question
      - pointer_chasing_before_region_triage
      - guessing_from_single_snapshot
      - advanced_analyzers_before_valid_fixture_session

  ambiguity_resolution:
    order:
      1: preserve_raw_data_and_session_integrity
      2: preserve_replayability
      3: minimize_live_process_pressure
      4: maximize_information_gain
      5: keep_core_generic
      6: keep_rift_logic_in_adapter_or_analyzer
      7: add_test_or_validator_before_expanding_scope
    if_uncertain:
      - do_not_guess_truth
      - encode_assumption_in_report
      - mark_candidate_unverified
      - recommend_next_smallest_validation_step

  first_implementation_order:
    rule: do_not_skip_earlier_items_unless_working_equivalent_already_exists
    steps:
      1: create_solution_and_projects
      2: define_session_schema_models
      3: implement_verify_command_for_schema_and_checksums
      4: add_fixture_session_tests_not_requiring_RIFT
      5: implement_read_only_process_discovery_and_region_enumeration
      6: implement_passive_capture_to_sessions_directory
      7: implement_offline_dynamic_region_triage
      8: implement_report_generation_from_stored_artifacts
      9: implement_cluster_and_structure_detection
      10: implement_optional_rift_adapter_validation
      11: implement_player_match_after_structure_candidates_exist
      12: implement_actor_camera_signal_separation
      13: implement_cross_session_recovery
    early_stop_rule: finish_the_earliest_unfinished_vertical_slice_then_report_risks

  optimization_gate:
    do_not_optimize_pipeline_until:
      - fixture_session_or_baseline_capture_exists
      - verify_command_passes_on_at_least_one_session
      - report_generated_from_stored_snapshots
      - build_and_tests_run_or_unavailable_reason_reported
    allowed_before_gate:
      - clarify_schema
      - improve_error_messages
      - improve_tests
      - remove_obvious_duplicate_code

  architecture:
    src/RiftScan.Core:
      purpose: generic_read_only_process_memory_snapshot_engine
      allowed:
        - process_discovery
        - read_only_attach_abstraction
        - virtual_region_enumeration
        - region_filtering
        - batched_region_reading
        - snapshot_serialization
        - compression_abstraction
        - checksums
        - cancellation_tokens
        - structured_logging_interfaces
      forbidden:
        - rift_position_yaw_camera_logic
        - candidate_scoring
        - addon_export_dependency
        - launcher_input_or_window_control
    src/RiftScan.Capture:
      purpose: live_capture_orchestration_only
      allowed:
        - passive_sweep
        - capture_batch
        - region_priority_map
        - capture_budgeting
        - session_manifest_generation
        - crash_safe_partial_session_handling
      forbidden:
        - expensive_candidate_analysis
        - pointer_chasing
        - gameplay_semantics
        - candidate_scoring
    src/RiftScan.Analysis:
      purpose: offline_analysis_only
      allowed:
        - delta_analysis
        - stability_analysis
        - entropy_analysis
        - float_density_analysis
        - sequential_analysis
        - cluster_detection
        - structure_extraction
        - candidate_scoring
        - cross_session_comparison
      forbidden:
        - process_attach
        - live_memory_reads
        - launcher_input_or_window_control
    src/RiftScan.Rift:
      purpose: rift_specific_adapter_and_behavior_models
      allowed:
        - addon_truth_loader
        - rift_stimulus_labels
        - rift_motion_models
        - actor_vs_camera_signal_expectations
        - player_match_validation
      forbidden:
        - generic_memory_reader_implementation
        - live_capture_implementation
    src/RiftScan.Cli:
      purpose: stable_command_line_surface
      allowed:
        - capture_commands
        - analyze_commands
        - report_commands
        - verify_commands
        - compare_commands
        - migration_commands
      forbidden:
        - hidden_side_effects
        - undocumented_output_formats
        - commands_requiring_ui_clicking
    tests:
      purpose: deterministic_validation_without_RIFT_for_core_analysis
      required:
        - schema_tests
        - region_filter_tests
        - snapshot_roundtrip_tests
        - checksum_tests
        - analyzer_determinism_tests
        - report_generation_tests
    sessions:
      purpose: immutable_or_append_only_capture_artifacts
      may_contain:
        - manifests
        - region_maps
        - module_maps
        - snapshot_bins
        - jsonl_indexes
        - stimuli
        - addon_truth
        - reports
        - candidates
      must_not_contain:
        - credentials
        - tokens
        - launcher_auth_material
    tools:
      purpose: developer_utilities_only
      allowed:
        - schema_validator
        - report_viewer
        - benchmark_runner
        - session_compactor
      forbidden:
        - duplicate_scanners
        - one_off_yaw_position_or_camera_scanners

  windows_api_boundary:
    allowed_process_access_intent:
      - PROCESS_QUERY_INFORMATION
      - PROCESS_VM_READ
    allowed_patterns:
      - OpenProcess_for_query_and_read
      - VirtualQueryEx_for_region_enumeration
      - ReadProcessMemory_for_selected_regions
      - QueryFullProcessImageName_or_equivalent_identity_check
    forbidden_in_core:
      - WriteProcessMemory
      - CreateRemoteThread
      - QueueUserAPC_into_target
      - SetWindowsHookEx_for_target_control
      - patching_target_code_or_data
    rule: platform_calls_must_be_wrapped_so_analysis_and_tests_can_run_without_live_process_access

  execution_pipeline:
    invariant_order:
      1: PassiveSweep
      2: DynamicRegionTriage
      3: RegionBudgetSelection
      4: ClusterDetection
      5: StructureExtraction
      6: CandidateScoring
      7: OptionalStimulusFilter
      8: PlayerIdentification
      9: CrossSessionValidation
      10: ReportGeneration
    hard_gates:
      - passive_sweep_before_stimulus_filter
      - capture_before_analysis
      - stored_snapshots_before_reports
      - cluster_detection_before_player_identification
      - player_identification_only_after_entity_structure_candidates_exist
      - offline_analysis_must_not_require_RIFT_running

  capture_contract:
    modes:
      passive_idle:
        required: true
        default_samples: 50
        default_interval_ms: 100
        primary_signal: natural_client_and_world_activity
      passive_world_activity:
        required: recommended
        default_samples: 100
        default_interval_ms: 100
        primary_signal: npc_other_player_environment_motion
      optional_move_forward:
        required: false
        rule: one_primary_signal_per_batch
      optional_turn_left:
        required: false
        rule: one_primary_signal_per_batch
      optional_turn_right:
        required: false
        rule: one_primary_signal_per_batch
      optional_camera_only:
        required: false
        rule: avoid_player_translation_if_possible
    defaults:
      process_candidates:
        - rift_x64
        - rift_x64.exe
      full_process_dump: false
      compression: zstd_or_lz4_if_available_else_none
      checksum: xxhash64_or_sha256
      min_samples: 20
      default_samples: 50
      max_samples_without_explicit_flag: 100
      default_interval_ms: 100
      min_interval_ms_without_explicit_flag: 50
      max_raw_bytes_per_session_without_explicit_flag: 8GiB
    region_policy:
      include_if:
        - MEM_COMMIT
        - readable
        - not_guard_page
        - not_noaccess
      prioritize_if:
        - read_success_rate_high
        - change_rate_nonzero
        - float_density_high
        - repeated_layout_density_high
        - entity_pool_candidate
        - heap_like_region
        - stable_dynamic_across_snapshots
      deprioritize_if:
        - static_after_sufficient_baseline
        - entropy_too_high_random_noise
        - executable_code_section
        - ui_texture_or_asset_buffer_candidate
        - repeatedly_unreadable
        - byte_cost_high_signal_low
      never_default:
        - full_process_dump
        - all_regions_every_sample_forever
        - high_frequency_reads_of_low_signal_regions

  session_artifact_contract:
    root: sessions/<session_id>
    raw_data_policy: immutable_or_append_only_after_write
    required_files:
      - manifest.json
      - regions.json
      - modules.json
      - snapshots/index.jsonl
      - snapshots/*.bin
      - checksums.json
    generated_files:
      - triage.jsonl
      - clusters.jsonl
      - structures.jsonl
      - candidates.jsonl
      - report.md
      - next_capture_plan.json
    optional_files:
      - stimuli.jsonl
      - addon_truth.jsonl
      - notes.md
    required_manifest_fields:
      - schema_version
      - session_id
      - project_version
      - created_utc
      - machine_name
      - os_version
      - process_name
      - process_id
      - process_start_time_utc
      - capture_mode
      - snapshot_count
      - region_count
      - total_bytes_raw
      - total_bytes_stored
      - compression
      - checksum_algorithm
      - status
    required_candidate_fields:
      - candidate_id
      - session_id
      - analyzer_version
      - region_id
      - base_address_hex
      - offset_hex
      - absolute_address_hex
      - data_type
      - value_sequence_summary
      - feature_vector
      - analyzer_sources
      - score_total
      - score_breakdown
      - confidence_level
      - validation_status
      - explanation_short
    retention:
      default: preserve_all_raw_and_generated_artifacts
      failed_sessions: preserve_if_integrity_valid_and_mark_not_success
      compaction: lossless_only_unless_explicitly_marked_and_recorded
      deletion: only_via_explicit_prune_command_with_dry_run_and_manifest_record

  analyzer_contract:
    rules:
      - offline_replayable
      - deterministic_for_same_input
      - emits_json_or_jsonl
      - includes_analyzer_version
      - does_not_attach_to_process
      - does_not_require_RIFT_running
      - does_not_mutate_raw_snapshots
    order:
      1: DeltaAnalyzer
      2: StabilityAnalyzer
      3: FloatDensityAnalyzer
      4: SequentialAnalyzer
      5: ClusterAnalyzer
      6: StructureAnalyzer
      7: BehaviorAnalyzer
      8: PlayerMatchAnalyzer
      9: CrossSessionAnalyzer
    minimum_analyzer_output:
      - analyzer_id
      - analyzer_version
      - input_artifacts
      - output_artifacts
      - elapsed_ms
      - input_bytes
      - diagnostics

  rift_discovery_model:
    assumptions_to_validate_not_hardcode:
      - visible_players_exist_in_client_memory
      - visible_npcs_exist_in_client_memory
      - actor_orientation_and_camera_orientation_are_distinct_signals
      - entity_structures_are_more_durable_than_entity_instances
      - addon_truth_can_validate_player_position_when_available
    entity_discovery_must:
      - scan_multi_entity_patterns
      - detect_repeating_position_like_vec3_blocks
      - detect_stride_or_record_layout_candidates
      - correlate_multiple_entities_before_player_match
      - use_npc_and_other_player_motion_as_passive_signal
      - promote_stable_entity_pools_for_followup
    entity_discovery_must_not:
      - ignore_npcs_or_other_players
      - preselect_only_player_region
      - assume_short_lived_entities_are_waste_before_layout_detection
      - spend_repeated_followup_budget_on_unstable_transient_instances
    addon_truth_policy:
      role: validator_not_primary_scanner
      allowed:
        - load_known_player_position
        - load_timestamps
        - load_context_labels
        - validate_player_match
        - reject_candidates_conflicting_with_known_truth
      forbidden:
        - make_core_capture_depend_on_addon_output
        - treat_missing_addon_truth_as_capture_failure
    signal_expectations:
      move_forward:
        expected: player_position_changes_actor_yaw_mostly_stable
        reject: random_idle_change_or_ui_animation_noise_only
      turn_left_or_right:
        expected: actor_yaw_changes_smoothly_position_near_stable
        reject: jumps_without_temporal_smoothness_or_camera_only_match
      camera_only:
        expected: camera_yaw_or_pitch_changes_actor_yaw_may_not_change
        reject: position_translation_only_or_actor_turn_only_match

  scoring_model:
    formula: behavior_match + temporal_stability + structural_validity + cross_session_consistency + information_gain - noise_penalty - storage_cost_penalty
    total_min: 0
    total_max: 100
    clamp_total: true
    missing_component_score: 0
    rejected_candidate_score: 0
    require_component_explanation: true
    components:
      behavior_match: 0_to_25
      temporal_stability: 0_to_20
      structural_validity: 0_to_25
      cross_session_consistency: 0_to_20
      information_gain: 0_to_10
      noise_penalty: 0_to_30
      storage_cost_penalty: 0_to_10
    confidence:
      high: score_total_greater_or_equal_75_and_validation_not_conflicting
      medium: score_total_50_to_74
      low: score_total_less_than_50
      rejected: conflicts_with_truth_or_required_invariant
    top_candidate_requires:
      - score_total
      - score_breakdown
      - evidence_summary
      - why_not_rejected
      - next_validation_step

  truth_claim_rules:
    allowed_claim_levels:
      observed: session_id_and_artifact_path
      candidate: candidate_id_and_score_breakdown
      validated: candidate_id_validation_status_supporting_sessions
      recovered: old_candidate_id_new_candidate_id_cross_session_evidence
    forbidden_claims:
      - found_actor_yaw_without_candidate_id
      - found_camera_orientation_without_camera_only_or_equivalent_validation
      - found_player_without_entity_structure_evidence
      - build_passed_without_command_output_or_clear_statement

  adaptive_strategy:
    enabled: true
    scope: region_priority_capture_density_candidate_retention_only
    may_adapt:
      - region_priority
      - snapshot_frequency
      - static_region_sampling_after_baseline
      - entity_pool_attention
      - next_capture_recommendation
    must_not_adapt:
      - pipeline_order
      - passive_sweep_requirement
      - cluster_detection_requirement
      - player_after_structure_rule
      - raw_data_retention

  cli_contract:
    executable: riftscan
    required_commands:
      - riftscan capture passive
      - riftscan analyze session
      - riftscan report session
      - riftscan verify session
    preferred_examples:
      - riftscan capture passive --process rift_x64 --samples 50 --interval-ms 100 --out sessions/<id>
      - riftscan analyze session sessions/<id> --all
      - riftscan report session sessions/<id> --top 100
      - riftscan verify session sessions/<id>
    future_commands:
      - riftscan capture stimulus --mode turn-left --samples 50 --out sessions/<id>
      - riftscan compare sessions sessions/<id1> sessions/<id2>
      - riftscan migrate session sessions/<id> --to-schema <version>
      - riftscan session prune --dry-run
    command_rules:
      - idempotent_where_possible
      - writes_machine_readable_output
      - returns_nonzero_on_failure
      - supports_verbose_logging
      - prints_output_paths_on_success
      - no_silent_raw_artifact_mutation

  build_test_quality:
    if_solution_exists:
      restore: dotnet restore
      build: dotnet build --configuration Release
      test: dotnet test --configuration Release --no-build
      format_check: dotnet format --verify-no-changes
    if_no_solution_exists:
      first_steps:
        - create_solution
        - create_core_capture_analysis_cli_projects
        - create_unit_test_project_before_complex_analyzers
    required_before_completion:
      - run_restore_if_solution_exists
      - run_build_if_solution_exists
      - run_tests_if_tests_exist
      - run_format_check_if_available
      - report_unrun_checks_with_reason
    required_tests:
      - session_schema_validation
      - region_filtering
      - snapshot_index_roundtrip
      - checksum_validation
      - analyzer_determinism
      - scoring_thresholds
      - report_generation_from_fixture_candidates

  observability:
    logs: structured
    required_log_fields:
      - session_id
      - command
      - phase
      - elapsed_ms
      - bytes_read
      - regions_seen
      - regions_selected
      - read_failures
      - artifact_path
    required_metrics:
      - total_bytes_read
      - total_bytes_stored
      - compression_ratio
      - read_success_rate
      - analysis_elapsed_ms
      - candidates_emitted
      - candidates_rejected
    diagnostics_rule: explain_why_regions_or_candidates_were_dropped

  versioning:
    format: MAJOR.MINOR.PATCH
    rule: capability_not_iteration_count
    pre_stable_range: 0.x.y
    stable_baseline: 1.0.0
    patch_bump_when:
      - bug_fix
      - performance_improvement_without_behavior_change
      - scoring_tuning_without_schema_change
      - documentation_or_test_improvement
    minor_bump_when:
      - backward_compatible_command_analyzer_capture_mode_report_or_schema_field_added
    major_bump_when:
      - public_cli_contract_breaks
      - session_format_breaks_without_migration
      - pipeline_order_changes
      - core_architecture_changes
    milestones:
      v0_1: repository_cli_skeleton_schema_validator
      v0_2: read_only_passive_capture_valid_session
      v0_3: offline_replay_basic_triage_report
      v0_4: cluster_structure_entity_pool_candidates
      v0_5: rift_adapter_validation_stimulus_labels
      v0_6: player_match_after_structure
      v0_7: actor_camera_signal_separation
      v0_8: cross_session_recovery
      v1_0: reliable_replayable_truth_discovery_baseline

  coding_rules:
    prefer:
      - small_files
      - small_types
      - explicit_names
      - immutable_records_for_artifacts
      - streaming_io_for_large_snapshots
      - cancellation_tokens_for_long_operations
      - dependency_injection_at_boundaries_not_everywhere
    required_headers:
      generated_code:
        - version
        - purpose
        - character_count_when_practical
      scripts:
        - version_comment
        - purpose_comment
        - end_of_script_marker
    must:
      - use_structured_logging
      - validate_inputs
      - fail_fast_on_invalid_session_schema
      - write_clear_error_messages
      - keep_core_free_of_rift_specific_logic
      - document_binary_formats
    must_not:
      - create_god_classes
      - swallow_exceptions_without_context
      - hide_errors
      - use_coordinate_clicking
      - add_launcher_input_or_window_control_to_scanner_core
      - touch_credentials_tokens_or_auth_material
      - copy_wow_or_other_mmo_assumptions_into_rift_logic

  report_requirements:
    formats:
      - markdown_for_human_review
      - json_or_jsonl_for_machine_reuse
    report_md_must_include:
      - session_id
      - process_name
      - capture_mode
      - snapshot_count
      - captured_region_count
      - total_bytes_raw
      - total_bytes_stored
      - analyzer_list_and_versions
      - top_candidates
      - confidence_scores
      - rejected_candidate_summary
      - limitations
      - next_recommended_capture
    next_capture_plan_json_must_include:
      - recommended_mode
      - target_region_priorities
      - reason
      - expected_signal
      - stop_condition

  acceptance_criteria:
    minimum_viable_success:
      - passive_capture_or_fixture_session_produces_valid_session
      - verify_command_checks_session_integrity
      - offline_analysis_runs_without_RIFT_running
      - dynamic_regions_ranked
      - report_generated
    v0_success:
      - entity_structure_candidates_detected
      - position_like_vec3_candidates_ranked
      - yaw_or_rotation_candidates_ranked
      - player_candidate_matched_only_after_structure_detection
    v1_success:
      - reproducible_session_artifacts
      - replayable_offline_analysis
      - stable_top_candidate_ranking
      - optional_addon_truth_validation_supported
      - next_capture_plan_reduces_search_space
    final_project_success:
      - position_detected
      - actor_yaw_detected
      - camera_orientation_detected
      - entity_structures_detected
      - results_reproducible_from_stored_snapshots
      - no_rescan_required_for_new_analyzer

  completion_rules:
    before_final_response:
      - summarize_files_changed
      - summarize_commands_run
      - summarize_tests_passed_or_failed
      - summarize_artifacts_created
      - summarize_remaining_risks
      - do_not_claim_success_without_artifact_or_test_evidence
    if_blocked:
      - state_exact_blocker
      - preserve_partial_work
      - identify_smallest_next_action
      - do_not_create_unrelated_helpers
      - do_not_expand_scope_to_avoid_blocker
    final_response_sections:
      - changed
      - commands_run
      - validation_result
      - remaining_risks
      - next_smallest_action

  end_marker: END_OF_AGENTS_MD

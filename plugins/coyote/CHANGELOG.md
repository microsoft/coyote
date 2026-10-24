# Coyote plugin changelog

Plugin versions are independent of Coyote NuGet package versions.

## 0.1.0

- Initial standalone plugin, supporting **Coyote 1.7.11 only**.
- Adapted the SpecDrivenHarness Coyote skill and corrected guidance on
  controlled delays, deadlock diagnosis, collection scheduling, and API probes.
- Added a version-pinned build, rewrite, strict-mode, race-detection, and replay
  regression gate.
- Documented default scheduling granularity and its performance tradeoff; added
  a consumer walkthrough with completion-aware checks, replay, and monitors.

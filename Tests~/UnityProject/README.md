# Unity test project

This project is used by CI (`game-ci/unity-test-runner`) and can also be opened locally.

- The package is referenced as `file:../..`.
- The package is listed in `testables`, so its EditMode tests run.
- Unity version: 2021.3.45f1.
- CI requires a `UNITY_LICENSE` repository secret.

Run locally with the Unity version pinned in `ProjectSettings/ProjectVersion.txt`:

```bash
UNITY=/path/to/Unity
"$UNITY" -batchmode -nographics \
  -projectPath Tests~/UnityProject \
  -runTests -testPlatform EditMode \
  -testResults /tmp/td-tests.xml \
  -logFile /tmp/td-tests.log
```

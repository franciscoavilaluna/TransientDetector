# Estado del proyecto y cómo continuar

## Estado actual (2026-10-20)

- Rama de trabajo: `chore/package-p0`
- `main` no se tocó.
- Fases 1–9 implementadas.
- Versión del paquete: `2.0.0`.
- Tests EditMode: **27/27 pasan** en Unity 6.
- Paquete sin dependencias externas de DSP: FFT propia, sin MathNet.
- CI preparado para Unity 2021.3 en `.github/workflows/ci.yml`.
- Proyecto de tests: `Tests~/UnityProject`.
- Editor window: `Window > Audio Beat Detector > Beat Analysis`.
- Sample: `Samples~/Demo`.

## Cómo retomar

```bash
cd /home/pacosmosis/docs/projects/TransientDetector
git fetch --all
git switch chore/package-p0
git pull --ff-only
git status
git log --oneline -5
```

Prompt para pegar en pi:

> Continuamos con TransientDetector. Estamos en `chore/package-p0`. Fases 1–9 terminadas, tests 27/27 y paquete 2.0.0. Revisa el estado, corre los tests y ayúdame con la integración en el juego / tuning con música real. No hagas merge a `main` ni push sin confirmármelo.

## Qué incluye 2.0.0

### Análisis

- Bandas kick, snare, hi-hat y full-band.
- `BeatEvent` con tiempo, fuerza y banda.
- `BeatMap` con `onsets`, `beats`, `beatGrid`, `downbeats`, `bands`, `bpm`, `tempoConfidence`, `energy`, `duration`, `sampleRate`, `firstBeatTime`, `beatPeriod`.
- `QuantizeTime`, `GetBeatIndex`, `GetBeatTime`.
- BPM, grid y downbeats 4/4.
- FFT propia radix-2.
- Cancelación, progreso y lectura de audio multicanal por chunks.

### Runtime

- `Conductor` con offsets de latencia.
- `BeatMapPlayer` con `AudioSettings.dspTime`.
- `BeatMapCache` con JSON y archivos.
- `BeatMapAsset` para guardar análisis como asset.

### Editor y samples

- Ventana de análisis con waveform, onsets, beats y downbeats.
- `Samples~/Demo/BeatDemo.cs`.

### Tests y CI

- 27 tests EditMode.
- `Tests~/UnityProject` para CI.
- GitHub Actions con `game-ci/unity-test-runner` (requiere `UNITY_LICENSE`).

## Pendiente recomendado

1. Probar con música real de distintos géneros y ajustar `delta`, `minInterval`, bandas y prior de tempo.
2. Verificar el CI de Unity 2021.3 con `UNITY_LICENSE`.
3. Probar IL2CPP y builds standalone.
4. Evaluar WebGL: el análisis con hilos no está soportado; usar la ruta síncrona solo en clips cortos.
5. Integrar el `BeatMap` en el generador de niveles del juego.
6. Medir rendimiento en móvil y, si hace falta, añadir resampling a 22050 Hz.

## Limitaciones conocidas

- La banda de snare puede reaccionar al ataque broadband del kick.
- Tempo asumido mayormente constante; cambios largos de tempo pueden requerir tuning.
- Sin detector en tiempo real.
- WebGL no soportado para análisis con hilos.
- Memoria alta en canciones largas.

## Verificación rápida

```bash
cd /home/pacosmosis/docs/projects/TransientDetector
dotnet build "Tools~/AudioBeatDetector.csproj" -v q
```

Tests en Unity 6:

```bash
UNITY=/home/pacosmosis/docs/Unity/Hub/Editor/6000.6.3f1/Editor/Unity
"$UNITY" -batchmode -nographics \
  -projectPath /tmp/TransientDetectorTest \
  -runTests -testPlatform EditMode \
  -testResults /tmp/td-tests.xml \
  -logFile /tmp/td-tests.log
```

Resultado esperado: `passed="27" failed="0"`.

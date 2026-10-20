# Estado del proyecto y cómo continuar

## Estado actual (2026-10-20)

- Rama de trabajo: `chore/package-p0`
- `main` no se tocó: sigue en `7d501cb`
- Fase 1: commit `3bc698e` — paquete compilando en Unity, MathNet vendorizado
- Fase 2/3: validación, robustez, bordes y normalización
- Tests EditMode: **13/13 pasan** en Unity 6
- Tono puro 440 Hz: **0 kicks, 0 snares** (falso positivo corregido)
- `hopSize = 0`: lanza `ArgumentOutOfRangeException` (ya no `DivideByZeroException`)
- WAV estéreo con kick/snare/pad: sin excepciones y detecta kicks
- `Runtime/` existe en `Library/PackageCache/...`, no en la carpeta física `Packages/`

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

> Continuamos con TransientDetector. Estamos en `chore/package-p0`. Fases 1–3 terminadas y tests 13/13. Sigue con la Fase 4 (BeatEvent con fuerza, multi-banda, full-band e hi-hat), luego Fase 5 (BPM/beat grid/downbeats) y Fase 6 (BeatMapPlayer/latencia/caché). No hagas merge a `main` ni push sin confirmármelo.

## Siguiente trabajo

### Fase 4 — Modelo de beats

- `BeatEvent` con tiempo, fuerza y banda.
- `BeatMap` multi-banda con `BandResult[]`.
- Banda full-band y banda de hi-hat.
- Evitar que el ataque del kick dispare la banda de snare.
- Timeline combinada de onsets.

### Fase 5 — BPM y grid musical

- Estimación de BPM por autocorrelación/comb filter.
- Fase del beat y grid estable.
- Downbeats/compases.
- Cuantización a 1/4, 1/8 y 1/16.

### Fase 6 — Runtime

- `BeatMapPlayer`/`Conductor` con `AudioSettings.dspTime`.
- Calibración de latencia.
- Eventos, pausa y seek.
- Caché del análisis en JSON o ScriptableObject.

### Fase 7 — Rendimiento y memoria

- Resample a 22050 Hz.
- Reutilizar buffers en `OnsetRefiner`.
- `CancellationToken` e `IProgress<float>`.
- Lectura por chunks.

### Fase 8 — FFT propia

- Quitar MathNet definitivamente.
- Tests contra la FFT actual.
- Verificar IL2CPP.

### Fase 9 — Repo y calidad

- Samples/demo.
- Editor window para visualizar beats.
- CI con tests.
- Probar Unity 2021.3 LTS.
- GitHub Release.

## Problemas conocidos

- La banda de snare detecta kicks por el contenido broadband del ataque.
- Todavía no hay BPM, beat grid ni downbeats.
- Solo probado en Unity 6; falta Unity 2021.3 LTS.
- WebGL no probado.
- El análisis de canciones largas usa bastante memoria.

## Verificación rápida

```bash
cd /home/pacosmosis/docs/projects/TransientDetector
dotnet build "Tools~/AudioBeatDetector.csproj" -v q
```

Tests en Unity:

```bash
UNITY=/home/pacosmosis/docs/Unity/Hub/Editor/6000.6.3f1/Editor/Unity
"$UNITY" -batchmode -nographics \
  -projectPath /tmp/TransientDetectorTest \
  -runTests -testPlatform EditMode \
  -testResults /tmp/td-tests.xml \
  -logFile /tmp/td-tests.log
```

El resultado debe ser `passed="13" failed="0"`.

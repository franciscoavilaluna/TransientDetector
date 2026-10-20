# Estado del proyecto y cómo continuar

## Estado actual (2026-10-20)

- Rama de trabajo: `chore/package-p0`
- Fase 1 completada: commit `3bc698e` — *Fix Unity package dependency and clean build artifacts*
- Tag: `v1.1.0` (apunta a `3bc698e`)
- `main` no se tocó: sigue en `7d501cb`
- Verificado en Unity 6 desde un clon limpio de solo lo commiteado:
  - El paquete se instala como `file:` y compila sin errores.
  - `MathNet.Numerics.dll` está vendorizado en `Runtime/Plugins`.
  - Smoke test: `SMOKE: kick=8 snare=15`.

## Cómo retomar mañana

```bash
cd /home/pacosmosis/docs/projects/TransientDetector
git fetch --all
git switch chore/package-p0
git pull --ff-only
git status
git log --oneline -5
```

Prompt para pegar en pi:

> Continuamos con TransientDetector. Estamos en la rama `chore/package-p0`, Fase 1 en `3bc698e`, tag `v1.1.0`. La Fase 1 está terminada y verificada. Sigue con la Fase 2 (validación de settings, NaN/Inf, null-checks y tests) y la Fase 3 (bordes y normalización). No hagas merge a `main` ni push sin confirmármelo.

## Siguiente trabajo

### Fase 2 — Validación y robustez

- Añadir `DetectorSettings.Validate()`.
- Validar `hopSize`, `fftSize`, `sampleRate`, bandas, radios, `localFloor`, `logGamma`, `delta`, `noiseFloor` y `minInterval`.
- Sanitizar `NaN`/`Infinity` en muestras y flujo.
- Inicializar `BeatMap` vacío en vez de dejar arrays null.
- Null-checks consistentes en las versiones sync y async.
- Tests de settings inválidos.

### Fase 3 — Bordes y normalización

- Analizar solo frames completos (o usar padding reflejado).
- Evitar que el percentil 98 amplifique ruido numérico.
- Arreglar `NormalizeLocal`: no recortar el pico local a 1.
- Usar un umbral robusto mediana + MAD con un floor.
- Tests: tono puro, silencio, ruido blanco y clicks con rango dinámico.

## Problemas conocidos

- Un tono puro de 440 Hz genera falsos positivos (~7 kicks/s).
- `hopSize = 0` lanza `DivideByZeroException`.
- La banda de snare detecta kicks por el contenido broadband del ataque.
- Todavía no hay BPM, beat grid ni downbeats.
- Solo probado en Unity 6; falta Unity 2021.3 LTS.

## Verificación rápida

```bash
cd /home/pacosmosis/docs/projects/TransientDetector
dotnet build "Tools~/AudioBeatDetector.csproj" -v q
```

Para Unity: crear un proyecto limpio, añadir el paquete como `file:` y ejecutar el smoke test de `Assets/Editor/DetectorSmokeTest.cs`.

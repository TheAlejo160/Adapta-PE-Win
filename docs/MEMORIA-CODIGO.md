# Mapa verificado de Adapta PE Windows

Proyecto de grafo: `adapta-pe-win`. Raíz: este repositorio. Regeneración completa solicitada para la entrega 1.0.1; persistencia en `.codebase-memory/graph.db.zst`. El índice anterior tenía sólo dos nodos; el nuevo índice contiene símbolos de las fuentes C# de Windows y scripts compatibles. Las fuentes mandan sobre cualquier índice o resumen.

## Flujo de la aplicación

1. `App.Main` / `OnStartup` crea una instancia por sesión y compone `MainWindow`. Aplica estilos, crea ventana/bandeja y después restaura preferencias. Bloqueo y suspensión llaman al mismo apagado de módulos.
2. `MainWindow.Build` crea Control, Lectura y color, IA opcional y Ayuda. `FloatingWindow` vive en el mismo archivo y llama a los mismos `SetVoice` y `SetCamera`; no compone otro motor. `Theme.Asset` comparte bitmaps congelados.
3. `Voice` captura y reconoce, `WakeState.Consume` valida palabra/ventana de activación, y `MainWindow.Run` muestra la intención. `Commands.Parse` conserva texto literal de dictado y resuelve intenciones; `Actions.Execute` serializa acciones o delega a `LocalCommand`.
4. `Actions` conserva destino externo y elecciones. `DesktopAccess` ejecuta patrones UIA; `Native` contiene entrada y ventanas Win32. `Applications` separa catálogo y ventanas abiertas; `AppNames` comparte alias. Cierre normal usa ventana/PID comprobados y `WM_CLOSE`.
5. `Camera.Start` mantiene captura, detector y movimiento independientes. Preview usa un buffer compartido, `MainWindow` reutiliza `WriteableBitmap`. `KineticState.Update` devuelve velocidad por segundo y progreso de dwell; muestras antiguas no controlan entrada.
6. `Filters` administra el recurso global de color; `OptionalAI` sólo responde a órdenes explícitas con consentimiento de pantalla por sesión. `Preferences` guarda ajustes y protege la clave con DPAPI.

## Responsabilidades y límites

| Fuente | Responsabilidad |
| --- | --- |
| `src/AdaptaPE/App.cs` | Instancia, arranque, eventos de sesión y salida |
| `src/AdaptaPE/MainWindow.cs` | WPF, PiP, bandeja, estado y composición |
| `src/AdaptaPE/Theme.cs` | Estilos XAML, marca y caché de imágenes WPF |
| `src/AdaptaPE/Core/Commands.cs` | Catálogo, parser, teclas y activación de voz |
| `src/AdaptaPE/Core/AppNames.cs` | Canonización y prioridad de identidad de apps |
| `src/AdaptaPE/Core/KineticState.cs` | Calibración, física, dwell y matrices de color |
| `src/AdaptaPE/Windows/Voice.cs` | Sesión SAPI/local, cancelación y síntesis |
| `src/AdaptaPE/Windows/WaveInput.cs` | Captura WinMM por evento y buffers acotados |
| `src/AdaptaPE/Windows/LocalSpeech.cs` | Adaptador Vosk nativo local bajo demanda |
| `src/AdaptaPE/Vision/Camera.cs` | Captura/preview, detección y bomba de movimiento |
| `src/AdaptaPE/Vision/LocalVision.cs` | Modelos y modos físicos de seguimiento |
| `scripts/installer.nsi` | Asistente nativo, versión instalada, accesos y desinstalación |
| `scripts/installer.ps1` | Publicación autocontenida, licencias y empaquetado |
| `tests/AdaptaPE.Checks/Program.cs` | Regresiones portables; no sensores ni WPF |

NSIS no tiene símbolos en este grafo y se lee directamente. Las exclusiones `bin`, `obj`, `artifacts` y DLL son intencionales; los binarios no son fuentes. Las incidencias de parseo se comprueban por ruta antes de citar símbolos. Una cobertura sin incidencias registradas no demuestra exhaustividad.

## Consultar y actualizar

Con el MCP codebase-memory instalado:

```text
index_status(project="adapta-pe-win")
search_graph(project="adapta-pe-win", name_pattern=".*Parse.*", file_pattern="*.cs")
trace_path(project="adapta-pe-win", function_name="adapta-pe-win.src.AdaptaPE.Core.Commands.Commands.Parse", direction="both")
get_code_snippet(project="adapta-pe-win", qualified_name="adapta-pe-win.src.AdaptaPE.Core.Commands.Commands.Parse")
check_index_coverage(project="adapta-pe-win", paths=["src/AdaptaPE/Core/Commands.cs"])
index_repository(repo_path="<ruta local de este repositorio>", name="adapta-pe-win", mode="full", persistence=true)
```

No modificar la skill global para guardar información de un único proyecto. El índice, este mapa y el ADR del proyecto conservan ese contexto. Si el MCP no está disponible, leer las fuentes directamente y declarar ese límite.

## Referencias revisadas

Extensión Base: `popup.html`, `classes/VoiceAssistant.js` y README. Se trasladan navegación natural, alias de postura, descanso y cancelación de clic; no se importa DOM ni se ejecuta JavaScript en la app.

macOS Base: `Adapta-PE/ContentView.swift` se lee directamente porque su índice reportaba cambios. Se conserva composición por módulos, acceso de calibración/pausa y marca. No se atribuyen validaciones macOS a Windows.

## Estado verificable

1.0.1: compilación cruzada, lógica portable y compilación NSIS disponibles. WPF, entrada Win32, instalación/actualización y sensores de esta entrega necesitan Windows. No hay nuevas mediciones de CPU, RAM o FPS ni actualizador de red. Ver `VALIDACION-WINDOWS.md`.

## Actualización de voz 1.0.2

`Commands` añade frases naturales y un vocabulario finito compartido con SAPI. `AppNames` comparte alias, coincidencias de nombre y puntuación por identidad de proceso. `Applications` combina Inicio/App Paths y snapshots de ventanas con HWND/PID; los parecidos exigen elección. `Actions` reutiliza esos snapshots para enfocar/cerrar, enumera apps abiertas y comprueba destino bajo el cursor.

`Core/RecognitionSession.cs` conserva generaciones e invalida arranques que esperan liberación. `Voice.Restart` mantiene activación, espera liberar Vosk y carga SAPI con gramáticas de comandos/activación más dictado. `MainWindow.ChangeVoice` mantiene intención de escucha al cambiar ajustes. Base y Lite tienen estos motores equivalentes; Lite conserva su wrapper de comandos excluidos y composición propia.

Regeneración persistente completa para esta entrega y revisión de cobertura por fuente. NSIS se lee directamente: el índice no analiza ese lenguaje. El índice describe código, no ejecución Windows. CLI Antigravity se consultó para revisión independiente, pero no devolvió texto utilizable; las conclusiones se verificaron contra fuentes y comprobaciones locales.

Tres targets compilados desde macOS. Las comprobaciones portables verifican parser, alias, negaciones y espera/cancelación del arranque. Una compilación inicial de Lite net48 informó NU1900 por consulta de vulnerabilidades NuGet no disponible. La compilación final de los tres targets termina sin errores ni advertencias; no se acredita una auditoría nueva de paquetes. No se añaden dependencias. Precisión acústica, HWND/UIA real y cambio de motor con dispositivos siguen pendientes en Windows.

Cobertura final: las nueve fuentes C# modificadas (incluidas comprobaciones) no reportan incidencias y coinciden con el índice. `check-voice.ps1` tiene un hueco de parseo en línea 3 (`Add-Type`); esa línea se leyó directamente en ambas ediciones. `.csproj` y NSIS no se siguen como código en el grafo y se verifican directamente. `word_boundary.int` es un dato del modelo con parseo parcial, no evidencia de lógica C#; el empaquetador verifica sus hashes de manifiesto.

## Numeración unificada 1.0

`MainWindow.ProductVersion` toma el atributo informativo del ensamblado y se comparte con PiP. El proyecto define versión pública 1.0 y FileVersion 1.0.3.0. `installer.ps1` pasa ambos valores a NSIS; éste guarda DisplayVersion público y BuildVersion técnico, con fallback al registro antiguo/EXE propio. NSIS se verifica directamente porque no se analiza en el grafo. Las pruebas de lógica/metadata y compilación desde macOS no acreditan actualización Windows real.

## Publicación de fuentes — 3 de octubre de 2026

El README documenta clonación, compilación, instalación local y recorrido de cada motor. `.codebase-memory/` sigue siendo memoria regenerable del equipo y queda excluida de Git; se conserva este mapa público. Se regenera el índice completo al finalizar y se revisa cobertura de las fuentes consultadas. NSIS y archivos de proyecto se leen directamente: no se les atribuye cobertura estructural del grafo. La publicación de fuentes no acredita ejecución Windows ni una release binaria validada.

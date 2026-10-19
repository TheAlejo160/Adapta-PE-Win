# AdaptaPE: guía unificada de mejoras, plataformas, rendimiento y verificación

**Actualizado:** 17 de octubre de 2026.
**Referencia de comportamiento:** extensión Base 13.0 y Lite 3.0.
**Destinos:** Windows (C#/WPF), macOS (Swift/SwiftUI) y, más adelante, Linux.
**Skill:** `adaptape-nativo` (Codex y Claude Code). Su `SKILL.md` incluye este documento completo.

Este documento reúne en un solo lugar qué se mejoró en las extensiones (voz, mouse cinético y mascotas), el estado de cada proyecto, las brechas, cómo portar cada mecánica por plataforma, los requisitos de rendimiento y detección, y la lista de verificación. Las extensiones son la referencia de comportamiento, no de código: en escritorio no se importa JavaScript, DOM, Manifest V3, MediaPipe web ni el backend Flask.

**Fuente canónica:** `/Users/thealejo/PycharmProjects/WelcomeScreen/extensión/AdaptaPE.md`. `skills/adaptape-nativo/SKILL.md` lo incluye completo. Cada carpeta de AdaptaPE tiene una copia idéntica de este archivo y de la skill en `.agents/skills/adaptape-nativo/` (Codex) y `.claude/skills/adaptape-nativo/` (Claude Code). Edita sólo la fuente y ejecuta `skills/adaptape-nativo/scripts/sincronizar.sh` (dentro de la extensión Base) para actualizar la skill y todas las copias.

Detalle exacto de umbrales y reglas: `CLAUDE.md` (o `AGENTS.md`) y `KINETICMOUSE.md` de la extensión Base. Si este resumen y el código discrepan, el código y las pruebas mandan.

---

## 1. Qué se mejoró en las extensiones

### 1.1 Voz (Base y Lite, código compartido byte a byte)

| Mejora | Dónde vive | Comportamiento clave |
| --- | --- | --- |
| Reconocedor único siempre activo | `voz/escucha.js` (offscreen), `background.js` `sincronizarVoz()` | Un solo reconocedor por extensión, independiente del foco. Reinicio en `onend` con espera de 300 ms a 5 s según fallos. Detener limpia temporizadores y descarta callbacks de motores anteriores. |
| Agrupación de resultados | `classes/VoiceAssistant.js` | Finales agrupados 550 ms; provisionales estables 1.200 ms; prefijos consumidos por índice para no duplicar órdenes. Una transcripción idéntica no reinicia el temporizador. |
| Palabra de activación | `VoiceAssistant.js`, `sitios.js` `IdiomasVoz` | «Computadora/Computadoras/Computador» siempre; además la del idioma: en/de/it «computer», pt «computador», fr «ordinateur». Ventana de 8 s tras el pitido. Sin «compu» ni «ordenador» (falsos disparos). Muletilla sola equivale a la palabra sola. |
| Seis idiomas | `sitios.js` `IdiomasVoz`, `textos.js` | es/en/pt/de/fr/it. Clave única `idiomaVoz` (`auto` → idiomas del sistema, respaldo `es-PE`). Cambiar idioma reinicia sólo el reconocedor afectado. Interfaz, ayuda, TTS y avisos siguen el idioma. |
| Cortesía y variantes españolas | `prepararComando(texto, idioma)` | Quita ¿¡ y puntuación final, envoltorios («por favor», «podrías…»), finales; aplica `frases` del idioma; lleva el primer verbo español (usted, subjuntivo, voseo, infinitivo, enclíticos) a su forma canónica. Idempotente. Conserva mayúsculas y acentos del dictado (`$n`). |
| Parser canónico | `VoiceAssistant.js` | Un único parser en español; los otros idiomas se traducen a órdenes canónicas. Nunca se traducen consultas, dictado, nombres de sitios ni de mascotas. |
| Sitios y búsquedas | `sitios.js` `reconocerSitio()` | Catálogo único de portadas, búsquedas y alias. «busca en YouTube» sin consulta pide la consulta. «arriba/abajo» dentro de una búsqueda no es scroll. |
| Selección por voz | `VoiceAssistant.js` | `selecciona/resalta <nombre>` busca por nombre accesible y anuncia; `dale clic` consume la selección; una selección oculta o eliminada no se sustituye por otra. |
| Acciones del navegador | `background.js` `ejecutarAccion()` | Pestañas, ventanas, historial, zoom, ayuda, duplicar, silenciar, ir a pestaña, cerrar otras, pantalla completa. Funciona también en páginas protegidas con el parser central. |
| Privacidad | Todo el módulo | No se guardan grabaciones ni transcripciones; el estado mostrado vive en memoria. |

### 1.2 Mouse cinético (sólo Base)

| Mejora | Comportamiento clave |
| --- | --- |
| Tres modos | `cabeza` (rostro + Pose de respaldo), `manos` (muñeca de mano abierta o puño, codo como respaldo, sin cabeza/torso) y `zona` (textura local para muñón o mano parcial, sin clasificar anatomía). Preferencias antiguas se convierten sin perder ajustes. |
| Calibración sólo para cabeza | ≥4 inferencias distintas durante ≥900 ms en ventana de 1.200 ms, mediana/percentil 80/deriva. Extremidades usan centro fijo `puntoBase`: listas desde la primera posición fiable. |
| Joystick relativo | La primera posición detectada es el reposo; desplazamiento ×6, dirección sostenida, vuelve al centro al regresar. Entrada `zonaMuerta + 0.004`, salida `zonaMuerta × 0.85`. Perpendicular <0.3 de la dominante se suprime. |
| Señal con dedos | 1–4 dedos alineados (coseno ≥0.85) dan dirección directa (`apuntando:true`); confirmación ≥50 ms y coseno ≥0.7 entre inferencias; la señal tiene prioridad sobre el desplazamiento. Inclinación de palma/puño como respaldo. |
| Física por tiempo real | Mediana de 3 inferencias, EMA por milisegundos, zona segura circular con histéresis 0.008, velocidad en px/s, integración ≤100 ms (extremidades) o 50 ms (cabeza). Nunca por conteo de frames. |
| Clic por permanencia | 1.000 ms sobre el mismo objetivo; armado tras salir de la atracción ≥180 ms; un clic por entrada. |
| Clic por pinza (manos) | `medirPinza()`: distancia 3D 4–8 / 0–9; activa ≤0.25, suelta ≥0.40; ≥2 inferencias y ≥80 ms; soltar <700 ms = clic; ≥700 ms = clic derecho. |
| Clic por boca (cabeza) | `jawOpen` ≥0.5 abierta, ≤0.2 cerrada; ≥3 inferencias y ≥300 ms; clic al cerrar; ≥1.500 ms abierta = bostezo, se cancela. |
| Puño para dwell | Cierre deliberado tras apertura previa (≥120 ms, ≥3 inferencias, seguimiento ≥250 ms) centra el control y arma dwell; abrir cancela. Un clic por cierre. |
| Mano falsa | Confianza ≥0.8 para adquirir, ≥0.6 para conservar; esqueleto con proporciones imposibles se rechaza; mano nueva controla tras ≥3 inferencias y ≥150 ms. |
| Zona (textura) | Adquisición sólo de un candidato que reaparece desplazado; correlación normalizada ±12 px alrededor de una predicción por velocidad; error ≤0.25; giros ±0.12/±0.24 rad; pérdida ≥1.200 ms exige textura nueva. |
| Atracción a controles | Sondeo local ≤28 px sólo con movimiento lento; no recorre todo el árbol ni arma clic. |
| Carga adaptativa | `medirCarga()`: media 80/20 envío→resultado; desde la quinta muestra válida (luego en cada una), >90 ms baja detalle, <40 ms sube; descanso min(0.5×media, 130−media) ms. Extremidades 640×480→480×360→320×240; cabeza 320×240→240×180. |
| Robustez del motor | Worker recreado tras fallo (espera 1 s→30 s); un ImageBitmap en vuelo; datos >500 ms no controlan el cursor; modelo fallido no queda cacheado. |
| Seguridad del clic | Pérdida, pausa, cambio de fuente, calibración y saltos >200 ms desarman; 250 ms iniciales sin clic. |
| Privacidad | Imágenes, postura y plantillas sólo en memoria; `ajustesCineticos` guarda sólo modo/temblor/velocidad/amplitud. |

### 1.3 Mascotas (Base 13.0 y Lite 3.0)

| Mejora | Comportamiento clave |
| --- | --- |
| Tres personajes | `tinku` (gallito), `cuchu` (cuy), `yati` (llama); predeterminado `ninguna`. Atlas WebP + JSON por personaje. |
| Posición | `posicionMascota` guarda sólo fracciones x/y en [0,1]; inicio inferior derecho; arrastre o flechas desde el botón con nombre. |
| No estorba | El dibujo no intercepta clics; sólo el botón. Respeta movimiento reducido. |
| Ahorro | Temporizador hasta el siguiente frame (máx. 200 ms); rAF sólo mientras la mirada se desplaza; atlas decodificado antes del primer dibujo; se destruye al ocultarse y vuelve sin recargar. |
| Mirada | Sigue el puntero real y el cinético; vuelve al reposo 1,5 s después del último movimiento. |
| Voz y lectura | Reacciona a `read`/`talk` con el fin real de TTS; `hablando` se libera solo a los 60 s si el fin no llega; `callar` cancela. Órdenes elegir/saludar/dormir/despertar/ocultar en seis idiomas. |
| Nombres por voz | Normaliza espacios, separadores y letras repetidas sólo en nombres («Y Atti», «Tin cu», «Cu chu»), sin interceptar búsquedas ni dictado. |
| Estados semánticos | Estados por `tipo` (error/espera/escucha/activa), no por palabras de un idioma. |

### 1.4 Límites Base/Lite

Lite tiene voz, dictado, navegación, selección, filtros, idiomas y mascotas. **Lite nunca tiene cámara, mouse cinético ni TalkBack**, ni sus permisos. Esta frontera se mantiene en todas las plataformas.

---

## 2. Proyectos, rutas y pruebas

Rutas del Mac del titular. En otro equipo, pide las rutas equivalentes.

| Proyecto | Ruta | Stack | Pruebas |
| --- | --- | --- | --- |
| Extensión Base 13.0 (referencia) | `/Users/thealejo/PycharmProjects/WelcomeScreen/extensión/` | Manifest V3, Vanilla JS, MediaPipe local | `node tests/<archivo>.test.cjs` |
| Extensión Lite 3.0 (referencia) | `/Users/thealejo/PycharmProjects/WelcomeScreen/extensiónLite/` (usa `clases/`) | Igual sin cámara, cinético ni TalkBack | Cubierta por las pruebas de Base |
| macOS Base | `/Users/thealejo/Desktop/TheAlejoCloud/Adapta-PE/` | Swift/SwiftUI, Speech, Vision, AX, ScreenCaptureKit + Core Image, `AVSpeechSynthesizer`. Paridad en `docs/PARIDAD.md` | `Adapta-PETests` (Xcode) |
| macOS Lite | `/Users/thealejo/Desktop/TheAlejoCloud/Adapta-PE-Lite/` | Igual sin cámara, cinético ni TalkBack; proyecto independiente | `Adapta-PE-LiteTests` |
| Windows Base | `/Users/thealejo/Desktop/Adapta-PE-Win/` | C#/WPF, `net10.0-windows` + `net48` + `net461`, OpenCvSharp DNN con ONNX, SAPI + Vosk. Validación en `docs/VALIDACION-WINDOWS.md` | `tests/AdaptaPE.Checks`, `scripts/checks.ps1` |
| Windows Lite | `/Users/thealejo/Desktop/Adapta-PE-Lite-Win/` | Igual sin cámara ni cinético | `tests/` del proyecto |
| Pro (Laya) | `/Users/thealejo/Desktop/Adapta-PE-Pro/` | macOS primero; Windows (WPF) y Linux (GTK/Python) sólo como alcance documentado | Fuera de alcance salvo petición |
| SDK de mascotas | `/Users/thealejo/Desktop/Mascota-Modular-Adapta-PE/` | SDK web de la mascota modular | `tests/` |
| AdaptaPE Browser | `/Users/thealejo/Desktop/AdaptaPE Browser/` | Electron | `tests/` |
| Landing | `/Users/thealejo/WebstormProjects/adapta-pe/` | Next.js | Fuera de alcance de esta guía |

---

## 3. Brechas respecto a Base 13.0 / Lite 3.0

Revisión por búsqueda de símbolos y contrastada por Codex el 17/10/2026; confirmar en el código antes de actuar. Los nombres nativos están en inglés.

| Mejora | macOS | Windows |
| --- | --- | --- |
| Reconocedor único, reinicio con espera creciente | Hecho (`VoiceManager`) | Parcial: `Voice.Recover()` cambia a Vosk o detiene; sin espera creciente |
| Activación «Computadora» + 8 s | Hecho, palabra configurable | Hecho, palabra configurable |
| Seis idiomas (reconocimiento, TTS, interfaz) | **Falta**: sólo locales es-* | **Falta**: SAPI es y modelo Vosk `es` |
| Palabra de activación por idioma | **Falta** | **Falta** |
| `prepararComando`: cortesía, voseo, subjuntivo, enclíticos | Parcial («por favor») | Parcial («por favor») |
| Selección por nombre accesible | Hecho (AX) | Hecho (UI Automation) |
| Tres modos de seguimiento y zona | Hecho (`TrackingManager`, `ZoneTracker`) | Hecho (`LocalVision`, `ZoneTracker`) |
| Joystick y respaldo | Hecho (`GestureState`) | Hecho (`GestureState`, `Camera`) |
| Señal con dedos (1–4 dedos, prioridad sobre desplazamiento, confirmación 50 ms) | Parcial: 1–2 dedos, sólo cerca del reposo | Parcial: 1–2 dedos, sólo cerca del reposo |
| Histéresis del joystick `zonaMuerta + 0.004` / `× 0.85` | **Falta** (sólo existe la de zona segura, 0.008) | **Falta** (sólo la de zona segura, 0.008) |
| Clic por pinza y clic derecho sostenido | **Falta** | **Falta** |
| Clic por boca | **Falta** (Vision no da `jawOpen`; derivar de labios) | **Falta** (YuNet no da labios; requiere modelo con malla facial) |
| Puño para dwell | **Falta** | **Falta** |
| Mano falsa (confianza + esqueleto) | Revisar (`TrackingManager` lee confianza) | Revisar (`LocalVision` lee puntuación) |
| Carga adaptativa de detalle | **Falta** | **Falta** |
| Atracción a controles ≤28 px | Hecho (`cursorAttraction()`) | Hecho (`CursorAttraction.Apply()`) |
| Mascotas: atlas, estados, movimiento reducido | Hecho (`Mascot.swift`) | Hecho (`Mascot.cs`) |
| Mascotas: seis idiomas y nombres tolerantes | **Falta** | **Falta** |
| Linux | No existe | No existe |

---

## 4. Arquitectura común para escritorio

Las tres plataformas comparten la misma división. Cada módulo nativo es un port explícito de la mecánica, con sus pruebas trasladadas.

```
Entrada            Núcleo (lógica pura, sin APIs del SO)          Salida
───────            ────────────────────────────────────           ──────
Micrófono ─► Reconocedor ─► Activación ─► prepararComando ─► Parser ─► Acciones del SO
Cámara ───► Detector ─► Elección de fuente ─► Gesto/joystick ─► Física ─► Puntero / clic
                                         └► Pinza / boca / puño / dwell
Estado ───► Mascota (atlas, estados semánticos) ─────────────────────► Ventana flotante
```

Reglas de la división:

1. **Núcleo puro**: activación, `prepararComando`, parser, joystick, física, pinza, boca, puño, dwell y máquina de estados de la mascota no llaman APIs del sistema. Reciben tiempo, puntos normalizados [0,1] y confianza; devuelven decisiones. Así se prueban sin cámara ni micrófono.
2. **Adaptadores finos** por plataforma: reconocedor, TTS, cámara, detector, puntero, accesibilidad, filtros y ventana de mascota.
3. **Datos compartidos (propuesta; hoy `CommandParser.swift` y `Commands.cs` definen sus catálogos en código)**: `IdiomasVoz`, catálogo de sitios, textos y casos de prueba se exportarían desde las extensiones a JSON y se leen en cada plataforma. Un cambio en `sitios.js` o `textos.js` se regeneraría, no se copiaría a mano.
4. **Tiempo real**: todos los umbrales están en milisegundos con reloj monotónico (`Stopwatch`, `systemUptime`, `CLOCK_MONOTONIC`).
5. **Privacidad**: imágenes, postura y transcripciones sólo en memoria. Preferencias guardan modo, temblor, velocidad, amplitud, idioma, mascota y posición.

---

## 5. Plataformas: stack, equivalencias, trabajo y límites

### 5.1 Windows (C#/WPF)

Stack actual: WPF en C#, `net10.0-windows` + `net48` (Windows 7 SP1/8.1) + `net461` (Windows 8.0), `LangVersion 7.3`, x64. Visión con OpenCvSharp DNN y modelos ONNX locales en `assets/models` (YuNet, palm/handpose/pose/person de MediaPipe) con hashes en `Vision/ModelHashes.cs`. Voz con SAPI (`System.Speech`) y Vosk local (`Windows/LocalSpeech.cs`, `models/speech/es`). Respeta `Adapta-PE-Win/AGENTS.md`: sin Electron, MAUI, WinUI 3 ni servidor; comprobar capacidades al ejecutar.

| Mecánica | Archivo nativo | Trabajo | Equivalencia y límites |
| --- | --- | --- | --- |
| Reconocedor y reinicio | `Windows/Voice.cs`, `Core/RecognitionSession.cs` | Espera creciente 300 ms→5 s tras fallos. | Hoy `Recover()` cambia a Vosk o detiene, sin espera creciente. |
| Voz en seis idiomas | `Windows/Voice.cs`, `Windows/LocalSpeech.cs` | Elegir `RecognizerInfo` por `Culture` de `idiomaVoz`; modelos Vosk por idioma opcionales en `models/speech/<idioma>`. | SAPI depende de los paquetes de idioma instalados; Vosk aumenta el tamaño del paquete. Informar indisponibilidad; no aparentar éxito. |
| Activación y `prepararComando` | `Core/Commands.cs` | Activación por idioma; port de envoltorios, finales, `frases` y `verbos`. | C# 7.3 sin records ni patrones nuevos: compila también en `net461`. Mismos casos de `tests/voz.test.cjs` en `tests/AdaptaPE.Checks`. |
| TTS | `Windows/Voice.cs` (`SpeechSynthesizer`) | Voz instalada por cultura. | Si no existe, avisar. |
| Pinza y clic derecho | `Vision/LocalVision.cs`, `Core/GestureState.cs` | Port de `medirPinza()` sobre los 21 puntos 3D de handpose. | Mismos umbrales; sin validar con cámaras reales. |
| Clic por boca | `Vision/LocalVision.cs` | Evaluar un modelo local de malla facial ONNX con licencia compatible y hash. Sin modelo, no ofrecer la función. | YuNet sólo da 5 puntos faciales. |
| Puño para dwell y mano falsa | `Vision/LocalVision.cs`, `Core/GestureState.cs` | Port de `inclinacionMano()`, `esqueletoMano()` y umbrales de confianza. | Verificar qué puntuación devuelve `LocalVision` antes de fijar 0,8/0,6. |
| Histéresis y señal con dedos | `Core/GestureState.cs`, `Core/KineticState.cs` | Histéresis del joystick y señal de 1–4 dedos con confirmación de 50 ms y prioridad. | Ajustar contra `controlarGesto()` de `KineticEngine.js`. |
| Zona | `Core/ZoneTracker.cs` | Revisar contra el correlador de la extensión. | — |
| Carga adaptativa | `Vision/Camera.cs` | Medir la inferencia con `Stopwatch`; mismos escalones y descanso. | DNN de OpenCV en CPU con `Cv2.SetNumThreads(1)`; medir antes de cambiar hilos. |
| Puntero y clic | `Windows/Native.cs`, `DwellWindow.cs` | — | `SendInput`; coordenadas físicas con DPI mixto y monitores negativos. |
| Selección y atracción | `Windows/DesktopAccess.cs`, `Vision/Camera.cs` | — | UI Automation; UIPI impide procesos elevados. Atracción ≤28 px ya existe (`CursorAttraction.Apply()`). |
| Mascotas | `Core/Mascot.cs`, `Windows/Mascot.cs` | Reacciones en seis idiomas; nombres tolerantes en el parser. | Atlas validado (144 frames, `idle`, `look`); `SystemParameters` para movimiento reducido. |
| Preferencias | `Windows/Preferences.cs` | Mismas claves lógicas que la extensión. | Secretos con `ProtectedData`. |

Compilar desde macOS no acredita ejecución: registrar pruebas pendientes en `docs/VALIDACION-WINDOWS.md`.

### 5.2 macOS (Swift/SwiftUI)

Stack actual: SwiftUI, deployment macOS 14, hardened runtime. Speech (`SFSpeechRecognizer` local es-PE/es-ES/es-MX; red de Apple sólo con permiso), Vision (`VNDetectHumanHandPoseRequest`, `VNDetectFaceLandmarksRequest`, `VNDetectHumanBodyPoseRequest`), Accessibility (AX), ScreenCaptureKit + Core Image para filtros, `AVSpeechSynthesizer`. Paridad documentada en `docs/PARIDAD.md`. Lite (`Adapta-PE-Lite/`) es un proyecto independiente: no depende de Base ni de una condición `LITE`; porta voz y mascotas a ambos por separado.

| Mecánica | Archivo nativo | Trabajo | Equivalencia y límites |
| --- | --- | --- | --- |
| Voz en seis idiomas | `VoiceManager.swift`, `VoiceInputState.swift` | `SFSpeechRecognizer(locale:)` por `idiomaVoz`, preferir `supportsOnDeviceRecognition`. | Disponibilidad local varía por idioma y Mac. APIs más nuevas (p. ej. `SpeechAnalyzer`, macOS 26) sólo con `if #available` y verificadas en documentación actual. |
| Activación y `prepararComando` | `CommandParser.swift` | Activación por idioma y port de `prepararComando`. | Pruebas en `Adapta-PETests` con los casos de `tests/voz.test.cjs`. |
| TTS | `ActionManager.swift` | `AVSpeechSynthesisVoice(language:)`. | Avisar si no hay voz del idioma. La síntesis no debe escucharse a sí misma. |
| Pinza y clic derecho | `TrackingManager.swift`, `GestureState.swift` | Port de `medirPinza()` con los puntos equivalentes de `VNHumanHandPoseObservation`. | Vision da 21 puntos 2D con `confidence`, sin z: distancia 2D normalizada por tamaño de palma; documentar la diferencia. |
| Clic por boca | `TrackingManager.swift` | `VNFaceLandmarks2D.outerLips/innerLips`: apertura = distancia vertical de labios / ancho de boca. | Recalibrar umbrales; no equivale a `jawOpen`. Sin validar con personas reales. |
| Puño y mano falsa | `TrackingManager.swift`, `GestureState.swift` | Port de esqueleto, puño y umbrales de confianza. | Escalas distintas a MediaPipe: ajustar con fixtures. |
| Histéresis y señal con dedos | `GestureState.swift`, `KineticState.swift` | Histéresis del joystick y señal de 1–4 dedos con confirmación de 50 ms y prioridad. | Ajustar contra `controlarGesto()` de `KineticEngine.js`. |
| Zona | `ZoneTracker.swift` | Revisar contra el correlador de la extensión. | Correlación a 160×120. |
| Carga adaptativa | `TrackingManager.swift` | Medir `perform` de `VNImageRequestHandler`; mismos escalones. | Neural Engine cambia los tiempos: conservar el algoritmo, no los milisegundos esperados. |
| Puntero y atracción | `DesktopAccess.swift`, `TrackingManager.swift` | — | `CGEvent` en monitores físicos (orígenes negativos). Atracción ≤28 px ya existe (`cursorAttraction()`). |
| Mascotas | `Mascot.swift`, `Mascotas/` | Reacciones en seis idiomas y nombres tolerantes. | Ya valida atlas y respeta movimiento reducido. |
| Filtros | `ColorFilterManager.swift` | — | Mismas siete matrices; no son corrección médica. |

### 5.3 Linux (fase posterior)

No existe implementación. Propuesta Pro: GTK con Python. Antes de escribir código, crear un documento de alcance y verificar cada API en la documentación oficial actual del escritorio objetivo (GNOME y KDE, X11 y Wayland). Orden: documento de alcance → Lite (voz, mascotas) → Base (cámara, cinético, lectura).

| Área | Candidatos a evaluar | Riesgo principal |
| --- | --- | --- |
| Interfaz | GTK 4 (alineado con la propuesta Pro) | Elegir un solo lenguaje y runtime. |
| Voz | Vosk local (mismo motor que Windows) | Calidad por idioma y tamaño de modelos. |
| TTS | Speech Dispatcher | Voces instaladas variables. |
| Cámara | V4L2 directo o portal de cámara (PipeWire) en Flatpak | Permisos del sandbox. |
| Visión | Mismos modelos ONNX que Windows con OpenCV DNN u ONNX Runtime | Rendimiento CPU; reutilizar hashes de modelos. |
| Puntero y clic | X11: XTest. Wayland: portal RemoteDesktop/libei o `uinput` con permisos | Wayland restringe la inyección global y puede pedir confirmación. |
| Accesibilidad | AT-SPI2 | Nombres accesibles desiguales por aplicación. |
| Filtros de color | Según compositor | Puede no ser posible globalmente en Wayland: declararlo antes de prometer paridad. |

---

## 6. Rendimiento, fluidez y detección

AdaptaPE debe ser fluido y responder rápido en la gran mayoría de laptops y PC con Windows, MacBook y Mac de escritorio, no sólo en equipos potentes. Esta sección define los equipos objetivo, los presupuestos de tiempo y las técnicas para cumplirlos. Los presupuestos son **objetivos a medir**. No están validados mientras no haya mediciones con hardware real.

### 6.1 Equipos objetivo

Optimiza primero para el nivel bajo. Si funciona ahí, funciona en los demás.

| Nivel | Windows / Linux | macOS | Cámara |
| --- | --- | --- | --- |
| Bajo (diseño y pruebas obligatorias) | Laptop de 4 hilos, GPU integrada, 8 GB de RAM | Mac Intel compatible con macOS 14, 8 GB | Webcam integrada 720p a 30 fps, poca luz |
| Medio | CPU de 6–8 núcleos reciente, GPU integrada moderna | Apple M1/M2, 8–16 GB | 720p–1080p |
| Alto | GPU dedicada o NPU | Apple M3 o posterior | 1080p |

Nunca exijas GPU dedicada, NPU, conexión a internet ni una cámara externa para las funciones básicas.

### 6.2 Presupuestos de tiempo

| Medida | Objetivo | Origen |
| --- | --- | --- |
| Inferencia por imagen | <40 ms ideal; >90 ms obliga a bajar detalle | `medirCarga()` de la extensión |
| Ciclo cámara→cursor (p95) | ≤100 ms objetivo; ≤180 ms máximo | `voz/camara.js`: ciclo ≤180 ms porque los saltos >200 ms desarman el clic |
| Edad máxima de un dato de cámara | 500 ms; los más viejos no mueven el cursor | `voz/camara.js` |
| Movimiento del cursor en pantalla | A la frecuencia del monitor (≥60 Hz), integrando por tiempo entre inferencias | Física por milisegundos de `KineticEngine.js` |
| Fin de frase→acción de voz (p95) | ≤800 ms con reconocimiento local; 550 ms son la agrupación de finales | `VoiceAssistant.js` |
| Interrumpir TTS («silencio», detener) | ≤150 ms | Objetivo propuesto |
| Arranque de la app hasta voz lista | ≤3 s en nivel bajo, sin cargar modelos de cámara si está apagada | Objetivo propuesto |
| CPU sostenida con cámara y voz | ≤25 % total en nivel bajo | Objetivo propuesto |
| HUD y vista de cámara | HUD ≤10 Hz; vista ≤30 fps, independiente de la inferencia | macOS `PARIDAD.md`, extensión |
| Mascota | Temporizador al siguiente frame (máx. 200 ms); bucle continuo sólo mientras mueve la mirada | `mascotas/motor.js` |

### 6.3 Técnicas de fluidez (todas las plataformas)

1. **Una imagen en vuelo.** Si la inferencia sigue ocupada, descarta la imagen nueva en lugar de encolarla. Nunca acumules imágenes atrasadas.
2. **Inferencia fuera del hilo de interfaz.** El hilo principal sólo dibuja y mueve el cursor. Windows: `Task`/hilo dedicado; macOS: cola `userInitiated`; Linux: hilo de trabajo fuera de GTK.
3. **Cursor desacoplado de la cámara.** Mueve el cursor con un temporizador sincronizado con la pantalla (WPF `CompositionTarget.Rendering`, macOS `CADisplayLink`/`CVDisplayLink` según disponibilidad, Linux reloj de frame de GTK) usando la física por tiempo real. Una cámara de 15 fps no debe producir un cursor a saltos.
4. **Carga adaptativa** de la extensión: media 80/20, decisión desde la quinta muestra válida, escalones de resolución y descanso min(0,5×media, 130−media) ms.
5. **Cargar sólo lo necesario.** Modelos del modo activo, cargados una vez y reutilizados. Lite no incluye modelos de visión. Un modelo que falla no queda cacheado como error permanente.
6. **Pausar sin destino.** Pantalla bloqueada, suspensión, app oculta sin destino o cámara apagada: inferencia detenida y recursos liberados. Al volver, sin pedir permisos otra vez.
7. **Energía y temperatura.** Con batería en ahorro de energía o estado térmico alto, empezar en el escalón de detalle bajo. Usa la API de la plataforma (macOS `ProcessInfo.isLowPowerModeEnabled` y `thermalState`; Windows y Linux: verificar la API disponible en cada target) y comprueba su disponibilidad al ejecutar.
8. **Sin asignaciones por imagen.** Reutiliza búferes de imagen, tensores y matrices. En .NET evita LINQ y objetos temporales en el bucle de inferencia; en Swift evita copiar `CVPixelBuffer`.
9. **Accesibilidad acotada.** Consultas AX/UI Automation/AT-SPI2 limitadas a ≤10 Hz, cerca del cursor y con una consulta en vuelo. Nunca recorrer todo el árbol por imagen.
10. **Voz siempre local primero.** Reconocedor en el dispositivo; red sólo con permiso explícito. Resultados provisionales estables para no esperar al final de la frase.

### 6.4 Mejoras de detección

Antes de cambiar un umbral o modelo, mide con los fixtures de `tests/fixtures` de la extensión y con grabaciones locales que no se distribuyen.

1. **Seguimiento por región (ROI).** Tras encontrar una mano, recorta alrededor de los landmarks anteriores y ejecuta sólo el modelo de landmarks. Vuelve a ejecutar el detector de palma sólo al perderla o cada cierto tiempo. Así funciona el diseño de MediaPipe y ahorra la mayor parte del coste en CPU. Verifica si `LocalVision.cs` ya lo hace antes de implementarlo.
2. **Continuidad y confianza.** Mano de mayor confianza; ≥0,8 para adquirir y ≥0,6 para conservar; una mano ≥0,8 próxima gana a una dudosa más cercana; esqueleto con proporciones imposibles rechazado.
3. **Poca luz.** Evalúa una normalización de luminancia ligera antes de la inferencia sólo si mejora los fixtures de poca luz sin empeorar los demás. Zona compensa ya los cambios uniformes de luz.
4. **Respaldo entre modelos.** Pose sustituye a la mano perdida con `respaldo:true`, y el reposo del joystick se desplaza por la diferencia entre modelos.
5. **Aceleración opcional, nunca obligatoria.** macOS: Vision usa Neural Engine/GPU automáticamente; restringe las solicitudes a la fuente activa. Windows: el DNN de OpenCV corre hoy en CPU (`Cv2.SetNumThreads(1)` en `Camera.cs`); mide antes de cambiar hilos y evalúa `Target.OPENCL` o ONNX Runtime con DirectML sólo con respaldo CPU, salidas comparadas con los fixtures y sin romper `net48`/`net461`. Linux: CPU primero.
6. **Mismo resultado en todos los equipos.** El escalón de detalle cambia la resolución de entrada, no los umbrales del gesto (que van normalizados a [0,1]).

### 6.5 Cómo medir

- Registra en memoria, sólo en modo diagnóstico, la duración de inferencia, la edad del dato al mover el cursor, el escalón de detalle y la latencia de voz. Muestra p50/p95 en el panel de diagnóstico; no los escribas en disco por defecto.
- Prueba en al menos un equipo de nivel bajo de cada sistema y con poca luz antes de afirmar fluidez.
- Informa los resultados con el equipo usado. Sin medición real, escribe «sin medir en hardware real».

---

## 7. Orden recomendado

0. Medir la línea base de rendimiento (sección 6) en un equipo de nivel bajo de cada sistema; repetir la medición después de cada paso.
1. Exportar `IdiomasVoz`, catálogo de sitios, textos y casos de prueba de las extensiones a JSON versionado.
2. `prepararComando` y activación por idioma en el núcleo de macOS y Windows (Base y Lite a la vez).
3. Reconocedor y TTS por idioma en cada plataforma, con aviso de indisponibilidad.
4. Pinza y puño para dwell (sólo Base).
5. Mano falsa y carga adaptativa (sólo Base).
6. Clic por boca, sólo si existe un detector de labios local adecuado (sólo Base).
7. Histéresis del joystick y señal con dedos completa (sólo Base); reinicio de voz con espera creciente en Windows.
8. Mascotas en seis idiomas y nombres tolerantes (Base y Lite).
9. Linux: documento de alcance, luego Lite (voz + mascotas), luego Base.

## 8. Verificación y criterios de aceptación

### Criterios de aceptación

- Cada mecánica portada tiene pruebas del núcleo con los mismos casos que `tests/*.test.cjs` y pasan en la plataforma.
- Lite sigue sin cámara, cinético, TalkBack ni sus permisos.
- Ninguna imagen, postura, grabación o transcripción se guarda en disco.
- Los umbrales usan tiempo real; ningún clic depende del número de frames.
- Los presupuestos de la sección 6 se miden en un equipo de nivel bajo de cada sistema, con poca luz, y el informe indica el equipo. Sin medición, se escribe «sin medir en hardware real».
- El cursor se mueve fluido aunque la cámara entregue pocas imágenes por segundo, y ninguna imagen atrasada se procesa.
- Detener todo cancela voz, cursor, lectura e inferencia inmediatamente.
- Las funciones no disponibles en el sistema se muestran como no disponibles, con el motivo.
- La documentación de cada proyecto (README, AGENTS/PARIDAD/VALIDACION) se actualiza y separa lo compilado de lo validado con hardware real.
- No se afirma validación universal con amputaciones ni precisión de gestos sin pruebas con personas usuarias.

### Lista de comprobación

Marca cada punto como **probado**, **sólo compilado** o **pendiente con hardware real**. No los mezcles en el informe.

#### Núcleo

- [ ] Los casos portados de `tests/voz.test.cjs`, `tests/cinetico.test.cjs` o `tests/deteccion.test.cjs` pasan en el proyecto de pruebas nativo.
- [ ] `prepararComando` es idempotente y conserva mayúsculas, acentos y signos del dictado.
- [ ] La activación acepta «Computadora/Computadoras/Computador» y la palabra del idioma elegido; rechaza «compu» y «ordenador».
- [ ] Todos los tiempos usan un reloj monotónico en ms; ningún umbral cuenta frames.
- [ ] Pérdida, pausa, cambio de fuente, calibración y saltos >200 ms desarman clic; no hay clic en los 250 ms iniciales.
- [ ] Un clic por gesto o entrada; pinza consumida limpia el cierre de puño pendiente.

#### Variantes

- [ ] Lite compila sin cámara, visión, cinético, TalkBack ni sus permisos o entitlements.
- [ ] Los cambios comunes (voz, mascotas, textos) llegan a Base y Lite de la plataforma.

#### Privacidad y recursos

- [ ] Ninguna imagen, postura, plantilla, grabación o transcripción se escribe en disco ni en logs.
- [ ] Apagar cámara, voz o filtros libera sus capturas; detener todo cancela inferencia y TTS.
- [ ] Una respuesta tardía de permisos o modelos no reactiva un módulo apagado.

#### Rendimiento y detección

- [ ] Una sola imagen en vuelo; las imágenes atrasadas se descartan, no se encolan.
- [ ] La inferencia corre fuera del hilo de interfaz; el cursor se mueve con un temporizador sincronizado con la pantalla.
- [ ] Carga adaptativa activa: baja el detalle con inferencia >90 ms y lo sube con <40 ms.
- [ ] Sólo se cargan los modelos del modo activo; Lite no incluye modelos de visión.
- [ ] Bloqueo, suspensión, cámara apagada o falta de destino pausan la inferencia y liberan recursos.
- [ ] No hay asignaciones por imagen en el bucle de inferencia.
- [ ] Fixtures de detección (manos reales, manos falsas, puño, poca luz) iguales o mejores que antes del cambio.
- [ ] Mediciones p50/p95 de inferencia, ciclo cámara→cursor, latencia de voz y CPU registradas con el equipo usado, o marcadas «sin medir en hardware real».

#### Plataforma

- [ ] Una función no soportada (idioma sin reconocedor, voz TTS ausente, sin detector de labios, Wayland) se muestra como no disponible con su motivo.
- [ ] Windows: compila en `net10.0-windows`, `net48` y `net461` cuando el archivo está compartido.
- [ ] macOS: APIs más nuevas que macOS 14 están protegidas con `if #available`.
- [ ] Campos de contraseña excluidos de lectura y dictado.

#### Pendiente con hardware real (no se acredita compilando)

Presupuestos de la sección 6 en equipos de nivel bajo, precisión de reconocimiento por idioma, umbrales de pinza/boca/puño, mano falsa, carga adaptativa en equipos lentos, monitores con DPI mixto, y uso con amputaciones o movilidad reducida. Regístralo en el documento de validación del proyecto.

#### Documentación

- [ ] README y AGENTS del proyecto destino describen la nueva función y sus límites.
- [ ] Paridad/validación (`docs/PARIDAD.md`, `docs/VALIDACION-WINDOWS.md`) actualizada.
- [ ] Tabla de brechas de `AdaptaPE.md` actualizada si cambió el estado.

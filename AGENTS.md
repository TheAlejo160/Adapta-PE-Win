# Instrucciones para desarrollar Adapta PE para Windows

## Objetivo y estado

Implementar una aplicación de accesibilidad de escritorio independiente en **C#/.NET y WPF**, trasladando los comportamientos de Adapta PE y Lite de web/macOS. Priorizar Windows 10 y 11; ofrecer compatibilidad histórica separada para 7 SP1, 8.1 y 8.0 con límites explícitos.

Al 2 de octubre de 2026 existe implementación Base en `src/AdaptaPE`, modelos locales en `assets/models`, scripts de compilación y comprobaciones de lógica en `tests/AdaptaPE.Checks`. El usuario autorizó implementar y crear un instalador local con acceso directo. Compilación cruzada no acredita ejecución Windows; marcar instalación/sensores/rendimiento pendientes y no publicar releases públicas sin autorización y validación real.

Lee [README.md](README.md) antes de modificar arquitectura, alcance o compatibilidad. Mantén el trabajo dentro de la petición del usuario; no cambies las cuatro fuentes de referencia sin autorización para esos proyectos.

<!-- codebase-memory-mcp:start -->
Para exploración estructural utiliza la habilidad instalada `codebase-memory`. Comprueba el proyecto activo e indexa el repositorio si no existe un índice. Usa `search_graph`, `trace_path` y `get_code_snippet` antes de búsquedas estructurales por texto. Ejecuta `check_index_coverage` para cada archivo usado como evidencia; lee directamente rangos omitidos o fuentes con índice desactualizado y declara sus límites. Usa `rg` para literales, configuración y verificación. Si las herramientas no están disponibles, indica la limitación y verifica las fuentes directamente sin afirmar una verificación por grafo.
<!-- codebase-memory-mcp:end -->

## Referencias verificadas

- macOS Base: `../TheAlejoCloud/Adapta-PE/Adapta-PE/`. Revisar `CommandParser.swift`, `VoiceManager.swift`, `VoiceInputState.swift`, `ActionManager.swift`, `ApplicationCatalog.swift`, `DesktopAccess.swift`, `TrackingManager.swift`, `KineticState.swift`, `ColorFilterManager.swift`, `OptionalAI.swift` y `Adapta_PEApp.swift` según el módulo afectado.
- macOS Lite independiente: `../TheAlejoCloud/Adapta-PE-Lite/`, con fuentes locales en `Adapta-PE-Lite/`, proyecto `Adapta-PE-Lite.xcodeproj` y `docs/LITE.md`. Incluye voz y filtros; excluye físicamente cámara, cinética, TalkBack automático e IA. No depende del proyecto Base ni de la condición `LITE`.
- Web Base: `../../PycharmProjects/WelcomeScreen/extensión/`; Lite: `../../PycharmProjects/WelcomeScreen/extensiónLite/`. Consultar `sitios.js`, `classes/VoiceAssistant.js` o `clases/VoiceAssistant.js`, filtros y sus README. Sólo Base incluye `KineticEngine`, TalkBack y cámara.
- `docs/PARIDAD.md` del proyecto macOS distingue comportamiento equivalente y límites de plataforma. Sus validaciones macOS no acreditan funcionamiento Windows. Los índices locales pueden estar desactualizados: las fuentes actuales tienen prioridad.

No importar Swift, JavaScript DOM, extensiones MV3 ni el servidor Flask para hacer funcionar la aplicación Windows. Reutilizar datos y casos de regresión mediante un port C# explícito. El prototipo Python de visión devuelve una simulación; no es un motor de inferencia reutilizable.

## Stack y política de compatibilidad

1. Interfaz WPF en C# y XAML; bibliotecas estándar y APIs públicas de Windows primero. No añadir Electron, WebView obligatorio, MAUI, WinUI 3 ni un servidor para una necesidad que resuelve WPF.
2. Edición principal `net10.0-windows` con .NET 10 LTS. Validar Windows 11 y Windows 10 prioritariamente; Windows 10 Home/Pro 22H2 es objetivo de pruebas de ejecución, no una afirmación de soporte oficial de .NET 10. Revisar la matriz vigente enlazada en README antes de cada lanzamiento.
3. Legacy `net48` para Windows 7 SP1 y 8.1, y como vía de compatibilidad Windows 10 cuando corresponda. Nunca sustituir por 4.8.1 si se mantiene 7/8.1.
4. Windows 8.0 requiere una compilación distinta `net461`, con runtime sin soporte. Implementarla al final, como compatibilidad histórica documentada; no omitirla ni afirmar que `net48` funciona allí.
5. Reutilizar fuentes compartidas cuando compilen en cada target. No bajar toda la aplicación a `net461`, copiar motores completos por runtime ni imponer .NET Standard 2.0 al target 4.6.1. Usar bloques de compilación pequeños sólo en límites realmente distintos.
6. Comprobar capacidades al ejecutar y excluir llamadas a APIs ausentes. Guardar el motivo de indisponibilidad para mostrarlo; una función no disponible no debe aparentar éxito.
7. Inicialmente x64. La magnificación necesita arquitectura nativa: x86 sólo en Windows x86, nunca un proceso x86 bajo WOW64. ARM64 exige pruebas de toda dependencia nativa. No utilizar AnyCPU sin revisar este requisito.

Versiones de runtime, paquetes, mínimos de sistema y contratos nativos deben verificarse en documentación oficial actual. Fijar versiones estables compatibles cuando se incorporen dependencias; no escoger paquetes preview por defecto.

## Organización mínima

Estructura actual:

- `src/AdaptaPE/App.cs`, `MainWindow.cs`, `Theme.cs` y `DwellWindow.cs`: aplicación WPF, interfaz nativa en C# sin XAML, bandeja, panel, suspensión y composición de módulos.
- `src/AdaptaPE/Core`: parser, catálogo de sitios, activación de voz, contrato de audio continuo, física cinética y matrices; lógica portable.
- `src/AdaptaPE/Windows`: voz SAPI/Vosk/WinMM, UIA, entrada Win32, acciones, catálogo de apps, filtros, preferencias e IA opcional.
- `src/AdaptaPE/Vision`: captura OpenCV con Media Foundation/DirectShow y modelos OpenCV Zoo. CPU, un hilo y modelos bajo demanda; no servidor ni Python.
- `tests/AdaptaPE.Checks`: comprobaciones ejecutables de lógica sin sensores, credenciales ni red.
- `scripts`: compilación/publicación local por runtime y arquitectura.

Esta entrega es Base completa. No crear Lite ni abstraer variantes hasta que se solicite. Si se añade Lite, excluir físicamente cámara, modelos, TalkBack automático e IA de su compilación. `net461` usa wrapper/runtime OpenCvSharp 4.5.3 con framework explícito; verificar compatibilidad ONNX antes de acreditar paridad en Windows 8.0. Ver `docs/VALIDACION-WINDOWS.md`.

Añadir archivos sólo cuando una funcionalidad los necesite. MVVM sencillo cuando ayude a enlazar estado y UI; no añadir contenedor DI, bus de eventos genérico, repositorio de datos, base SQL ni jerarquías de proveedores anticipadas. Antes de escribir una función, localizar el patrón equivalente y sus llamadores; corregir el fallo en el punto común responsable.

## Contratos que deben conservarse

### Voz y comandos

- Una captura/reconocedor por sesión activa. Inicio, parada y recuperación serializados; cancelación o generación descarta callbacks anteriores. Apagar durante el arranque nunca debe reactivar un módulo.
- Palabra predeterminada «Computadora», personalizable; señal de disponibilidad y ventana de ocho segundos entre activación y orden. Mantenerla entre reinicios de reconocimiento y cambios de ventana.
- Enumerar con `InstalledRecognizers()` los motores españoles realmente disponibles. Preferir el idioma del usuario y variantes españolas instaladas; no asumir `es-PE` disponible. Verificar gramática de órdenes y dictado. `System.Speech` usa referencia Framework en Legacy y paquete Microsoft compatible en moderno.
- El flujo WinMM hereda `LiveAudioStream`: longitud desconocida (-1), posición acumulada y consulta de posición sin fingir rebobinado. SAPI consulta estos datos aunque el stream no permita búsqueda.
- Automático prefiere SAPI español y permite fallback Vosk local x64 incluido. Cargar/liberar modelo bajo demanda, sin red ni transcripciones persistidas. No prometer consumo equivalente a SAPI: el modelo pequeño añade memoria sólo cuando está activo.
- WinMM usa `CALLBACK_EVENT` y un hilo acotado que recoge/reencola buffers en orden; nunca llamar funciones de audio desde `waveInProc`. Al terminar inesperadamente SAPI, Automático cambia una vez a Local; SAPI explícito informa el error real. No reintroducir reintentos sobre un reconocedor terminado.
- Ausencia de micrófono/reconocedor/voz TTS debe producir diagnóstico recuperable. No cambiar silenciosamente a un proveedor de red ni declarar que un paquete de idioma garantiza dictado.
- Procesar cada frase una sola vez. Los provisionales pueden corregir texto completo; no contar palabras para recortar finales. El dictado conserva Unicode, signos y mayúsculas; normalizar sólo intenciones y nombres.
- Mantener alias y rutas del catálogo web. Construir consultas con codificación URL; aperturas web sólo HTTP/HTTPS. Catálogo de apps separado y seguro: una orden de voz no se convierte en PowerShell, `cmd.exe`, script o argumentos arbitrarios.
- Coincidencia exacta antes de parcial; ambigüedad ofrece opciones numeradas. Elegir sólo una coincidencia válida, cancelar al cambiar la app de destino y permitir «cancelar». No guardar selección ni transcripción.
- `Core/AppNames.cs` comparte alias/canonización para abrir y cerrar. Cierre por nombre enumera ventanas visibles, prioriza proceso y no utiliza un título web para cerrar otra app conocida. Coincidencias aproximadas siempre exigen elección. Revalidar HWND/PID antes de `WM_CLOSE`; nunca forzar terminación de procesos.
- Evitar órdenes derivadas de la síntesis propia. Durante lectura aceptar interrupciones explícitas y conservar una vía accesible de parada. Errores recuperables usan espera progresiva; permisos denegados no generan reintento infinito.
- Traducir atajos a Windows por acción: guardar `Ctrl+S`, pestaña `Ctrl+T`, escritorio `Win+D`; no copiar códigos de tecla macOS.

### Acciones sobre escritorio y accesibilidad

- Preferir patrones UIA apropiados (`Invoke`, `Value`, `Text`, selección/foco) a coordenadas o pulsaciones ciegas. Comprobar soporte del patrón y estado visible/habilitado/editable.
- Excluir elementos protegidos (`IsPassword`) de lectura, selección de texto y dictado. No inspeccionar contenido secreto para resolver nombres.
- Fijar la aplicación externa de destino y revalidar proceso, ventana, elemento y foco inmediatamente antes de actuar. La UI propia no debe reemplazar el destino al mostrar opciones o respuestas.
- Si un elemento desaparece, no sustituirlo por otro coincidente. Recorridos UIA con presupuesto de tiempo/nodos y cancelación; fuera del hilo WPF cuando puedan bloquear. Proveedores externos pueden colgar: no prometer que cancelar una tarea interrumpe una llamada COM bloqueada.
- `SendInput` como alternativa limitada: comprobar número de eventos enviados, liberar modificadores y respetar UIPI. No automatizar UAC, escritorio seguro ni elevar toda la app para ocultar restricciones.
- `uiAccess` sólo con una necesidad comprobada, firma y ubicación de instalación válidas. No alterar políticas de seguridad del equipo como paso de instalación.
- Navegador: atajos conocidos/UIA y URLs seguras. No prometer equivalencia de DOM, `chrome.tabs`, selección de pestañas o búsqueda contextual sin una prueba del navegador concreto.
- Lectura manual en ambas ediciones; TalkBack automático sólo Base, con debounce de 300 ms, resaltado y consultas limitadas. No afirmar sustitución de Narrador/NVDA.

### Cinética: sólo Base

- Probar primero captura Media Foundation e inferencia local; documentar modelo, licencia, dependencias nativas y mínimos de sistema antes de seleccionarlos. Ningún modelo en Lite, CDN obligatorio ni backend simulado.
- Objetivo completo: automático, cabeza, manos, torso, brazos y zona libre. Zona libre no debe exigir anatomía. Implementar progresivamente sin etiquetar modos incompletos como disponibles.
- Una inferencia en vuelo; descartar frames atrasados. Velocidad por tiempo monotónico (`Stopwatch`), no por número de frames. Separar refresco del HUD e inferencia; evitar anunciar por accesibilidad cada frame.
- Versión 1.0: captura/preview, inferencia y movimiento trabajan por separado. Mantener sólo un frame pendiente y buffers/bitmap reutilizados. Preview hasta 24 FPS visible; movimiento cada ~15 ms integra `MotionResult.Velocity` y caduca tras 350 ms desde captura. Pausa, pérdida, cambio de fuente y parada anulan velocidad. Detener durante carga/inferencia no puede reactivar entrada.
- Conservar fuente mientras sea válida; cambios requieren estabilidad y recalibración sin clic. Validar números finitos y landmarks físicos; no confundir referencias derivadas de control con puntos físicos necesariamente dentro del cuadro.
- Calibración en postura cómoda, suavizado temporal y mediana en modo temblor, zona muerta circular e histéresis. Mantener ajustes personales de velocidad, amplitud, temblor y permanencia; no eliminar calibración por simplificar. Frenar al volver a zona segura sin esperar al suavizado visual; dwell sigue exigiendo estabilidad.
- Tomar de `Core/KineticState.cs`, la referencia Swift y el motor web los criterios de estabilidad: referencia inicial de >=4 muestras durante >=900 ms en ventana de 1.200 ms. Comprobar el comportamiento con FPS distintos.
- Dwell de referencia de 1.000 ms ajustable sobre el mismo objetivo; armar sólo después de salir del anillo >=180 ms. Un clic por entrada. Calibración, pérdida de detección, pausa y saltos de tiempo desarman el clic.
- Ratón físico pausa el control de referencia durante dos segundos y exige estabilidad antes de reanudar. Distinguir eventos propios de entrada física para evitar autopausa.
- Escritorio virtual con coordenadas negativas, DPI y varios monitores. Bloqueo, suspensión y pérdida de cámara detienen el control; retorno nunca produce un clic pendiente.

### Filtros y recursos globales

- Conservar las siete matrices de `FiltrosDaltonismo` y `ColorFilterManager`. «Sin filtro» restaura el estado anterior apropiado; no sobrescribir sin comprobar filtros de otras ayudas.
- Prototipo inicial con Magnification API: `MagInitialize`, comprobación de efecto global desde Windows 8 y `MagUninitialize`. Validar formato 5×5, transposición y alfa mediante colores de referencia.
- Respetar arquitectura nativa y errores de API. Windows 7 no ofrece `MagSetFullscreenColorEffect`: evaluar control local de magnificación y declarar falta de filtro global hasta probar una alternativa.
- Si el prototipo falla, evaluar captura/composición moderna con exclusión de overlays, límites de memoria y FPS. No construir ese pipeline antes de conocer la necesidad. No prometer capturar contenido protegido.
- El efecto global puede competir con otra aplicación. Serializar Base/Lite, comprobar estado al restaurar y evitar que una instancia deshaga el filtro de otra. Liberar recursos en parada/salida; documentar recuperación tras terminación abrupta antes de distribuir.
- Las matrices son simulaciones visuales, no una corrección médica del daltonismo.

### Ciclo de vida, UI y privacidad

- Reutilizar fondo/logotipo macOS de `assets/brand`, icono en ejecutable/ventanas/bandeja, vista de cámara en Control y PiP. No sustituir identidad por iconos genéricos.
- UI en español, nombres accesibles, navegación de teclado, foco visible y contraste suficiente. Estado de error comprensible y acceso permanente a «Detener todo». No depender sólo de color, hover o voz.
- Mostrar progreso de dwell junto al cursor sin foco ni captura de entrada; conservar fracciones de píxel al reconciliar el cursor físico y reiniciar espera si el objetivo desaparece.
- Primer inicio apagado; restauraciones posteriores sólo con capacidades/autorizaciones disponibles. Inicio con Windows voluntario, por usuario; no servicio de sistema obligatorio.
- Cerrar ajustes mantiene asistencia, bandeja y panel sin tomar foco. El panel se ancla al área útil del monitor/esquina elegidos, se adapta al DPI y ofrece elecciones accesibles sin tapar la barra de tareas.
- `StopAll` debe ser idempotente: invalidar arranques pendientes, cancelar acciones y selección, apagar cámara/voz, TTS/TalkBack, IA y filtros, liberar recursos y recordar módulos apagados. Salir libera recursos conservando preferencias; no confundir ambos comportamientos.
- Bloqueo/cambio de sesión/suspensión detienen entrada y sensores. Reanudar revalida dispositivo, permisos, monitor y foco; cinética se recalibra. No impedir el reposo del equipo para sostener la escucha.
- Configuración separada por edición en `%LOCALAPPDATA%`; escritura con reemplazo seguro y recuperación ante archivo dañado. No guardar imágenes, audio, postura corporal ni transcripciones. Logs sólo de estado/errores sin contenido privado.
- Credenciales en Credential Manager o DPAPI ligado al usuario; nunca en JSON, código, repositorio o logs. No añadir telemetría remota por defecto.
- Windows y macOS tienen modelos de permisos distintos. Explicar el uso de cámara/micrófono y consultar fallos/políticas reales; no diseñar un supuesto diálogo de Accesibilidad macOS en Windows.
- IA sólo Base, opcional y apagada: proveedor HTTPS, modelo y clave del usuario; orden explícita, timeout/cancelación y error visible. Consentimiento adicional por sesión para enviar pantalla. Nunca enviar órdenes desconocidas, transcripciones completas o capturas implícitas.

## Pruebas, distribución y definición de terminado

Cada cambio de lógica no trivial deja una comprobación ejecutable mínima que falle ante la regresión; ampliar las existentes antes de crear infraestructura nueva. Lógica pura sin micrófono/cámara/UIA real en las pruebas rápidas; integración física por separado. Pruebas nunca usan claves ni proveedores reales por defecto.

Para cambios implementados, compilar los targets afectados en Windows y ejecutar comprobaciones. Priorizar parser Unicode, deduplicación, apagado durante inicio, pérdida/retorno, dwell a 15/30/60 FPS, matrices y exclusiones Lite. Probar UI por teclado y lector, bandeja/panel, bloqueo y suspensión, DPI mixto, permisos y dispositivos reales según el módulo.

Si se trabaja desde macOS, se pueden revisar fuentes y lógica portable, pero declarar como pendientes WPF/Win32, voz, sensores e instaladores. No presentar pruebas macOS o una compilación cruzada como validación Windows.

Distribución: paquetes por edición, runtime y arquitectura. Verificar dependencias nativas, firma Authenticode, prerrequisitos, inicio voluntario y desinstalación sin residuos activos. MSIX no es la única vía si se pretende cubrir Windows 7/8; evaluar instalador MSI/EXE para Legacy. No pedir desactivar antivirus, SmartScreen ni UAC. Publicar sólo cuando el usuario haya autorizado la distribución.

Instalador actual: NSIS 3.13, sólo Windows 10/11 x64, por usuario en `%LOCALAPPDATA%\Programs\AdaptaPE`, .NET autocontenido y Visual C++ oficial como prerrequisito condicional. `scripts/installer.ps1` genera lista de borrado específica del paquete. Conservar archivos ajenos, preferencias y runtimes compartidos; no usar `RMDir /r $INSTDIR`. No elevar la app al finalizar ni habilitar inicio con Windows durante instalación. Cerrar la app antes de actualizar/desinstalar.

Actualizar README al añadir comandos, funciones, requisitos o resultados de validación. Actualizar AGENTS al cambiar responsabilidades o contratos. No enlazar archivos inexistentes como instrucciones disponibles ni inventar URLs de releases. Conservar atribuciones y licencias de fuentes/modelos; añadir los avisos correspondientes antes de empaquetar.

Una entrega está terminada cuando el comportamiento solicitado funciona en los targets probados, las comprobaciones pertinentes pasan, Lite conserva su alcance y la documentación separa lo validado de las limitaciones. Informar de forma breve qué cambió, qué se verificó y qué queda sin verificar; no afirmar compatibilidad universal.

### Refinamiento 1.0.1

- `Theme.Asset` conserva imágenes congeladas compartidas; acceso desde el hilo WPF. PiP reutiliza el mismo bitmap de cámara, no abre otro sensor. Interruptores PiP llaman `SetVoice`/`SetCamera`; su sincronización no dispara cambios. Cámara tiene estado separado de la respuesta de voz.
- `Run` muestra la intención detectada; sólo una respuesta de acción acredita su resultado. Conservar errores visibles y no cambiar transcripciones al mostrar opciones. Anillos y punto relativo siguen visibles en preview durante calibración/pausa cuando están disponibles.
- NSIS utiliza BMP de 24 bits derivados de la marca original. Detecta Windows/build y versión en registro/ubicación propia, permite actualizar/reparar y bloquea downgrade. Selección de accesos escritorio/Inicio persistida en HKCU; no añade búsqueda global de portables, selector ficticio de runtime ni updater de red.
- Regenerar memoria persistente `adapta-pe-win` al terminar cambios. Revisar cobertura por fuente; `installer.nsi` se lee directamente porque el grafo no analiza NSIS. `docs/MEMORIA-CODIGO.md` conserva mapa y límites sin atribuir ejecución Windows al índice.

### Voz y comandos 1.0.2

- `Core/RecognitionSession.cs` invalida arranques anteriores y espera liberación del motor. Cambiar motor/micrófono/palabra conserva intención de escucha; Detener todo cancela la espera. Mantener regresión portable de cambios rápidos y parada durante liberación.
- `Commands.NaturalActions`, `NaturalShortcuts` y `SpeechPhrases` conservan frases finitas compartidas con SAPI. No reemplazar dictado libre ni hacer coincidencia aproximada de verbos de acción.
- `Applications.OpenWindows`/`FindRunning` conservan HWND/PID, nombre de proceso y título. App Paths complementa Inicio. Alias de Google identifican Chrome; sitio Google explícito conserva navegación web. Parecidos requieren elección y deben mantener destino externo.

### Versión pública y revisión técnica

- Base y Lite muestran versión pública `1.0`, definida en `Version` del proyecto y leída del atributo informativo por MainWindow/PiP. No volver a mantener números literales independientes en la UI.
- `FileVersion` es revisión técnica de cuatro componentes, actualmente `1.0.3.0`. NSIS recibe `VERSION` público y `FILE_VERSION` técnico; registra DisplayVersion y BuildVersion por separado. Comparar revisión técnica y conservar fallback para instalaciones anteriores y EXE propio, incluidos los cuatro componentes. No retirar protección de downgrade para renombrar una entrega.

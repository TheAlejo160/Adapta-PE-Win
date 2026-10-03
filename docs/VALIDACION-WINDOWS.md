# Validación pendiente en Windows

El usuario confirmó funcionamiento de la entrega 0.2.1. Los cambios de 1.0.0/1.0.1 se compilan desde macOS y todavía necesitan pruebas de voz, cámara, cursor e instalación en Windows. No se han medido CPU, RAM, GPU, latencia o precisión con dispositivos reales.

## Antes de distribuir

Registrar edición/build de Windows, arquitectura, runtime, GPU, cámaras, micrófonos, reconocedor español y versión del paquete. Ejecutar `scripts/checks.ps1` y verificar:

1. Voz SAPI y motor Local incluido, arranque sin reconocedor SAPI, indicador de audio y dictado real español; dispositivos predeterminados y seleccionados, desconexión y recuperación. Confirmar que TTS no ejecuta órdenes ordinarias y que «Computadora, silencio» interrumpe.
2. Bloc de notas, campos web Edge/Chrome, controles ambiguos, cambio de ventana, selección eliminada, contraseñas y restricciones UIPI. Leer por número debe responder con el contenido del control elegido.
3. Cabeza, ambas manos, torso, brazos y zona libre. Calibración inicial, pérdida, cambio de fuente, pausa física dos segundos y ausencia de clic accidental. Comprobar cámara visible en Control/PiP durante calibración y pausa; anillos y progreso junto al cursor; reanudar dwell tras pérdida breve de objetivo; subpíxeles y Perfil ágil. Variar FPS y sensibilidad.
4. Filtros: identidad, colores de referencia, estado previo, convivencia con Lupa/filtros externos, apagar, salir y reiniciar después de terminación abrupta. Windows 7 usa capa de magnificación; confirmar que no bloquea entradas ni crea realimentación visual.
5. Monitores con coordenadas negativas, DPI mixto y panel anclado. Desconectar un monitor, cambiar escala y barra de tareas.
6. Primer inicio apagado, restauración explícita, cerrar ajustes, bandeja, panel, Ctrl+Shift+S, bloqueo, suspensión y salida. Detener durante arranque de cámara o consulta IA.
7. IA apagada y sin clave; HTTPS, error HTTP, timeout, cancelación, respuesta grande, captura sólo con consentimiento por sesión. Una búsqueda normal abre navegación web; no confundirla con envío a IA.
8. Navegar completamente por teclado, Narrador y NVDA, campos con nombres accesibles, contraste y foco visible.
9. Ejecutar `scripts/check-voice.ps1` con motor Automático y luego Local incluido; exige 30 segundos de escucha y apaga Voz al terminar. Después probar una orden hablada real. Confirmar recuperación de SAPI una vez, parada durante carga del modelo y ausencia de reactivación por callbacks antiguos.
10. Instalador en Windows 10/11 x64, cuenta estándar, Visual C++ presente/ausente y permiso cancelado; accesos en escritorio/Inicio, arranque sin .NET preinstalado, actualización y bloqueo con app abierta. Desinstalar desde Configuración: retirar archivos propios/accesos/inicio propio, conservar preferencias y cualquier archivo ajeno añadido a la carpeta. No desinstalar runtimes compartidos.
11. Cerrar Chrome, Bloc, Word, calculadora UWP y aplicaciones con varias ventanas; frases «cierra», «cerrar la aplicación», artículos y puntuación. Un navegador con título «Microsoft Word» no debe confundirse con Word. Nombres parecidos exigen elegir; ventana cerrada/reemplazada antes de elegir debe cancelar el cierre. Verificar diálogos de guardar sin terminación forzada.
12. Preview con detector a 5/12/30 FPS y modelos de manos/cuerpo cargando: debe seguir mostrando cámara fresca sin cola ni JPEG. Verificar 30 minutos, ocultar/reabrir Control/PiP, detener durante apertura/carga, desconectar cámara y reactivar. Medir FPS reales y RAM; el objetivo de 24 FPS no garantiza que cualquier controlador lo alcance.
13. Misma postura a 5/8/12/15/30 FPS: velocidad comparable, movimiento fluido, frenado al volver al centro, entrada física y pérdida detienen movimiento. Probar monitor 1080p/4K/DPI mixto, equilibrado/ágil/preciso y comandos de velocidad/dwell. Una inferencia lenta o escritorio seguro nunca debe dejar velocidad vigente indefinidamente.

## Compatibilidad histórica

`net48` y `net461` son compilaciones distintas. Compilar no prueba que el runtime nativo OpenCV, sus modelos, Visual C++ Redistributable y controladores funcionen en el sistema antiguo.

`net461` utiliza OpenCvSharp 4.5.3 con soporte de framework explícito. Su importador ONNX antiguo puede rechazar alguno de los modelos actuales; este target es experimental y no acredita paridad de cámara en Windows 8.0. No sustituirlo por paquetes .NET Standard incompatibles ni mezclar wrapper/runtime de versiones diferentes. Verificar primero esa combinación; si falla, convertir y validar modelos para el importador antiguo o retirar Windows 8.0 de la matriz publicada, documentando la decisión.

Windows 7 SP1, 8.0 y 8.1 están fuera de soporte de seguridad. La compatibilidad de ejecución no prolonga soporte de Microsoft.

## Medir consumo

Con Administrador de tareas o Windows Performance Recorder, medir cinco minutos por escenario: todo apagado, voz sola, cabeza a 12 FPS, manos, torso, zona libre, filtro y combinación. Repetir en un equipo modesto con dos núcleos y sin GPU dedicada. Registrar promedio y pico de CPU, RAM privada y latencia de interacción, no sólo capturas instantáneas.

La app usa cámara de trabajo 320×240, un hilo de cómputo OpenCV, inferencia serial y un frame pendiente, máximo configurable 5–30 detecciones/s; preview visible hasta 24 FPS, bitmap/buffers reutilizados y movimiento independiente cada ~15 ms. La velocidad caduca a los 350 ms desde captura. Los modelos sólo se cargan al usarlos y se liberan al apagar cámara. Filtros globales Windows 8+ no capturan pantallas en el código; Windows 7 refresca a 10 FPS. UIA consulta como máximo 1800 nodos/800 ms por operación; un proveedor COM bloqueado requiere aislamiento adicional si aparece en pruebas reales.

La versión 0.2.0 corrige el contrato del flujo de audio que SAPI consultaba y añade un motor español incluido. La regresión portable comprueba metadatos del stream y decodificación de resultados; no sustituye una prueba de reconocimiento con voz real. Verificar también x64 y dependencias nativas de Vosk en cualquier Windows histórico.

La versión 0.2.1 sustituye el callback de audio por evento nativo y procesa buffers en orden. [Microsoft prohíbe llamadas de audio dentro de waveInProc por riesgo de bloqueo](https://learn.microsoft.com/en-us/previous-versions/dd743849(v=vs.85)). El fallo de reinicio SAPI queda cubierto por la comprobación Windows de escucha; no se ejecuta desde macOS.

## Instalador y PiP 1.0.1 pendientes

- Instalación nueva, actualización 0.2.1/1.0.0 y reparación 1.0.1; rechazar versión anterior a la instalada. Probar registro presente con archivos ausentes y EXE en carpeta propia sin registro.
- Elegir ambos accesos, ninguno o sólo uno; volver atrás en asistente y verificar elección conservada. Confirmar registro, accesos, preferencias y desinstalación. No borrar archivos añadidos por el usuario.
- Confirmar detección Windows 10/11 x64 y rechazo de sistemas/arquitecturas distintos. No elegir Legacy desde el EXE moderno. Probar Visual C++ presente, instalación y cancelación del prerrequisito.
- Cuy/header visibles y legibles con DPI 100/150/200 %, teclado y lector de pantalla. Confirmar que ninguna página se recorta.
- PiP: activar/desactivar voz/cámara; mostrar orden detectada, respuesta y error. Cámara con anillos y punto relativo, incluso durante pausa/calibración; comprobar que cambios de estado no borran respuesta.
- Ventana principal y PiP en 1366×768, varias pantallas y DPI mixto. Cerrar ajustes mantiene sensores; Detener todo anula entrada. Verificar foco visible y todos los controles por teclado.

La entrega 1.0.1 comprueba parser, compilación C# y compilación del asistente NSIS desde macOS. No hay mediciones físicas nuevas de FPS, RAM o CPU.

## Regresión de voz y apps 1.0.2 pendiente en Windows

1. Con voz activa, cambiar Local → Automático → SAPI → Local y micrófono varias veces. Debe quedar activo sólo el último motor. Cambiar con voz apagada debe conservarla apagada.
2. Apagar/Detener todo mientras carga Vosk o libera el motor anterior. Esperar a que termine: no debe reaparecer escucha, señal ni captura. Probar suspensión durante el cambio.
3. Comparar reconocimiento hablado de «Computadora, cerrar Google», «me puedes cerrar la app Chrome porfa», clic derecho, doble clic y lectura de pantalla con SAPI y Local. Registrar reconocedor, micrófono y errores sin guardar audio/transcripciones.
4. Chrome instalado sin acceso en Inicio, Chrome portable ya abierto, varios perfiles/ventanas y otra app con documento titulado Google Chrome. Nombres mal reconocidos deben exigir elección; cerrar/reemplazar una ventana antes de elegir debe cancelar.
5. «Muestra apps abiertas», «cambia a Discord», cambiar destino antes de elegir y confirmar que Adapta no sustituye el destino. Colocar cursor fuera del destino y pedir clic: debe mostrar error sin pulsar otra ventana.
6. Dictado Unicode, texto que contiene «cerrar Chrome», negaciones y síntesis propia. Verificar pestaña anterior/siguiente para descartar conflictos con cambio de app. Lectura sigue excluyendo contraseñas.

Estos casos requieren Windows real. Las comprobaciones de generación/tareas usan una liberación controlada y no prueban el driver WinMM, SAPI ni Vosk nativo.

Comprobaciones de esta entrega: 747 sin sensores. Compilación cruzada de `net10.0-windows`, `net48` y `net461`; Una consulta inicial de vulnerabilidades NuGet no estuvo disponible; la compilación final termina sin errores ni advertencias. Instalador local x64 generado; ejecución e instalación Windows pendientes.

## Numeración pública 1.0

Validar en Windows que ventana, PiP, asistente, Configuración/Desinstalar y propiedades de versión del producto indiquen 1.0 en ambas ediciones. FileVersion técnico es 1.0.3.0. Actualizar desde Base 1.0.2 y Lite 1.0.1 (registro sin BuildVersion); reparar la misma revisión; bloquear una revisión superior. Probar EXE propio sin registro con una revisión en el cuarto componente. Conservar accesos/preferencias y comprobar desinstalación.

Verificación de la numeración unificada: 750 comprobaciones aprobadas sobre el paquete, incluida versión de producto 1.0 y revisión de archivo 1.0.3.0. Compilación cruzada sin errores ni advertencias en los tres targets; fuentes NSIS y migración de claves revisadas directamente. Instalación/actualización, WPF y reconocimiento con micrófono permanecen pendientes en Windows.

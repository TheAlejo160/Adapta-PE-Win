<p align="center"><img src="assets/brand/logo.png" alt="Cuy de Adapta PE" width="128"></p>

# Adapta PE para Windows

Aplicación **Base completa en C# y WPF**, basada en las funciones de Adapta PE para navegador y macOS. Incluye voz, control de aplicaciones, cursor cinético, lectura accesible, siete filtros, bandeja, panel flotante e IA externa opcional. Conserva el fondo y logotipo originales de macOS, con icono propio en ejecutable, ventanas y bandeja. No requiere extensión, Swift, Python ni servidor local.

**Estado:** el usuario confirmó arranque, instalación y uso de la versión 0.2.1 en Windows. La versión **1.0** amplía voz y comandos; incorpora también el refinamiento previo; compilación y lógica portable verificadas desde macOS. Falta comprobar sus cambios de cámara, cursor y comandos con sensores reales. No se han medido CPU/RAM ni acreditado toda la matriz de compatibilidad. Consulta [validación Windows pendiente](docs/VALIDACION-WINDOWS.md).

**1.0.1:** instalador con cuy original, detección de Windows 10/11 x64, instalación/actualización/reparación y protección frente a versiones más antiguas. Accesos de escritorio/Inicio elegibles, conservados al actualizar. Interfaz más sobria, funciones principales agrupadas, foco visible y PiP con activación de voz/cámara, orden detectada, respuesta/error y estado de calibración. Mantiene cámara y guías independientes del detector. Comparte alias naturales de la extensión; «cancelar clic» retira sólo el clic pendiente. Imágenes de marca cargadas una vez; no se añade un framework visual ni animaciones continuas.

## Voz y comandos: versión 1.0

Cambio de reconocimiento, micrófono o palabra de activación conserva la escucha si estaba activa. Espera a que el motor anterior libere audio/modelo; varios cambios rápidos conservan sólo el último. Detener todo durante esa espera impide el arranque pendiente. Cambiar ajustes con voz apagada no la activa.

SAPI combina dictado libre con una gramática finita de comandos, palabra de activación y nombres del catálogo instalado. Prefiere un reconocedor español de la cultura del usuario cuando existe. El motor local mantiene su modelo español y dictado libre; no se ha medido una mejora de precisión acústica con micrófonos reales. Un resultado SAPI de baja confianza muestra diagnóstico y no ejecuta la orden.

El parser admite más de cien frases adicionales y cortesía como «oye», «me puedes», «podrías», «quiero que», «necesito» y «hazme el favor de». Ejemplos:

- «Computadora, me puedes cerrar la app Google por favor»: Google/Chrome/crom se resuelven al proceso Chrome.
- «Computadora, cambia a Discord», «trae Chrome al frente», «muestra apps abiertas»: busca ventanas abiertas sin depender de un acceso directo. Abrir una app ya abierta enfoca su ventana; varias ventanas o coincidencias aproximadas requieren elección.
- «Computadora, hazme un clic derecho», «abre el menú contextual», «doble clic»: clic en la aplicación de destino bajo el cursor.
- «Computadora, léeme la pantalla», «leer la pantalla», «qué dice la pantalla», «detén la lectura»: lectura manual del contenido accesible de la ventana externa. No añade OCR ni promete leer contenido que UIA no publica.
- «Computadora, guarda el documento», «reabre la pestaña», «muestra descargas», «ve al final», «maximiza la ventana».
- «Computadora, apaga la voz»: detiene escucha; se reactiva mediante ajustes o PiP.

Catálogo de Inicio más App Paths de HKCU/HKLM en ambas vistas del registro. Sólo abre ejecutables/accesos existentes; no acepta argumentos shell dictados. Chrome puede detectarse por registro aunque no tenga acceso de Inicio. «Abre la página de Google» elige explícitamente el sitio; «abre Google» puede enfocar/abrir Chrome si está disponible. Se conservan las 28 rutas de sitios.

Nombres parecidos exigen elegir incluso si hay una sola posibilidad. Cierre conserva HWND/PID de la ventana enumerada, los revalida y solicita WM_CLOSE; no termina procesos. Un título de página que menciona Word no acredita que Chrome sea Word. Dictado conserva Unicode y texto literal; frases negadas no activan acciones por aproximación.

Validación de esta actualización: comprobaciones portables y compilación cruzada desde macOS. Reconocimiento hablado, cambio de motor, ventanas reales, UIA y ejecución del instalador siguen pendientes en Windows.

## Contenido

- [Instalación y actualización](#instalación-y-actualización)
- [Instalar desde el repositorio](#instalar-desde-el-repositorio)
- [Primer uso](#primer-uso)
- [Panel PiP](#panel-pip)
- [Funciones y comandos](#funciones-y-comandos)
- [Diseño para consumo contenido](#diseño-para-consumo-contenido)
- [Compatibilidad](#compatibilidad)
- [Código y comprobaciones](#código-y-comprobaciones)
- [Solución de problemas](#solución-de-problemas)
- [Privacidad e IA opcional](#privacidad-e-ia-opcional)

**1.0.0:** cierre por nombre con artículos, imperativo/infinitivo, puntuación y alias habituales de reconocimiento. Identifica ventanas por proceso antes de usar títulos, enumera todas las ventanas visibles y solicita elección para nombres aproximados o varias ventanas. Preview hasta 24 FPS independiente de inferencia, bitmap y buffers reutilizados, sin codificación JPEG ni cola de imágenes atrasadas. Cursor actualizado por separado aproximadamente a 60 Hz, velocidad por segundo y frenado al volver a zona segura. Perfiles equilibrado/ágil/preciso y órdenes de velocidad/dwell. Mantiene ajustes personales al actualizar; el perfil equilibrado aplica los nuevos valores recomendados.

**0.2.1:** instalador EXE por usuario con .NET incluido, accesos en escritorio/Inicio y desinstalación desde Configuración. Captura WinMM mediante `CALLBACK_EVENT`, sin llamadas de audio dentro de callbacks del driver; buffers conservan orden temporal. Eliminados reintentos de SAPI tras terminar la escucha: Automático cambia una vez al motor español incluido; SAPI explícito muestra el error original. Detener invalida callbacks anteriores; la recuperación conserva la ventana de activación de voz.

**0.2.0:** interfaz con fondo, marca, cuatro secciones y panel similares a macOS. Vista de cámara visible en Control y PiP, también durante calibración y pausa. Anillos de postura, landmarks, barra de dwell y porcentaje junto al cursor. Conservación de fracciones de píxel y velocidad por tiempo a 5–30 FPS; perfil Ágil configurable. Corregido el contrato de audio continuo consultado por SAPI, causante de «Specified method is not supported». Reconocimiento español local incluido como alternativa, indicador de audio y selección de motor. Estas correcciones están compiladas; su funcionamiento físico debe confirmarse en Windows.

**0.1.1:** corregido el acceso a `HwndSource` al crear la ventana antes de mostrarla. `EnsureHandle` todavía no conecta el árbol visual: ahora se obtiene la fuente mediante el HWND. Los errores de arranque muestran un diálogo y cierran la instancia, en lugar de quedar en una ventana oculta. Compilación comprobada desde macOS; confirmar apertura de ajustes en Windows con `scripts/check-startup.ps1`.

## Instalación y actualización

Para uso normal en Windows 10/11 x64, ejecuta **`artifacts/AdaptaPE-1.0-Setup-x64.exe`**. Instala en `%LOCALAPPDATA%\Programs\AdaptaPE`, normalmente dentro de `C:\Users\<usuario>\AppData\Local`, y permite elegir accesos directos en escritorio e Inicio. Las dependencias permanecen en esa carpeta; se abre la app desde su icono. No activa inicio automático con Windows. Cierra la versión anterior desde la bandeja antes de actualizar.

El asistente muestra Windows detectado y la ubicación. No hace falta seleccionar manualmente un sistema operativo: este EXE contiene la edición principal x64; elegir “Windows 7” no la convierte en Legacy. Para 7/8 se compilan paquetes separados. La instalación es por usuario y conserva una ubicación fija para actualizar y desinstalar con seguridad.

Si encuentra una instalación registrada o un EXE en esa ubicación, muestra **Actualizar** para una versión anterior y **Reparar** para la misma versión. Bloquea reemplazar una versión más reciente. Conserva preferencias y la elección de accesos directos. No busca archivos privados ni EXE portables en toda la carpeta Descargas; un ZIP portable fuera de la carpeta de instalación sigue siendo independiente.

Las actualizaciones son **manuales**: cierra Adapta PE desde la bandeja y ejecuta el nuevo instalador. La consulta de nuevas versiones a la web/GitHub queda como trabajo futuro; no hay comprobaciones de red al iniciar ni un actualizador añadido.


El instalador incluye .NET y modelos locales. Si falta Visual C++ x64 compatible, abre el instalador oficial Microsoft incluido; ese componente puede pedir autorización de administrador. Después abre Adapta PE como usuario normal. No elimina runtimes compartidos al desinstalar. Preferencias y clave cifrada permanecen en `%LOCALAPPDATA%\AdaptaPE`; la desinstalación borra sólo archivos conocidos del paquete y sus accesos. La ejecución del instalador/desinstalador aún requiere validación Windows.

Alternativa portable: conserva toda la carpeta de publicación. Sólo los paquetes **no autocontenidos** requieren instalar **.NET Desktop Runtime 10** por separado. Para compilar, instala SDK .NET 10 y herramientas de escritorio compatibles.

Desde la raíz del repositorio, en PowerShell:

```powershell
# Edición principal: ejecutable Windows x64 dependiente del Desktop Runtime 10.
./scripts/build.ps1

# Paquete con runtime incluido: mayor tamaño en disco.
./scripts/build.ps1 -SelfContained

# Instalador moderno autocontenido. Requiere NSIS 3.13 (makensis en PATH).
./scripts/installer.ps1

# Legacy: requiere .NET Framework 4.8 instalado.
./scripts/build.ps1 -Framework net48

# Windows 8.0: compilación histórica experimental, runtime 4.6.1.
./scripts/build.ps1 -Framework net461
```

Cada paquete queda en `artifacts/<framework>-<runtime>/`. Ejecuta **AdaptaPE.exe** en Windows conservando toda su carpeta, incluidos los DLL y `models/`. Los paquetes OpenCvSharp necesitan Visual C++ Redistributable compatible con su runtime y los componentes multimedia de Windows; las ediciones N/KN pueden necesitar Media Feature Pack. Un ejecutable aislado sin sus dependencias no es el paquete completo.

Los proyectos Framework usan paquetes de ensamblados de referencia para compilar. El runtime .NET Framework del equipo es un requisito separado. El instalador moderno todavía no tiene firma Authenticode; Windows puede mostrar editor desconocido. No desactives UAC, antivirus ni SmartScreen. Legacy no usa este instalador.

`assets/prerequisites/manifest.json` fija URL y SHA-256 de Visual C++ descargado de Microsoft. `scripts/installer.ps1` verifica ese archivo antes de empaquetar; la carpeta de publicación incluye avisos de licencia de .NET. Compilación de instalador desde macOS: publica `--self-contained true`, genera `artifacts/uninstall-files.nsh` con los archivos publicados y llama a `makensis` con las definiciones de `scripts/installer.ps1`. [NSIS](https://nsis.sourceforge.io/Docs/), [Visual C++ oficial](https://learn.microsoft.com/en-us/cpp/windows/latest-supported-vc-redist).

## Instalar desde el repositorio

Código fuente: [TheAlejo160/Adapta-PE-Win](https://github.com/TheAlejo160/Adapta-PE-Win). La publicación de fuentes no acredita una release instalable validada. `artifacts/` se genera localmente y no se descarga al clonar. Abre PowerShell en la raíz del repositorio Windows.

Requisitos para construir: SDK .NET 10 y acceso a NuGet. Para crear el EXE instalador: NSIS 3.13 (`makensis` en PATH o ruta explícita). Cámara y voz requieren Windows, los modelos incluidos y las DLL nativas. El usuario final recibe el instalador autocontenido y no necesita instalar el SDK.

```powershell
# Desde la raíz del código Windows:
dotnet restore src/AdaptaPE/AdaptaPE.csproj
./scripts/checks.ps1
./scripts/build.ps1 -Framework net10.0-windows -Runtime win-x64 -SelfContained
# Conserva toda la carpeta publicada y ejecuta AdaptaPE.exe en Windows.
./scripts/installer.ps1 -MakeNsis "C:/Program Files (x86)/NSIS/makensis.exe"
# Resultado: artifacts/AdaptaPE-1.0-Setup-x64.exe
```

Si PowerShell bloquea un script descargado, revísalo y desbloquea sólo ese archivo con `Unblock-File ./scripts/build.ps1`; no cambies la política global del equipo. También puedes ejecutar directamente los comandos `dotnet` de la sección Código. `assets/prerequisites/VC_redist.x64.exe` debe coincidir con el SHA-256 de su manifiesto; el empaquetado rechaza otro archivo.

| Script / parámetro | Valores y resultado |
| --- | --- |
| `build.ps1 -Framework` | `net10.0-windows` (predeterminado), `net48`, `net461` |
| `build.ps1 -Runtime` | `win-x64` (predeterminado); `win-x86` es experimental y no incluye Vosk x64 |
| `build.ps1 -SelfContained` | Incluye .NET en la publicación moderna. No se admite para .NET Framework |
| `installer.ps1 -MakeNsis` | Ejecutable `makensis`, predeterminado desde PATH. Empaqueta sólo Windows 10/11 x64 |
| `check-startup.ps1 -Executable` | Ruta del EXE publicado. Comprueba ventana inicial en Windows |

`build.ps1` produce `artifacts/<framework>-<runtime>/`. `installer.ps1` publica en `artifacts/net10.0-windows-win-x64-contained/`, incluye licencias y genera una lista específica para desinstalar. No edita preferencias ni activa sensores durante la compilación. No hay instalador Legacy/ARM64 acreditado.

### Clonar, compilar e instalar desde PowerShell

En Windows 10/11 x64 con WinGet disponible, instala Git y el **SDK** .NET 10 si faltan. Abre otra terminal después de instalarlos para actualizar el PATH. Si WinGet no está disponible, utiliza los instaladores oficiales de Git y [.NET](https://learn.microsoft.com/en-us/dotnet/core/install/windows).

```powershell
winget install --id Git.Git --exact
winget install --id Microsoft.DotNet.SDK.10 --exact
```

```powershell
git clone https://github.com/TheAlejo160/Adapta-PE-Win.git
Set-Location Adapta-PE-Win
dotnet --info
dotnet restore src/AdaptaPE/AdaptaPE.csproj
./scripts/checks.ps1
./scripts/build.ps1 -Framework net10.0-windows -Runtime win-x64 -SelfContained
# Ejecutar la publicación portable en Windows; conservar toda la carpeta.
& ./artifacts/net10.0-windows-win-x64/AdaptaPE.exe
```

Para instalar con accesos directos y desinstalador, cierra la app desde la bandeja, instala **NSIS 3.13** y genera el instalador. Puedes comprobar su disponibilidad con `winget show --id NSIS.NSIS --exact --versions` y usar `winget install --id NSIS.NSIS --exact --version 3.13`, o descargarlo desde [NSIS](https://nsis.sourceforge.io/Download). La compilación exige `makensis` en PATH o su ruta explícita.

Base requiere además el redistribuible oficial de Visual C++ fijado en [su manifiesto](assets/prerequisites/manifest.json). El binario descargado se excluye de Git. Descárgalo desde PowerShell y verifica su hash antes de empaquetar:

```powershell
$adaptaPrerequisite = Get-Content ./assets/prerequisites/manifest.json -Raw | ConvertFrom-Json
$adaptaVcPath = Join-Path (Get-Location) 'assets/prerequisites/VC_redist.x64.exe'
Invoke-WebRequest -Uri $adaptaPrerequisite.url -OutFile $adaptaVcPath
if ((Get-FileHash $adaptaVcPath -Algorithm SHA256).Hash.ToLowerInvariant() -ne $adaptaPrerequisite.sha256) {
    throw 'Microsoft actualizó la descarga o el archivo no coincide. No empaquetes: revisa la procedencia y el manifiesto.'
}
```

La URL oficial puede cambiar de contenido. Si no coincide, no omitas la comprobación ni cambies el hash sin revisar el archivo. La app portable también necesita Visual C++ compatible y componentes multimedia; el instalador gestiona condicionalmente el redistribuible incluido. [Requisitos oficiales de Visual C++](https://learn.microsoft.com/en-us/cpp/windows/latest-supported-vc-redist).

```powershell
./scripts/installer.ps1 -MakeNsis 'C:/Program Files (x86)/NSIS/makensis.exe'
Start-Process ./artifacts/AdaptaPE-1.0-Setup-x64.exe -Wait
# Después de completar el asistente de instalación:
& "$env:LOCALAPPDATA/Programs/AdaptaPE/AdaptaPE.exe"
```

Para actualizar las fuentes, usa `git pull --ff-only`, vuelve a compilar y ejecuta el nuevo instalador. Conserva tus cambios locales antes de actualizar. El instalador admite reparación y bloquea una revisión técnica anterior. Para desinstalar, cierra Adapta y usa Configuración de Windows o el `Uninstall.exe` de su carpeta de instalación; se conservan preferencias personales.

Estos comandos describen el flujo Windows. En macOS sólo se puede compilar de forma cruzada y ejecutar las comprobaciones portables; no se ejecutan WPF, Win32 ni el instalador. Legacy se construye con `-Framework net48` o `-Framework net461` según la tabla de compatibilidad y necesita su runtime instalado, sin `-SelfContained`.

## Panel PiP

Abre **Panel flotante · PiP**. Se ancla al monitor/esquina elegidos en Ayuda y conserva acceso a Ajustes y Detener todo. Los interruptores Voz/Cursor actúan sobre los mismos motores que la ventana principal. El panel muestra texto escuchado, **Orden detectada** al entender la intención y después respuesta o error. Detectar una orden no significa que Windows haya permitido ejecutarla; revisa la respuesta.

Con cámara activa muestra preview local, anillos de zona segura, punto blanco de movimiento y landmarks disponibles. Verde marca la zona segura; naranja, el anillo para armar dwell; el punto blanco muestra desplazamiento respecto a la postura calibrada. La imagen se actualiza hasta 24 FPS, aunque la inferencia use menos detecciones/s. Calibración o pausa conserva la imagen; apagar cámara la retira. No se graban imágenes.

## Ajustes principales

| Ajuste | Predeterminado / efecto |
| --- | --- |
| Nombre del asistente | `Computadora`; espera ocho segundos para la orden tras la señal |
| Motor de voz | Automático: SAPI español, con fallback al local incluido |
| Micrófono | Dispositivo predeterminado de Windows; seleccionable |
| Control corporal | Cabeza; también automático, manos, torso, brazos y zona libre |
| Velocidad / amplitud | `1` / `0,65`; menor amplitud requiere movimientos menores |
| Permanencia | `1 s`, ajustable de `0,4` a `5 s`; un clic por entrada al centro |
| Detecciones/s | `12`; seleccionables de `5` a `30`, independientemente de preview |
| Temblor | Apagado; añade filtrado y recalibra al cambiar |
| Arranque con Windows | Apagado; optativo desde Ayuda, nunca desde instalador |
| IA externa | Apagada; endpoint HTTPS, modelo y clave propios |

## Primer uso

1. Primer lanzamiento con asistencia apagada. Enfoca una aplicación externa, por ejemplo Bloc de notas.
2. En Control, abre Dispositivos y reconocimiento, elige micrófono y activa Voz y di **«Computadora, muestra campos»**. También puedes escribir una orden en ajustes.
3. Automático intenta SAPI español instalado y usa el reconocedor local incluido si no está disponible, falla al arrancar o termina inesperadamente la escucha. No reinicia indefinidamente el mismo SAPI. Puedes elegir Local incluido directamente. SAPI necesita reconocimiento español y dictado; una voz TTS no basta. El indicador de audio muestra si el micrófono recibe señal. Los enlaces abren permisos del micrófono y ajustes de sonido en Windows 10/11. Un error real de dispositivo se muestra y requiere corregir permisos/dispositivo; no se oculta como un problema genérico de micrófono.
4. En Control, abre Cámara y consumo para elegir dispositivo y Adaptar a mi movimiento para elegir modo. Activa el cursor y mantén postura cómoda para calibrar. La vista muestra cámara y anillos de postura. Perfil equilibrado aplica sensibilidad 1 y amplitud 0,65; Ágil aplica 1,5 y 0,65. La curva tiene zona de precisión y alcanza 1.200 px/s con sensibilidad 1; el límite es 2.400 px/s, o 1.600 con temblor activo. Son parámetros del algoritmo, no latencia o velocidad efectiva medida en el equipo. Ajusta a tu comodidad; volver a la zona segura frena.
5. Para hacer clic por permanencia, sal del anillo y vuelve al centro manteniendo el mismo objetivo. Sólo se arma un clic por entrada. El porcentaje aparece junto al cursor y también en ajustes/PiP. Las superficies sin semántica UIA usan una región de 24×24 px y ventana verificada; perder el objetivo reinicia la espera. El ratón físico pausa el control durante dos segundos.
6. En Lectura y color, elige un filtro o activa TalkBack automático. Cerrar ajustes conserva asistencia y acceso desde bandeja/panel.
7. **Detener todo** cancela operaciones, libera sensores, detiene lectura y retira el filtro propio. **Ctrl+Shift+S** es la parada de emergencia si Windows permite registrar el atajo.

«Salir» conserva preferencias para otro inicio; «Detener todo» recuerda asistencia apagada. El inicio con Windows es voluntario. Bloqueo y suspensión detienen entrada/sensores; al volver se revalidan recursos y se recalibra el cursor. Restablecer un dispositivo puede requerir reactivarlo.

## Funciones y comandos

Precede la orden con «Computadora» o la palabra elegida. Decir sólo la palabra emite una señal y abre ocho segundos para el comando. Durante lectura se aceptan interrupciones explícitas, no órdenes ordinarias derivadas del audio.

- **Aplicaciones y sitios:** «abre Bloc de notas», «abre You Tube en nueva ventana», «busca movilidad en Wikipedia», «abre Visual Studio Code en el monitor dos». Catálogo local de apps y 28 sitios/alias heredados. Los nombres ambiguos ofrecen elecciones.
- **Navegación y teclado:** «nueva pestaña», «atrás», «recarga», «guardar», «guardar como», «imprimir», «mostrar escritorio», «presiona control shift N». Atajos adaptados a Windows, no códigos de macOS.
- **Campos y controles:** «muestra campos», «enfoca Nombre», «escribe Hola, José en el campo Nombre», «selecciona Contacto», «pulsa lo seleccionado», «selecciona el texto Buenos días», «borra el campo». Dictado conserva Unicode y signos; contraseñas quedan excluidas.
- **Elecciones:** «elige dos», «la segunda», número, nombre único, «siguiente», «anterior» o «cancelar». Hasta seis por página; se cancelan al cambiar el destino o después de 45 segundos.
- **Lectura y menús:** «lee la pantalla», «lee el botón Contacto», «muestra comandos», «ejecuta la acción Exportar PDF», «silencio». UI Automation depende de la semántica publicada por cada app.
- **Cinética:** «control cabeza», «control manos», «control torso», «control brazo izquierdo», «control zona libre», «calibrar cursor», «activar temblor», «movimientos pequeños», «cursor más lento», «pausar cursor», «reanudar cursor», «siguiente clic derecho».
- **Colores:** «filtro deuteranopía», «quitar filtro». Protanomalía, protanopía, deuteranomalía, deuteranopía, tritanomalía, tritanopía y acromatopsia. Simulaciones de color heredadas, no tratamiento médico.
- **Aplicación:** «mostrar ajustes», «modo flotante», «panel esquina superior derecha», «detener todo», «salir de adapta».
- **Cerrar por nombre:** «cierra Chrome», «cerrar la aplicación Word», «ciérrame la app Bloc de notas». Se admiten alias como crom, msedge o notepad. Se prioriza identidad del proceso; un título de navegador que mencione Word no se trata como Word. Varias ventanas o coincidencias aproximadas se ofrecen como opciones y exigen elegir. Se usa `WM_CLOSE` con ventana/PID revalidados; no se fuerza cierre ni se omite el diálogo de guardado.
- **Alias de la extensión:** «regresa», «refresca la página», «cambia a la pestaña anterior», «postura cómoda», «centrar cursor», «descansar», «continuar cursor», «control rostro», «control muñón», «movilidad reducida», «cancelar clic». «Cancelar clic» retira dwell/clic derecho pendiente sin apagar cámara.
- **Ajustar cursor por voz:** «cursor más rápido», «aumenta la velocidad del mouse», «cursor más lento», «modo preciso», «modo rápido», «activar clic automático», «desactivar dwell». Se conservan «calibrar cursor», «pausar cursor», «reanudar cursor», «clic» y «clic derecho».
- **IA explícita:** «pregunta a la IA …», «analiza pantalla con IA». Disponible sólo si se activa y configura.

Se utiliza el navegador enfocado cuando se reconoce como compatible. URLs en la ventana actual/nueva se introducen en su barra mediante atajos y foco comprobado. Con otro destino se usa el manejador predeterminado. Búsqueda contextual intenta resolver el sitio mediante UIA y puede volver a Google; no garantiza acceso semántico a cualquier pestaña ni equivalencia con `chrome.tabs`. Reproducción/pausa necesita controles accesibles; no se envía espacio ciegamente a formularios.

## Diseño para consumo contenido

- Cámara de trabajo **320×240**, **12 FPS** predeterminados, configurables entre 5 y 30.
- Un hilo de cómputo OpenCV, una captura y una inferencia serial. La captura reemplaza un único frame pendiente; el detector toma el más reciente. No hay cola creciente. Una detección con más de 350 ms de antigüedad no controla el cursor.
- Sólo se ejecuta el detector correspondiente al modo activo. Modelos cargados bajo demanda, liberados al apagar cámara. Cabeza usa YuNet; manos y cuerpo usan modelos MediaPipe de OpenCV Zoo. Zona libre sigue textura con flujo óptico.
- Vista previa sólo con Control/PiP visibles, hasta 24 FPS y una notificación UI pendiente. Buffers BGR y `WriteableBitmap` reutilizados: no comprime ni decodifica JPEG por frame. Calibrar o pausar no oculta cámara. Captura solicita 30 FPS; el controlador puede entregar menos.
- Física temporal, calibración, suavizado y mediana de tres muestras al activar temblor. Actualización de cursor independiente, cada 15 ms aproximadamente; integra velocidad por tiempo real, sin acelerar al aumentar FPS. Caduca la velocidad tras 350 ms sin una muestra reciente. El retorno a zona segura frena antes de esperar el suavizado de dwell.
- Filtros globales mediante Magnification API en Windows 8+; el código no captura continuamente pantallas para esos filtros. Windows 7 utiliza una capa de magnificación a 10 FPS.
- Una captura de micrófono PCM 16 kHz; buffer acotado. UIA limita recorridos y consultas; lectura bajo cursor sólo cuando está activa o es necesaria para dwell. SAPI es la primera opción; el modelo español incluido sólo carga al activar ese motor y se libera al apagar Voz. Vosk documenta aproximadamente 300 MB de RAM para modelos pequeños; no es una medición de esta app. No utiliza GPU ni un servidor. [Modelos Vosk](https://alphacephei.com/vosk/models).
- Todo apagado: sin captura de cámara/micrófono, inferencia, lectura bajo cursor ni red. Bandeja y gestión de ventanas permanecen disponibles.

Son límites de diseño, **no mediciones de CPU/RAM ni garantía para todos los equipos**. Modelos de visión ocupan aproximadamente 25 MB; voz añade aproximadamente 58 MB. Los runtimes OpenCV, Vosk y .NET aumentan el tamaño en disco. Medir escenarios reales antes de prometer un requisito mínimo de RAM o un porcentaje de CPU.

## Compatibilidad

- **`net10.0-windows`**: edición principal para Windows 11 y prioridad de ejecución Windows 10. El soporte oficial depende de edición/build de Windows y de la matriz Microsoft; Windows 10 Home/Pro 22H2 no se declara oficialmente soportado por .NET 10.
- **`net48`**: vía Legacy para Windows 7 SP1, 8.1 y Windows 10. Compilación comprobada; ejecución del runtime OpenCV, modelos, sensores y filtros todavía pendiente.
- **`net461`**: vía histórica para Windows 8.0 con OpenCvSharp 4.5.3, su runtime de igual versión y dependencia gráfica corregida. Compilación separada; importador ONNX antiguo puede requerir conversión de modelos. **No se acredita aún paridad de cámara en Windows 8.0.**
- Arquitectura inicial x64. `scripts/build.ps1 -Runtime win-x86` construye x86 para equipos x86; Magnification API no funciona con proceso x86 en Windows x64. ARM64 no se distribuye ni valida todavía.

.NET 10 es LTS hasta noviembre de 2028; no se propone .NET 6 como solución histórica. Windows 7/8/8.1 y .NET Framework 4.6.1 carecen de soporte vigente. Windows 10 Home/Pro terminó soporte ordinario el 14 de octubre de 2025. ESU no convierte automáticamente cualquier edición en plataforma soportada por .NET 10. [Política .NET](https://dotnet.microsoft.com/en-us/platform/support/policy), [matriz Windows](https://learn.microsoft.com/en-us/dotnet/core/install/windows), [requisitos Framework](https://learn.microsoft.com/en-us/dotnet/framework/get-started/system-requirements), [ciclo Windows 10](https://learn.microsoft.com/en-us/lifecycle/products/windows-10-home-and-pro).

Entrada Win32 respeta UIPI; aplicaciones elevadas, UAC y escritorio seguro no están cubiertos. No se requiere ejecutar Adapta como administrador. Filtros pueden competir con otras ayudas; la app verifica y restaura sólo el efecto que controla. Guarda recuperación local para el siguiente inicio tras una terminación abrupta. [SendInput](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-sendinput), [Magnification API](https://learn.microsoft.com/en-us/previous-versions/windows/desktop/magapi/magapi-intro).

## Código y comprobaciones

```text
src/AdaptaPE/
  App.cs, MainWindow.cs      WPF, bandeja, panel y ciclo de vida
  Theme.cs, DwellWindow.cs   Estilos nativos y progreso cerca del cursor
  Core/                     Parser, sitios, activación, física y matrices
  Windows/                  SAPI, WinMM, UIA, Win32, filtros, apps, IA y preferencias
  Vision/                   Captura e inferencia local, verificación de modelos
assets/models/              Visión ONNX y modelo español local, hashes y procedencia
assets/brand/               Fondo, logo e icono originales empaquetados para Windows
assets/speech-runtime/      Vosk x64 y licencias de sus bibliotecas
scripts/                    Compilación y comprobaciones PowerShell
tests/AdaptaPE.Checks/       Lógica portable sin sensores
```

Interfaz WPF construida directamente en C#, con estilos XAML nativos y sin framework de UI adicional. Este repositorio contiene la edición **Base**. La edición Windows Lite se mantiene en [su repositorio independiente](https://github.com/TheAlejo160/Adapta-PE-Lite-Win).

```powershell
dotnet build src/AdaptaPE/AdaptaPE.csproj -f net10.0-windows -c Release
dotnet build src/AdaptaPE/AdaptaPE.csproj -f net48 -c Release
dotnet build src/AdaptaPE/AdaptaPE.csproj -f net461 -c Release
dotnet run --project tests/AdaptaPE.Checks/AdaptaPE.Checks.csproj -c Release
# Sólo Windows: comprueba que el ejecutable abre ajustes (deja la app abierta).
./scripts/check-startup.ps1
# Sólo Windows: activa Voz, exige 30 s de escucha y la apaga al terminar.
./scripts/check-voice.ps1
```

Desde macOS se compilan las fuentes gracias a `EnableWindowsTargeting` y paquetes de referencia. **No se ejecuta WPF en macOS.** Las comprobaciones portables cubren parser, dictado Unicode, alias, elecciones habladas, activación, contrato SAPI de audio continuo, lectura de resultados locales, subpíxeles, recuperación de dwell, transposición de matrices, calibración, pérdida, valores inválidos y un clic por entrada a 5/8/12/15/30/60 FPS. Añaden cierre por nombre, exclusión de procesos distintos, perfiles por voz, velocidad estable e independencia de FPS dentro del 1 % y frenado en la primera muestra segura. No activan sensores ni usan IA. El instalador autocontenido se genera desde `artifacts/net10.0-windows-win-x64-contained/`. Ver [AGENTS.md](AGENTS.md).

## Solución de problemas

- **Ya está abierto:** usa el icono de la bandeja junto al reloj. Cerrar ajustes conserva la app. Sal desde la bandeja antes de actualizar.
- **No escucha:** revisa señal del micrófono, dispositivo y permiso; prueba Local incluido. Una voz TTS instalada no acredita reconocimiento SAPI.
- **Orden detectada, acción fallida:** enfoca la aplicación externa y revisa la respuesta; Windows puede impedir entrada a apps elevadas. No se solicita elevar toda Adapta.
- **Cursor incómodo:** aplica Perfil equilibrado, recalibra con postura cómoda y ajusta velocidad/amplitud. «Modo preciso» reduce velocidad; «modo rápido» la aumenta.
- **Cámara negra/lenta:** revisa índice, permiso y otras apps que la ocupen. Baja detecciones/s para reducir inferencia; preview permanece independiente. El controlador y el equipo pueden limitar FPS reales.
- **Error de dependencia:** conserva todos los DLL/modelos del paquete. Usa el instalador para .NET/Visual C++; ediciones N/KN pueden necesitar componentes multimedia de Microsoft.
- **Instalador detecta versión más reciente:** usa la versión instalada. Una reparación admite la misma versión; un downgrade se bloquea.

## Memoria del código y documentación

El índice `adapta-pe-win` se regenera en modo completo y se guarda en `.codebase-memory/graph.db.zst`. Incluye símbolos C#, llamadas, referencias y scripts compatibles. [Mapa verificado](docs/MEMORIA-CODIGO.md) identifica los módulos y contratos; [validación Windows](docs/VALIDACION-WINDOWS.md) separa comprobaciones portables de pruebas físicas pendientes. El grafo es ayuda de navegación, no una prueba de ejecución. NSIS no dispone de símbolos estructurales en ese índice y se revisa directamente.

## Privacidad e IA opcional

Preferencias en `%LOCALAPPDATA%\AdaptaPE\settings.json`; clave en archivo cifrado con DPAPI ligado al usuario. Audio, imágenes, postura y transcripciones no se guardan. La cámara procesa localmente; no descarga modelos durante el uso. Datos visibles de la sesión permanecen en memoria. El reconocimiento local incluido usa Vosk, no descarga modelos ni envía audio; produce texto simple, normalmente sin capitalización o puntuación automática.

IA apagada por defecto: endpoint HTTPS compatible con `chat/completions`, modelo y clave propios. No incluye créditos. Las consultas se envían sólo mediante una orden explícita. Analizar pantalla exige consentimiento adicional durante esa sesión, captura el monitor del cursor y puede incluir datos privados. La lectura UIA local no envía capturas a IA. Órdenes desconocidas nunca se envían automáticamente.

Las búsquedas web comparten su consulta con el sitio como una navegación normal. No se ejecutan comandos shell dictados. La app facilita acceso; no sustituye todas las funciones de Narrador/NVDA ni garantiza detección universal con cualquier condición motora.

## Referencias y autoría

Se revisaron las extensiones Base/Lite (`sitios.js`, `VoiceAssistant`, `KineticEngine`, filtros y README) y la app macOS (`CommandParser`, `VoiceInputState`, `KineticState`, acceso AX, voz, filtros y documentación de paridad). La Lite macOS funcional reside en su propio repositorio y proyecto `Adapta-PE-Lite`; incluye fuentes locales de voz y filtros, sin depender del proyecto Base. El backend Flask biónico simulado no se importa.

© 2026 Rodrigo Alejandro Apcho Aliaga — TheAlejo160. Se conservan [LICENSE](LICENSE), [LICENSE.txt](LICENSE.txt) y [avisos de terceros](THIRD_PARTY_NOTICES.md). Modelos OpenCV Zoo fijados por revisión y SHA-256, con sus licencias independientes. Código y documentación preparados con asistencia de IA; la integración y validación Windows siguen bajo supervisión del autor.

Comprobaciones de esta entrega: 747 sin sensores. Compilación cruzada de `net10.0-windows`, `net48` y `net461`; Una consulta inicial de vulnerabilidades NuGet no estuvo disponible; la compilación final termina sin errores ni advertencias. Instalador local x64 generado; ejecución e instalación Windows pendientes.

## Cómo se organiza y ejecuta el código

| Archivo o módulo | Responsabilidad |
| --- | --- |
| [App.cs](src/AdaptaPE/App.cs) | Entrada WPF, instancia única y cierre. |
| [MainWindow.cs](src/AdaptaPE/MainWindow.cs) | Compone los motores, sincroniza Ajustes/PiP y ejecuta Detener todo. |
| [Core/Commands.cs](src/AdaptaPE/Core/Commands.cs) | Intenciones y frases finitas; normaliza comandos y conserva dictado literal. |
| [Core/RecognitionSession.cs](src/AdaptaPE/Core/RecognitionSession.cs) | Ordena cambios de voz y descarta inicios/callbacks antiguos. |
| [Windows/Voice.cs](src/AdaptaPE/Windows/Voice.cs) | Selecciona SAPI/Vosk y coordina escucha, TTS y recuperación. |
| [Windows/Actions.cs](src/AdaptaPE/Windows/Actions.cs) | Ejecuta intenciones contra una aplicación externa y comunica el resultado. |
| [Windows/DesktopAccess.cs](src/AdaptaPE/Windows/DesktopAccess.cs) | Consulta UI Automation, campos, selección y lectura manual con destino verificado. |
| `Core/` restante | Catálogo de sitios, alias, activación y matrices. |
| `Windows/` restante | Entrada Win32, aplicaciones, filtros, preferencias y capacidades del sistema. |
| [Vision/Camera.cs](src/AdaptaPE/Vision/Camera.cs) y [Vision/LocalVision.cs](src/AdaptaPE/Vision/LocalVision.cs) | Captura independiente, inferencia local por modo, preview y resultados recientes. |
| [Core/KineticState.cs](src/AdaptaPE/Core/KineticState.cs) | Calibración, velocidad temporal, zona segura y armado de dwell. |
| [scripts/installer.ps1](scripts/installer.ps1) y [scripts/installer.nsi](scripts/installer.nsi) | Publican un paquete autocontenido y generan instalación/desinstalación por usuario. |

Una orden pasa por **captura → reconocimiento → activación «Computadora» → parser → Actions → UIA/Win32 → respuesta**. `MainWindow.Run` muestra la intención detectada; sólo la respuesta posterior acredita la acción. El destino externo se conserva aunque se abra PiP. Las opciones ambiguas se cancelan si cambia la aplicación; las contraseñas se excluyen.

Cambiar micrófono/motor invalida la sesión anterior y espera su liberación. **Detener todo** invalida arranques pendientes, cancela acciones/elecciones, detiene escucha y TTS, retira el filtro propio y recuerda módulos apagados. También detiene cámara, movimiento, TalkBack e IA. **Salir** libera recursos conservando preferencias.

El [mapa de código](docs/MEMORIA-CODIGO.md) amplía los contratos y límites del índice. El grafo es una ayuda de navegación; las fuentes, manifiestos y NSIS son la referencia para verificar el comportamiento. `.codebase-memory/` es memoria local regenerable y se excluye de Git. Las pruebas, licencias y recursos necesarios sí se publican.

## Numeración unificada

Por indicación del autor, Base y Lite usan la versión pública **1.0** en el producto, interfaz, panel, instalador, Configuración de Windows y nombre del archivo. La interfaz lee la versión informativa del ensamblado; no mantiene etiquetas duplicadas independientes.

La revisión técnica de archivo **1.0.3.0** ordena las actualizaciones sin cambiar la versión pública. El instalador guarda `DisplayVersion=1.0` y `BuildVersion=1.0.3.0`; compara la revisión para actualizar/reparar y bloquear paquetes anteriores. Acepta las instalaciones previas que sólo registraron `DisplayVersion` o tienen un EXE en la ubicación propia. Se conserva la protección frente a una revisión superior, las preferencias y los accesos elegidos.

Compilación y empaquetado desde macOS; la migración del registro y ejecución del instalador requieren Windows real.

Verificación de la numeración unificada: 750 comprobaciones aprobadas sobre el paquete, incluida versión de producto 1.0 y revisión de archivo 1.0.3.0. Compilación cruzada sin errores ni advertencias en los tres targets; fuentes NSIS y migración de claves revisadas directamente. Instalación/actualización, WPF y reconocimiento con micrófono permanecen pendientes en Windows.

## Verificación de la documentación

El 3 de octubre de 2026 se revisaron README, recursos y exclusiones de Git. Pasaron 747 comprobaciones portables y 750 al inspeccionar el paquete local. No se ejecutaron WPF, sensores ni instalador en Windows durante esta revisión documental.

## Proyectos de la familia Adapta PE

| Programa | Código y alcance |
| --- | --- |
| Extensión Base | [Adapta-PE](https://github.com/TheAlejo160/Adapta-PE): voz, filtros, cursor de página y TalkBack. |
| Extensión Lite | [Adapta-PE-Lite](https://github.com/TheAlejo160/Adapta-PE-Lite): voz y filtros. |
| Windows Base | [Adapta-PE-Win](https://github.com/TheAlejo160/Adapta-PE-Win): aplicación WPF independiente. |
| Windows Lite | [Adapta-PE-Lite-Win](https://github.com/TheAlejo160/Adapta-PE-Lite-Win): voz, lectura manual y filtros. |
| macOS Base y Lite funcional | [Adapta-PE-MacOS-Intel-Apple-Silicon-](https://github.com/TheAlejo160/Adapta-PE-MacOS-Intel-Apple-Silicon-): dos proyectos Xcode que comparten motores. |
| macOS Lite independiente | [Adapta-PE-Lite-MacOS](https://github.com/TheAlejo160/Adapta-PE-Lite-MacOS): voz y siete filtros en una aplicación autónoma. |

Los números de versión y permisos pertenecen a cada plataforma. Un ZIP de la extensión no instala la aplicación de escritorio. Los repositorios publican fuentes y recursos necesarios; los instaladores generados y la configuración personal quedan fuera de Git.

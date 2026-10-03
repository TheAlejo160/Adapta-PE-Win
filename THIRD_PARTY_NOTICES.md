# Componentes y atribuciones

Adapta PE Windows adapta comportamientos y datos de Adapta PE y Adapta PE Lite, © 2026 Rodrigo Alejandro Apcho Aliaga — TheAlejo160. Se conservan [LICENSE](LICENSE) y [LICENSE.txt](LICENSE.txt) de la extensión base. No se cambia la licencia de componentes de terceros.

- **.NET, WPF y System.Speech**: Microsoft y colaboradores. Código abierto bajo MIT; .NET Framework se distribuye con las condiciones de Microsoft, no se incluye su instalador aquí.
- **OpenCvSharp4**: shimat y colaboradores, Apache-2.0. Paquetes 4.10.0.20241108 en `net10.0-windows`/`net48`; 4.5.3.20210817 con target explícito `net461` en la compilación histórica. Cada wrapper usa su runtime Windows de la misma versión.
- **OpenCV**: OpenCV y colaboradores, Apache-2.0 para las versiones empleadas. El runtime nativo puede incluir componentes adicionales con sus propios avisos, conservados por NuGet.
- **OpenCV Zoo**: modelos y contratos de pre/postprocesado de YuNet, MP-PersonDet, MP-Pose, MP-PalmDet y MP-HandPose. Revisión fijada `47534e27c9851bb1128ccc0102f1145e27f23f98`. Los archivos de licencia de cada modelo están en `assets/models/`. `manifest.json` registra tamaño, procedencia y SHA-256; la aplicación verifica cada modelo al cargarlo.

Referencias: [OpenCvSharp](https://github.com/shimat/opencvsharp), [OpenCV Zoo](https://github.com/opencv/opencv_zoo), [System.Speech](https://www.nuget.org/packages/System.Speech).

No se incluye un backend Python ni se ejecutan los scripts de referencia de OpenCV. Inferencia, captura y postprocesado se realizan en C# y el runtime OpenCV local. No hay descargas de modelos durante el uso de la aplicación.

- **Marca visual**: fondo y logo originales del repositorio macOS del autor. El ICO empaqueta los PNG originales sin redibujar ni modificar sus imágenes.
- **Vosk 0.3.45**: Alpha Cephei, Apache-2.0, runtime Windows x64 de su [release oficial](https://github.com/alphacep/vosk-api/releases/tag/v0.3.45). Integración directa con API C, sin añadir un runtime de Python ni paquetes .NET Standard al target histórico.
- **Modelo español vosk-model-small-es-0.42**: AC Technologies / Alpha Cephei, Apache-2.0 según [catálogo oficial](https://alphacephei.com/vosk/models). Procedencia, SHA-256 de archivos y archivos ZIP en `assets/models/speech/manifest.json`; el SHA registra integridad de la descarga, no una firma del editor.
- **Dependencias del runtime de voz**: Kaldi (Apache-2.0), OpenBLAS (BSD), GCC libstdc++/libgcc (GPL-3.0 con GCC Runtime Library Exception) y mingw-winpthreads (licencias permisivas incluidas). Avisos íntegros en `assets/speech-runtime/licenses/`, copiados al paquete.
- **NSIS 3.13**: instalador Windows nativo, licencia zlib/libpng. [Proyecto y licencia](https://nsis.sourceforge.io/License); avisos de código de instalador en `licenses/installer/`. No añade un servicio ni un runtime a la app.
- **Microsoft Visual C++ Redistributable x64**: instalador oficial Microsoft conservado íntegro, con su licencia/interfaz propia. Procedencia y SHA-256 registrados en `assets/prerequisites/manifest.json`. Se incluye sólo en el instalador moderno y se ejecuta si falta el componente compatible; no se elimina al desinstalar Adapta PE.
- Los paquetes autocontenidos añaden .NET/Windows Desktop Runtime 10; licencias de esos paquetes en `licenses/Microsoft.NETCore.App/` y `licenses/Microsoft.WindowsDesktop.App/`.

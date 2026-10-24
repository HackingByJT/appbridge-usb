# AppBridge USB

**Abre una app Android instalada en una ventana de Windows mediante USB.**

[English](README.md) · Español

**0.1.0-beta** · [Descargar instalador o ZIP portable](https://github.com/HackingByJT/appbridge-usb/releases) · [Repositorio](https://github.com/HackingByJT/appbridge-usb) · Apache-2.0

![Ilustración conceptual de un teléfono Android físico conectado por USB a una sesión en Windows](https://raw.githubusercontent.com/HackingByJT/appbridge-usb/main/assets/social-preview.svg)

*Ilustración conceptual original. Un teléfono Android conectado ejecuta la app; scrcpy proporciona el motor de imagen, control y audio.*

AppBridge USB es un lanzador para Windows que utiliza un dispositivo Android físico. Elige un teléfono conectado y un paquete instalado para abrir esa app en una pantalla virtual independiente de Android. El teléfono ejecuta la app; Windows muestra y controla su ventana. El teléfono debe permanecer conectado y autorizado durante la sesión.

El motor de imagen, control y audio es **[scrcpy, de Genymobile, Romain Vimont y sus colaboradores](https://github.com/Genymobile/scrcpy)**. AppBridge USB aporta su propia interfaz de preparación, selección de dispositivo y paquete, y controles de sesión. Es un proyecto independiente y no implica respaldo de scrcpy ni de los proveedores de apps Android.

## Funciones de la beta

- Seleccionar un dispositivo USB autorizado y una app que ya esté instalada.
- Crear una pantalla virtual con el tamaño y la densidad informados por el teléfono, comenzando en orientación vertical.
- Iniciar en pantalla completa, apagar opcionalmente el panel físico del teléfono y enviar el audio del dispositivo.
- Pulsar **F12** para entrar en pantalla completa y **Esc** para salir mientras la ventana de la sesión está activa.
- Reiniciar opcionalmente la app Android para aplicar su escala de pantalla.
- Preparar los componentes oficiales de scrcpy cuando se solicite y comprobar su SHA-256.

La beta necesita hardware Android; no emula Android ni convierte una app Android en un programa autónomo de Windows. No incluye APK, cuentas de apps, canales ni streams, y no modifica la autenticación, suscripciones o protección de contenido de las apps.

## Requisitos

| Componente | Requisito de la beta |
| --- | --- |
| Computadora | Windows 11, x64 |
| Entorno | .NET Framework 4.8 o una versión 4.x posterior compatible |
| Android | Dispositivo físico con Android 12 / API 31 o posterior |
| Conexión | Cable USB de datos; depuración USB habilitada y computadora autorizada |
| App Android | Ya instalada, con el acceso proporcionado por su propio proveedor |
| Internet | Necesario para preparar scrcpy; cada app Android puede necesitarlo también |

Algunos fabricantes requieren un controlador USB para ADB o un permiso adicional para controlar el dispositivo con teclado y ratón. Consulta las [instrucciones de Android para dispositivos físicos](https://developer.android.com/studio/run/device) y las de tu fabricante. No necesitas Android Studio para usar AppBridge USB.

Android 12 es el mínimo previsto para esta beta; no garantiza que todos los dispositivos y apps funcionen. Las pantallas virtuales, el control, la orientación, la captura de audio y el video protegido dependen de la app, la versión de Android y el fabricante.

## Primer uso

Descarga el instalador o el ZIP portable para Windows x64 desde [GitHub Releases](https://github.com/HackingByJT/appbridge-usb/releases), o compila desde el código:

1. Instala el paquete de instalación o extrae todo el ZIP portable en una carpeta con permiso de escritura. Mantén `AppBridge.exe` y `AppBridgeControls.exe` juntos.
2. Abre `AppBridge.exe` y selecciona **Prepare components**. Este botón solicita la descarga oficial de scrcpy. Espera a que termine la verificación y extracción.
3. Habilita la depuración USB en el teléfono, conecta un cable de datos, desbloquéalo y acepta la autorización para esta computadora.
4. Selecciona **Refresh devices**, elige el teléfono y pulsa **List apps**. Selecciona un paquete Android instalado o escribe su identificador.
5. Revisa **Fullscreen**, **Turn phone screen off** y **Forward device audio**, activadas inicialmente. **Restart app to apply scale** es opcional y comienza desactivada; fuerza la detención de la app seleccionada antes de abrirla y puede interrumpir trabajo sin guardar.
6. Selecciona **Start USB session**. Cierra la ventana de la sesión para terminarla. Cerrar el lanzador deja en marcha una sesión que ya esté abierta.

F12 y Esc actúan sobre la ventana activa de la sesión. Esc sale de pantalla completa; no cierra la app ni termina la sesión. La interfaz de esta beta está en inglés.

### Panel físico, bloqueo y audio

**Apagar el panel del teléfono es diferente de bloquearlo con un PIN.** Desbloquea el teléfono normalmente cuando Android solicite autenticación. Bloquear el dispositivo puede interrumpir la app o la sesión; AppBridge USB no evita la pantalla de bloqueo. El botón físico de encendido puede volver a encender el panel. Consulta la [documentación de scrcpy sobre el panel](https://github.com/Genymobile/scrcpy/blob/v5.0.1/doc/device.md#turn-screen-off).

**Forward device audio captura la salida de audio del sistema del teléfono** y puede incluir otras apps y notificaciones, además de la app elegida. Revisa qué se está reproduciendo antes de comenzar. Android 12 permite esta forma de captura, pero las apps, el contenido protegido y el fabricante pueden limitar el resultado. Consulta la [documentación de audio de scrcpy](https://github.com/Genymobile/scrcpy/blob/v5.0.1/doc/audio.md).

### Perfil opcional de XuperTV

Si el paquete `com.msandroid.mobile` ya está instalado, **List apps** habilita **Use XuperTV** como atajo para seleccionarlo. Es opcional. AppBridge USB no proporciona el APK, una cuenta, contenido ni conexión con su proveedor. La disponibilidad y el uso permitido dependen de esa app y su proveedor.

## Componentes y datos locales

El instalador y el ZIP de AppBridge USB contienen ocho archivos: `AppBridge.exe`, `AppBridgeControls.exe`, sus dos archivos `.config`, `README.md`, `README.es.md`, `LICENSE` y `THIRD_PARTY_NOTICES.md`. El instalador utiliza [NSIS](https://nsis.sourceforge.io/) y crea su propio desinstalador. Las herramientas Android se obtienen por separado al seleccionar **Prepare components**:

| Elemento | Fuente fijada |
| --- | --- |
| scrcpy | [Release oficial de Genymobile v5.0.1](https://github.com/Genymobile/scrcpy/releases/tag/v5.0.1) |
| Archivo Windows | `scrcpy-win64-v5.0.1.zip` |
| SHA-256 | `b12a2c4ee8be317422451fc7dcf8ee20a71b5ea7ef9ad73ddd825a25316227a5` |
| Referencia del hash | [Instrucciones oficiales de Windows para v5.0.1](https://github.com/Genymobile/scrcpy/blob/v5.0.1/doc/windows.md) |
| Carpeta de componentes | `%LOCALAPPDATA%\AppBridgeUSB\runtime` |

La descarga se hace directamente desde la release oficial; AppBridge USB no aloja una recompilación de scrcpy. Una discrepancia del hash detiene la preparación. La carpeta local contiene scrcpy, ADB y sus bibliotecas, con licencias independientes; consulta [las atribuciones](THIRD_PARTY_NOTICES.md).

La búsqueda de dispositivos y paquetes utiliza comandos ADB locales. La beta no ofrece grabación ni sincronización automática del portapapeles. La depuración USB da a la computadora autorizada acceso considerable al teléfono: autoriza equipos de confianza y revoca la autorización desde el teléfono cuando deje de ser necesaria.

## Instalación y eliminación

El instalador por usuario utiliza `%LOCALAPPDATA%\Programs\AppBridgeUSB`, crea un acceso en el menú Inicio y ofrece uno opcional en el escritorio. Se elimina desde Aplicaciones instaladas de Windows o su desinstalador. La edición portable se ejecuta desde la carpeta extraída.

La beta no está firmada; no se afirma que tenga firma de código o certificación.

La desinstalación conserva los componentes descargados por separado. Para eliminarlos también, cierra AppBridge USB y sus ventanas de sesión y borra `%LOCALAPPDATA%\AppBridgeUSB\runtime`. AppBridge USB no desinstala ni borra las apps Android del teléfono.

## Solución de problemas

| Síntoma | Próximo paso |
| --- | --- |
| Faltan componentes | Selecciona **Prepare components** y revisa la conexión. Una discrepancia del hash debe resolverse antes de usar el archivo. |
| No aparece el teléfono USB | Desbloquéalo, confirma la autorización USB, prueba otro cable de datos o puerto y revisa el controlador ADB del fabricante. |
| Dispositivo no autorizado o desconectado | Reconéctalo y acepta la autorización en el teléfono; después actualiza la lista. |
| No aparece una app | Confirma que esté instalada para el usuario Android activo y vuelve a pulsar **List apps**. AppBridge USB no instala APK. |
| Ventana vacía, bloqueada o incompleta | La app o el dispositivo puede restringir pantallas virtuales o contenido protegido. Describe esta limitación en un informe de compatibilidad. |
| Escala incorrecta al abrir | Termina la sesión y prueba **Restart app to apply scale**. Esto reinicia la app elegida. |
| No hay audio | Revisa el volumen del teléfono/app y la opción de audio. La captura puede estar restringida; consulta la documentación oficial. |
| El teléfono se bloquea durante el uso | Desbloquéalo en el teléfono. Apagar el panel no desactiva la autenticación de Android. |

Para enviar comentarios, incluye la versión de AppBridge USB y Windows, modelo del teléfono, versión de Android, paquete de la app, opciones elegidas y una descripción breve. Elimina números de serie, datos de cuentas y contenido privado. El código fuente incluye las guías de contribución y reporte de seguridad en `CONTRIBUTING.md` y `SECURITY.md`.

## Compilar desde el código

### Estado de validación de 0.1.0-beta

| Comprobación | Resultado observado |
| --- | --- |
| Compilación local de ambos ejecutables, instalador y ZIP portable | Correcta |
| Interfaz del lanzador sin teléfono conectado | Abierta y observada |
| Sesión Android mediante la nueva interfaz del lanzador | Todavía no verificada |
| Instalación y desinstalación del paquete | Todavía no observadas |
| Primera compilación en GitHub Actions | [Correcta](https://github.com/HackingByJT/appbridge-usb/actions/runs/40073374903) |

Estas observaciones no establecen compatibilidad con una app o dispositivo Android concretos. Consulta las [comprobaciones detalladas de la release](https://github.com/HackingByJT/appbridge-usb/blob/main/docs/RELEASE-CHECKS.md) para conocer el alcance observado y los puntos pendientes.

### Comandos de compilación

Usa Windows PowerShell con el compilador de .NET Framework de Windows disponible. El código utiliza .NET Framework 4.8, x64 y C# 5. Desde la raíz del repositorio:

```powershell
.\scripts\build.ps1
```

Los ejecutables se generan en `build/app/AppBridge.exe` y `build/app/AppBridgeControls.exe`. No hace falta descargar scrcpy para compilar el lanzador.

Para crear los paquetes, obtén la herramienta NSIS desde [su proyecto oficial](https://nsis.sourceforge.io/) e indica la ruta a su compilador:

```powershell
.\scripts\package.ps1 -MakeNsisPath 'C:\Tools\NSIS\makensis.exe'
```

Los archivos en `dist/` son `AppBridgeUSB-0.1.0-beta-win64-setup.exe` y `AppBridgeUSB-0.1.0-beta-win64-portable.zip`, además de un manifiesto del contenido y `SHA256SUMS.txt`. Crear un paquete no demuestra por sí mismo que su instalación, desinstalación o compatibilidad con un dispositivo estén verificadas. El código fuente también incluye `CHANGELOG.md`, `installer/TOOLCHAIN.md` y el plan de releases y comunidad en `docs/LAUNCH.md`.

## Licencia y créditos

El código, la documentación y el arte originales del proyecto se distribuyen bajo [Apache License 2.0](LICENSE), copyright 2026 HackingByJT y colaboradores. Los componentes descargados por separado conservan sus propias licencias.

AppBridge USB depende del trabajo de [scrcpy](https://github.com/Genymobile/scrcpy), [Android Open Source Project / ADB](https://android.googlesource.com/platform/packages/modules/adb/), [FFmpeg](https://ffmpeg.org/), [SDL](https://www.libsdl.org/) y [libusb](https://libusb.info/). Visita y da crédito a esos proyectos. Sus licencias y la función de NSIS se describen en [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).

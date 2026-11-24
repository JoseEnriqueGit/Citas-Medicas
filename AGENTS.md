# Citas Médicas (MAUI, Android)

Retomar: leer `odd/tasks/citas-medicas.md` (specs, tareas, log) y `design/system.md`. Solo queda implementar.

## Estructura

- `CitasMedicas.Core` (net10.0): modelos, SQLite, servicios y reglas. Sin dependencias de MAUI, para poder probarlo con xunit.
- `CitasMedicas.Core.Tests` (xunit): pruebas del Core. Primero la prueba en rojo, luego el código.
- `CitasMedicas.App` (MAUI net10.0-android, `com.citasmedicas.app`): vistas, ViewModels, Shell. Todavía tiene la plantilla (`MainPage`, `AppShell`): reemplazarla en T4.
- `design/citas-medicas/*.html`: artboards de las 5 pantallas con sus estados. Cada nota bajo el teléfono dice cómo implementarlo.

## Comandos

```bash
export JAVA_HOME="C:/Users/ogand/.jdks/jbr-21.0.7"; export PATH="$JAVA_HOME/bin:$PATH"
dotnet build CitasMedicas.slnx
dotnet test CitasMedicas.Core.Tests
dotnet build CitasMedicas.App -t:Run -f net10.0-android
```

- Compilar siempre con el JDK 21 (con el 24 falla con `XA0030`).
- Emulador: `emulator -avd Medium_Phone_API_36.0`. Con Git Bash exportar `MSYS_NO_PATHCONV=1` antes de usar `adb`.
- Capturas: `adb exec-out screencap -p`. Coordenadas con `uiautomator dump`, texto con `input text` (`%s` por espacio).
- Si un diálogo ANR de `com.clinicadm.app` tapa la pantalla: `adb shell am force-stop com.clinicadm.app`.
- Comprobar que corre de verdad: `adb shell dumpsys activity activities | grep com.citasmedicas`.

## Reglas

- **Sin comentarios en el código** (ni `///` ni `//`), como en las demás entregas.
- Texto de la interfaz en español neutro, con tildes y ñ. Identificadores y mensajes de commit en inglés.
- Colores, tipografía y estados salen de `design/system.md`; no inventar valores. Estado nunca solo por color.
- Objetivos táctiles de 44 px como mínimo; etiquetas de campo siempre visibles; todo control con nombre accesible (`SemanticProperties`).
- Implementar cada pantalla con `gentle_design` acción `implement` sobre su artboard, y reportar con `handoff`.
- Las reglas de negocio viven en el Core y se prueban allí; la UI solo las llama.
- `dotnet new sln` en .NET 10 crea `.slnx`.
- `DisplayAlert` no sirve para los diálogos de confirmación con resumen: usar una página modal propia.
- Gotcha de SQLite: usar siempre ruta absoluta en `FileSystem.AppDataDirectory`; no depender de la caché (Android la purga con poco espacio).

## Cierre de cada tarea

Liberar lo que el trabajo abrió: emulador y procesos propios, trabajos en segundo plano, copias temporales. Conservar capturas y evidencias. Dejar el árbol git limpio o explicar qué queda.

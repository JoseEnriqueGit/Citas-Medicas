# Citas Médicas

Aplicación móvil Android (.NET MAUI) para administrar citas entre pacientes y centros médicos. Los datos se guardan localmente con SQLite.

## Funcionalidades

- Registro de pacientes
- Listado de médicos
- Solicitud de citas
- Consulta de fechas y horarios
- Historial de citas

## Estado

Andamiaje y diseño de la interfaz terminados. La implementación sigue el plan de `odd/tasks/citas-medicas.md`.

## Contenido

| Ruta | Qué es |
| --- | --- |
| `CitasMedicas.Core` | Modelos, SQLite, servicios y reglas de negocio |
| `CitasMedicas.Core.Tests` | Pruebas xunit del Core |
| `CitasMedicas.App` | App MAUI (Android) |
| `design/citas-medicas` | Diseño de las 5 pantallas y sus estados |
| `design/system.md` | Sistema de diseño (colores, tipografía, componentes) |
| `odd/tasks/citas-medicas.md` | Especificación, tareas y registro |

## Requisitos

.NET 10 SDK con el workload `maui-android`, JDK 21 y el Android SDK con el emulador `Medium_Phone_API_36.0`.

## Ejecutar

```bash
export JAVA_HOME="C:/Users/ogand/.jdks/jbr-21.0.7"; export PATH="$JAVA_HOME/bin:$PATH"
dotnet test CitasMedicas.Core.Tests
dotnet build CitasMedicas.App -t:Run -f net10.0-android
```

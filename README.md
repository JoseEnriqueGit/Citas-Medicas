# Citas Médicas

Aplicación móvil Android (.NET MAUI) para administrar citas entre pacientes y centros médicos. Los datos se guardan localmente con SQLite.

## Funcionalidades

- **Registro de pacientes**: nombre, cédula única (`000-0000000-0`), teléfono de 10 dígitos, correo opcional y fecha de nacimiento no futura. Todos los errores se muestran a la vez; una cédula repetida no guarda nada. El paciente activo puede editar sus datos.
- **Listado de médicos**: 9 médicos de ejemplo con especialidad y centro, búsqueda por nombre o especialidad combinable con el filtro por especialidad, y el próximo horario libre de cada uno.
- **Consulta de fechas y horarios**: bloques de 30 minutos de 08:00 a 16:00, de lunes a viernes. Los ocupados salen de las citas programadas; un día lleno ofrece ir al próximo día con hueco.
- **Solicitud de citas**: médico, fecha, horario y motivo (máximo 200 caracteres). La disponibilidad se comprueba de nuevo al confirmar: si el horario ya fue tomado no se guarda nada y se conservan los datos escritos.
- **Historial de citas**: la más reciente primero, con filtro por estado (Programada, Completada, Cancelada). Solo las programadas se cancelan, con confirmación, y el horario queda libre.

## Estructura

| Ruta | Qué es |
| --- | --- |
| `CitasMedicas.Core` | Modelos, SQLite (`sqlite-net-pcl`), servicios y reglas de negocio. Sin dependencias de MAUI |
| `CitasMedicas.Core.Tests` | Pruebas xunit del Core (72) |
| `CitasMedicas.App` | App MAUI (solo Android): Shell con 5 pestañas, vistas, ViewModels y controles |
| `design/citas-medicas` | Diseño de las 5 pantallas y sus estados |
| `design/system.md` | Sistema de diseño (colores, tipografía, componentes) |
| `odd/tasks/citas-medicas.md` | Especificación, tareas y registro |
| `evidence` | Capturas del emulador de cada pantalla y estado verificado |

## Reglas de negocio

- Una cita no se reserva en el pasado ni se duplica para el mismo médico, fecha y hora mientras esté programada. La base de datos lo impone con un índice único parcial, así que la regla se cumple aunque dos solicitudes lleguen a la vez.
- Los bloques ocupados no se guardan: se calculan a partir de las citas programadas. Cancelar una cita libera su horario.
- Las reglas viven en el Core y se prueban allí; la interfaz solo las llama.

## Requisitos

.NET 10 SDK con el workload `maui-android`, JDK 21 y el Android SDK con el emulador `Medium_Phone_API_36.0`.

## Ejecutar

```bash
export JAVA_HOME="C:/Users/ogand/.jdks/jbr-21.0.7"; export PATH="$JAVA_HOME/bin:$PATH"
dotnet test CitasMedicas.Core.Tests
dotnet build CitasMedicas.App -t:Run -f net10.0-android
```

Con el emulador abierto, la app se instala y se lanza sola. Si no queda en primer plano: `adb shell am start -n com.citasmedicas.app/crc6452d1be0e8d06e841.MainActivity`.

Si el archivo de datos se daña, la app muestra una pantalla con «Reintentar» y «Empezar de cero»; esta última aparta el archivo como `citas.db3.corrupta-<fecha>` y crea una base nueva.

Los builds de depuración usan Fast Deployment: no borres los datos con `adb shell pm clear` (el arranque aborta). Para empezar de cero usa `adb uninstall com.citasmedicas.app` y vuelve a ejecutar.

## Verificación

- `dotnet test CitasMedicas.Core.Tests`: 72 pruebas en verde (repositorio, horarios, pacientes y citas).
- Flujo completo recorrido en el emulador por `adb`, con capturas en `evidence/`: registro con errores y éxito, médicos con filtro y sin resultados, horarios con día lleno, solicitud con diálogo de confirmación, estado «Horario tomado» (con una cita insertada por fuera en la base), cita programada, historial con cancelación y estados vacíos.

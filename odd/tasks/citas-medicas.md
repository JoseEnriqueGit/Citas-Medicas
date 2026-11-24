# Sistema de Gestión de Citas Médicas

App móvil Android (.NET MAUI, SQLite local) para administrar citas entre pacientes y centros médicos.
Estado: implementación completa (T1 a T9), verificada en emulador. Diseño en `design/citas-medicas/` (5 artboards) y `design/system.md`.

## Specs

- S1. "Registro de pacientes." Campos: nombre completo, cédula (única, formato 000-0000000-0), teléfono (10 dígitos, 809-555-0142), correo (opcional, válido si se escribe) y fecha de nacimiento (no futura). Al registrar queda como paciente activo. Todos los errores se muestran a la vez. Cédula repetida: no se guarda nada. Diseño: `01-registro.html`.
- S2. "Listado de médicos." 9 médicos de ejemplo en el primer arranque, con especialidad y centro. Búsqueda por nombre o especialidad y filtro por especialidad, combinables. Cada fila muestra el próximo horario libre. Sin resultados: se cita la consulta y se ofrece "Limpiar búsqueda". Ver médicos no exige registro. Diseño: `02-medicos.html`.
- S3. "Consulta de fechas y horarios." Por médico y fecha: bloques de 30 minutos de 08:00 a 16:00 (16 bloques), lunes a viernes, sin fines de semana ni fechas pasadas. Los ocupados salen de las citas Programadas; no se guardan. Un día lleno ofrece ir al próximo día con hueco. Diseño: `03-horarios.html`.
- S4. "Solicitud de citas." Médico, fecha, horario libre y motivo (obligatorio, máximo 200). Requiere paciente activo. La disponibilidad se vuelve a comprobar al confirmar: si el horario ya fue tomado, no se guarda, se avisa y se conservan los datos escritos. Diseño: `04-solicitar-cita.html`.
- S5. "Historial de citas." Citas del paciente activo, la más reciente primero, con filtro Todas, Programadas, Completadas y Canceladas. Solo las Programadas se pueden cancelar, con confirmación; al cancelar el horario queda libre. Vacío: "Aún no tienes citas" con acción "Solicitar cita". Diseño: `05-historial.html`.
- S6. Estados de cita: Programada, Completada, Cancelada. Una cita no se reserva en el pasado ni se duplica en el mismo médico, fecha y hora mientras esté Programada.
- S7. Reglas de código del curso: proyecto independiente, sin comentarios en el código, copy de la UI en español neutro, solo Android.

## Tasks

| ID | Specs | Ruta | Descripción | Commit |
| --- | --- | --- | --- | --- |
| T1 | S1 S2 S6 | inline, TDD | Core: modelos `Paciente`, `Medico`, `Cita`, repositorio SQLite (sqlite-net-pcl), seed de 9 médicos | ada4bca |
| T2 | S3 | inline, TDD | Core: `HorarioService` (bloques, días hábiles, ocupados, próximo libre) | 087ceb6 |
| T3 | S4 S6 | inline, TDD | Core: `CitaService` (solicitar, doble reserva, pasado, cancelar, historial) y `PacienteService` (validación, cédula única) | 01c5ff5 |
| T4 | S7 | inline | App: `Colors.xaml` y `Styles.xaml` desde `design/system.md`, Shell con 5 pestañas, inyección de dependencias, quitar la plantilla | 162a1fe |
| T5 | S1 | inline | Pantalla Registro (3 estados) | 443daec |
| T6 | S2 S3 | inline | Pantallas Médicos y Horarios (6 estados) | 12f3b9d |
| T7 | S4 | inline | Pantalla Solicitar cita (4 estados, diálogo de confirmación) | 9311152 |
| T8 | S5 | inline | Pantalla Historial (3 estados, diálogo de cancelación) | 43af740 |
| T9 | S1-S7 | inline | Verificación en emulador: flujo completo por `adb`, capturas, README final | 502c80c |

Orden: T1 a T3 (Core con pruebas en rojo primero), T4, T5 a T8 una por una contra su artboard (`gentle_design` acción `implement`), T9. Un commit por tarea o por par de tareas pequeñas, Conventional Commits.

## Log

- L1 (usuario, solicitud original): "Sistema de Gestión de Citas Médicas. Aplicación móvil para facilitar la administración de citas entre pacientes y centros médicos. Funcionalidades principales: Registro de pacientes. Listado de médicos. Solicitud de citas. Consulta de fechas y horarios. Historial de citas." Pedido: "una app funcional".
- L2 (usuario): "Ok, crea un directorio nuevo para ese proyecto, prepara el entorno, el diseno de la UI de todas las pantallas planeadas con gentle desing, para que la nueva sesion solo procede a implemetar."
- L3 (decisión por defecto): se propuso SQLite local y sin PDF; el usuario respondió "Ok" sin elegir otra cosa. Si la consigna exige consumir un API REST o entrega en PDF, se agrega después (ver `Xamarin-Unidad2-Actividad1` como referencia para ambos).
- L4 (diseño): `design/citas-medicas/` con 5 artboards y `design/system.md` (sistema propuesto, el proyecto no tenía fuentes de diseño). Navegación: 5 pestañas inferiores (Paciente, Médicos, Horarios, Solicitar, Historial).
- L5 (entorno): `dotnet build CitasMedicas.slnx` con JDK 21 compila sin errores. Se fijó `SQLitePCLRaw.lib.e_sqlite3` y `.android` en 2.1.14 para evitar la advertencia NU1903 (vulnerabilidad alta de 2.1.11).
- L6 (implementación): T1 a T3 con pruebas primero (72 pruebas xunit en verde); T4 a T8 con un worker por pantalla contra su artboard (`gentle_design` implement y handoff). La regla de no duplicar se impone también en SQLite con un índice único parcial (`WHERE Estado = 0`).
- L7 (hallazgos de entorno): en Android la librería nativa `libe_sqlite3.so` de `SQLitePCLRaw.lib.e_sqlite3` (linux, necesita glibc) tapaba a la de Android y tumbaba el arranque; se excluyen sus assets nativos en `CitasMedicas.App.csproj` y se llama `SQLitePCL.Batteries_V2.Init()` en el Core. `adb shell pm clear` rompe el Fast Deployment de depuración: usar `adb uninstall` y volver a ejecutar. El permiso INTERNET debe quedar en el manifiesto (sin él la app de depuración aborta).
- L8 (verificación T9): estado «Horario tomado» reproducido en el emulador insertando por fuera una cita en la base (`run-as` + copia de `citas.db3`): bloque tachado, aviso, motivo conservado y nada guardado. Estados vacíos y «sin paciente» capturados en `evidence/t9-*`. Solo Android: se quitaron las carpetas de plataforma iOS, MacCatalyst y Windows.
- L9 (robustez, petición del usuario: criticar el ciclo de vida y probar fuera del happy path): 47 pruebas nuevas (119 en total) en RED y luego GREEN. Defectos del Core corregidos: horario con ticks sub-milisegundo esquivaba la regla de no duplicar; paciente inactivo podía solicitar cita (S4); cédula con dígitos Unicode; teléfono con letras si había 10 dígitos; correo con puntos vacíos; sin tope de longitud de nombre/correo; nacimiento anterior a 1900; Actualizar con id inexistente devolvía éxito y reactivaba pacientes inactivos; Historial lanzaba KeyNotFound con médico inexistente y su orden no era determinista; Bloques de un médico inexistente salía libre.
- L10 (pendientes de UI, sin corregir): `async void` en HistorialViewModel.Cancelar; modales sin liberar `_ocupado` si se sacan del stack por otra vía; Registrar y Revisar sin guarda contra doble toque; fallo de arranque con BD corrupta sin mensaje; fechas obsoletas tras medianoche al volver del fondo; borrador y selección en memoria que se pierden al morir el proceso; Médicos calcula el próximo horario en el hilo de UI.
- L11 (robustez de UI, pedida tras L10): `ErroresUi` captura excepciones de comandos (sin `async void`; alerta «No se pudo completar la acción»); los modales resuelven su resultado en `OnDisappearing`; `RevisarAsync` y `Registrar` ignoran reentradas; arranque con BD ilegible muestra `ErrorArranquePage` (Reintentar / Empezar de cero, que aparta el archivo como `.corrupta-<fecha>`). Verificado en emulador (`evidence/t10-*`): flujo completo, Atrás en ambos modales, doble toque simultáneo (una sola cita), cancelación, BD corrupta y reintento. Hallazgo de la verificación: el mensaje de «Reintentar» se escribía en una página reemplazada; corregido reutilizando la misma página.
- L12 (sin cubrir): modal abierto y navegación de Shell por otra vía no se probó; fechas obsoletas tras medianoche, borrador en memoria y cálculo del próximo horario en el hilo de UI siguen pendientes; decisión abierta sobre «Completada».

- L13 (entrega académica): `docs/entrega` con Conclusiones, Bibliografía y Anexos (Word y PDF) y guion de exposición; `docs/capturas` con 18 capturas; README reescrito para mostrar el repositorio. Las referencias se verificaron (URLs con respuesta 200) y los anexos A y B documentan honestamente lo no implementado (RF-02, RNF-02, RNF-03, reprogramación).

- L14 (capturas de uso real): `docs/capturas-usuario` con 18 capturas a 1080x2400 de una paciente ficticia (registro, médicos, horarios, solicitud, historial y cancelación), con barra de estado limpia. Para que se leyera la barra de estado se corrigió `MainActivity` (íconos oscuros sobre fondo claro, `AppearanceLightStatusBars`).

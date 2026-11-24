# Sistema de diseño · Citas Médicas

Sistema inicial **propuesto**: el proyecto no tenía fuentes de diseño (ni CSS, ni tokens, ni librería de componentes), así que estos valores no vienen de código existente. Fuente de cada fila: propuesta inicial de esta sesión de diseño (`design/system.html`). Plataforma: .NET MAUI, solo Android.

## Colors

| Token | Value | Usage | Source |
| --- | --- | --- | --- |
| `--color-primary` | `#0B6E6E` | Acciones principales, selección, tab activa, enlaces (6.0:1 sobre blanco) | design/system.html |
| `--color-primary-dark` | `#064A4A` | Texto sobre primary-tint, iconos activos | design/system.html |
| `--color-primary-tint` | `#E3F1F1` | Fondo de selección, chips activos, avisos informativos | design/system.html |
| `--color-background` | `#F3F7F7` | Fondo de pantalla | design/system.html |
| `--color-surface` | `#FFFFFF` | Tarjetas, campos, barra superior, tabs | design/system.html |
| `--color-ink` | `#102426` | Texto principal | design/system.html |
| `--color-muted` | `#455B5D` | Texto secundario (7.2:1 sobre blanco) | design/system.html |
| `--color-placeholder` | `#5C7173` | Placeholder (5.0:1 sobre blanco) | design/system.html |
| `--color-border` | `#6F8A8C` | Borde de controles (3.7:1, cumple 3:1 de componentes) | design/system.html |
| `--color-divider` | `#D5E2E3` | Separadores y borde de tarjetas (decorativo) | design/system.html |
| `--color-disabled` | `#E6ECEC` | Fondo de horario ocupado y estado Cancelada | design/system.html |
| `--color-danger` | `#B3261E` | Errores y cancelar (6.5:1 sobre blanco) | design/system.html |
| `--color-danger-tint` | `#FCE8E6` | Fondo de aviso de error | design/system.html |
| `--color-success` | `#1B6B3A` | Completada y registro correcto (6.5:1) | design/system.html |
| `--color-success-tint` | `#E2F3E8` | Fondo de éxito | design/system.html |
| `--color-warning` | `#7A4E00` | Avisos no bloqueantes | design/system.html |
| `--color-warning-tint` | `#FFF1D6` | Fondo de aviso | design/system.html |

## Typography

| Token | Value | Usage | Source |
| --- | --- | --- | --- |
| `--font` | `Roboto, system-ui, sans-serif` | Fuente del sistema Android; no se empaqueta ninguna fuente | design/system.html |
| `--text-title` | `24/32 600` | Título de pantalla | design/system.html |
| `--text-dialog` | `20/28 600` | Título de diálogo y estado vacío | design/system.html |
| `--text-section` | `18/24 600` | Encabezado de sección | design/system.html |
| `--text-body` | `16/24 400` | Cuerpo y texto de campos | design/system.html |
| `--text-label` | `14/20 500` | Etiquetas, texto secundario, avisos | design/system.html |
| `--text-caption` | `12/16 400` | Pie, contador, etiquetas de pestaña y de bloque | design/system.html |

## Spacing

| Token | Value | Usage | Source |
| --- | --- | --- | --- |
| `--space-1` | `4px` | Entre etiqueta y campo | design/system.html |
| `--space-2` | `8px` | Entre filas de lista y chips | design/system.html |
| `--space-3` | `12px` | Relleno de aviso y tarjeta | design/system.html |
| `--space-4` | `16px` | Margen de pantalla y separación entre grupos | design/system.html |
| `--space-5` | `24px` | Relleno de diálogo | design/system.html |
| `--space-6` | `32px` | Separación de estados vacíos | design/system.html |

## Radius

| Token | Value | Usage | Source |
| --- | --- | --- | --- |
| `--radius-control` | `8px` | Botones, campos, bloques de horario | design/system.html |
| `--radius-card` | `12px` | Tarjetas y avisos | design/system.html |
| `--radius-sheet` | `16px` | Diálogos | design/system.html |

## Shadows

| Token | Value | Usage | Source |
| --- | --- | --- | --- |
| `--shadow-dialog` | `0 8px 24px rgba(16,36,38,.24)` | Diálogo | design/system.html |
| `--shadow-bar` | `0 -4px 12px rgba(16,36,38,.08)` | Barra de acción inferior de Horarios | design/system.html |

## Component rules

- **Objetivos táctiles**: 48 px en botones y campos, mínimo 44 px en chips, fechas y acciones de texto.
- **Botón**: principal (relleno primary, texto blanco), secundario (borde primary), de texto, destructivo (relleno danger). Un solo principal por pantalla.
- **Campo**: etiqueta visible encima, nunca solo placeholder; error en texto con icono y borde de 2 px danger.
- **Chip**: seleccionado = tint + borde 2 px primary + check + texto en negrita (no solo color).
- **Bloque de horario**: libre (borde), ocupado (fondo disabled, texto "Ocupado", hora tachada, no tocable), elegido (relleno primary, check, texto "Elegido").
- **Estado de cita**: Programada (calendario, primary), Completada (check, success), Cancelada (x, gris); siempre texto e icono.
- **Aviso**: info, error, advertencia y éxito, con icono y texto; los de error usan `role=alert`.
- **Diálogo**: título, descripción corta, datos clave y dos acciones; la destructiva en danger, la segura a la izquierda.
- **Estados vacíos**: qué va aquí y una acción; con una consulta sin resultados se cita la consulta y se ofrece limpiarla.

## Mapeo a MAUI

Copiar los colores a `Resources/Styles/Colors.xaml` con la clave en PascalCase (`Primary`, `PrimaryDark`, `PrimaryTint`, `Background`, `Surface`, `Ink`, `Muted`, `Placeholder`, `Border`, `Divider`, `Disabled`, `Danger`, `DangerTint`, `Success`, `SuccessTint`, `Warning`, `WarningTint`) y los estilos (botones, campos, chips, tarjetas, estados) a `Resources/Styles/Styles.xaml`. Iconos: Material Symbols exportados como SVG en `Resources/Images`, o `FontImageSource`.

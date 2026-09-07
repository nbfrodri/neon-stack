# NEON STACK

Tetris de escritorio para Windows, con estética retro y ejecutable nativo en C# / Windows Forms. Sin navegador, cuentas ni conexión a Internet.

## Jugar

Abre **`dist/NeonStack.exe`** o haz doble clic en **`Jugar.cmd`**. Pulsa **Enter** o **Jugar** para empezar.

El ejecutable funciona por separado: puedes copiarlo a otra carpeta. Requiere Windows con .NET Framework 4.x, presente en el equipo donde se ha compilado. No necesita el SDK de .NET ni instalar paquetes. Solo se permite una instancia por usuario para proteger el archivo de récords.

## Ventana compacta y sonido

Pulsa **MINI** o **F2** para cambiar a una ventana compacta (aproximadamente 320 × 720 incluyendo el marco de Windows). Puedes reducirla más arrastrando los bordes. Conserva tablero, puntos, nivel, siguiente pieza y controles; **AMPLIAR / F2** recupera el diseño completo sin reiniciar la partida.

**ENCIMA / F3** activa «Siempre encima»: coloca la ventana sobre un vídeo y muévela a una esquina. **FIJADO / F3** desactiva esa opción. La pausa al perder el foco sigue funcionando.

La **siguiente pieza aparece inmediatamente encima del tablero en ambos modos**. En el completo se mantiene además la cola lateral de tres piezas. En el compacto, **TOP 20** abre los récords en una ventana aparte.

**SONIDO / F4** silencia los efectos y cambia a **MUDO**; pulsa otra vez para recuperarlos. **VOL** abre una barra que puedes arrastrar, con botones − y +. El volumen inicial es 35 % y solo afecta al juego. Hay efectos retro de giro, fijación, líneas, Tetris, inicio y fin de partida. Se generan localmente, sin archivos de sonido externos. Si no hay salida de audio disponible, el juego sigue funcionando.

El volumen, silencio, modo compacto y opción de estar encima se conservan al cerrar la ventana. El icono está incorporado al `.exe` en siete resoluciones, de 16 a 256 píxeles.

## Controles

| Acción | Teclas |
| --- | --- |
| Mover a izquierda / derecha | A / D o ← / → |
| Girar a la derecha | W o ↑ |
| Girar a la izquierda | Z |
| Bajar más rápido | S o ↓ |
| Caer y fijar inmediatamente | Espacio |
| Pausar / continuar | Esc o P |
| Abrir / cerrar clasificación | Tab; Esc para cerrar |
| Empezar / volver a jugar | Enter |
| Guardar iniciales al terminar | A–Z / 0–9, retroceso y Enter |

La partida se pausa al cambiar a otra ventana. Pulsa Esc o Continuar al volver.

## Reglas

- Tablero de 10 × 20 con cuadrícula visible. La silueta indica la posición exacta de la caída instantánea.
- Vista de las próximas tres piezas. Reparto en bolsas de siete: cada bolsa contiene una pieza de cada tipo.
- Giros de estilo retro: I, S y Z alternan entre dos posiciones estables; T, J y L recorren cuatro orientaciones. Así, dos giros de una pieza simétrica recuperan exactamente sus casillas en espacio libre. Se mantienen las pruebas de desplazamiento junto a obstáculos; es una variante retro, no una implementación estricta de SRS.
- Al tocar el suelo dispones de **300 ms** para ajustar la pieza. Mover o girar correctamente renueva ese margen hasta **6 veces por pieza**. Un movimiento bloqueado o girar el cuadrado no lo renueva. La barra bajo el tablero muestra el tiempo restante.
- Espacio fija la pieza al instante, sin esperar ese margen.
- Limpiar 1 / 2 / 3 / 4 líneas da **100 / 300 / 500 / 800 × nivel** puntos. Descenso suave: 1 punto por casilla; caída instantánea: 2 puntos por casilla.
- Cada diez líneas aumenta el nivel y la velocidad. No se aplican bonificaciones de combos o T-spins.

## Récords locales

Se conservan las **20 mejores partidas**, con iniciales, puntos, nivel, líneas y fecha. El panel lateral muestra las cinco primeras; **Ver clasificación** abre la tabla completa.

Al terminar, la puntuación se guarda automáticamente con las últimas iniciales. Escribe hasta tres caracteres y pulsa Enter para cambiarlas. Si cierras la ventana durante esa edición, también se intenta guardar el nombre.

Datos: **`%LOCALAPPDATA%\NeonStack\scores.json`**. Puedes copiar este archivo para trasladar tus récords. Las escrituras son atómicas y se conserva `scores.json.bak`. Si el archivo principal está dañado, se intenta recuperar la copia; el archivo dañado se conserva al guardar de nuevo. Si no se puede escribir, la aplicación lo indica y mantiene los resultados en memoria durante la sesión.

## Compilar y verificar

Desde PowerShell, en la carpeta del proyecto:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\build.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File .\test.ps1
```

Los scripts usan el compilador incluido con .NET Framework y no descargan dependencias. `build.ps1` genera `dist/NeonStack.exe`. `test.ps1` compila y ejecuta las pruebas en `artifacts`.

Captura reproducible del mismo renderizador utilizado por la ventana, sin abrir una ventana interactiva:

```powershell
.\dist\NeonStack.exe --screenshot .\artifacts\preview.png playing
```

Modos disponibles: `ready`, `playing`, `paused`, `gameover`, `records`, `volume`, `compact`, `compact-ready`, `compact-paused`, `compact-gameover`, `compact-volume`. Puedes añadir ancho y alto al final, por ejemplo `compact 306 684`. Las capturas usan datos de demostración en memoria, no reproducen sonido y no alteran tus récords.

## Estructura

- `src/Game.cs`: reglas, colisiones, SRS, gravedad y fijación; independiente de la interfaz.
- `src/GameForm.cs` y `src/PixelFont.cs`: interfaz, dibujo con doble búfer y teclado.
- `src/ScoreStore.cs`: persistencia y recuperación.
- `tests/Tests.cs`: pruebas de reglas, teclado, renderizado y archivos.
- `docs/plans`: diseño aprobado, plan y resultados de verificación.
- `skills/writing-plans/SKILL.md`: copia de la habilidad creada durante este proyecto e instalada en la carpeta personal de habilidades.

# NEON STACK

Tetris de escritorio para Windows, con estética retro y ejecutable nativo en C# / Windows Forms. Sin navegador, cuentas ni conexión a Internet.

## Descargar y jugar

Descarga **NeonStack.exe** desde [GitHub Releases](https://github.com/nbfrodri/neon-stack/releases) y ábrelo. No necesita instalador. Para actualizar, cierra el juego y sustituye el ejecutable; los récords y preferencias se guardan fuera de su carpeta.

Si has clonado el repositorio, primero compila o utiliza `Jugar.cmd`, que compila automáticamente si falta el ejecutable. Los binarios no están versionados en Git.

Abre **`dist/NeonStack.exe`** o haz doble clic en **`Jugar.cmd`**. Pulsa **Enter** o **Jugar** para empezar.

El ejecutable funciona por separado: puedes copiarlo a otra carpeta. Requiere Windows con .NET Framework 4.x, presente en el equipo donde se ha compilado. No necesita el SDK de .NET ni instalar paquetes. Solo se permite una instancia por usuario para proteger el archivo de récords.

## Ventana compacta y sonido

Pulsa **MINI** o **F2** para cambiar a una ventana compacta (aproximadamente 320 × 720 incluyendo el marco de Windows). Puedes reducirla más arrastrando los bordes. Conserva tablero, puntos, nivel, siguiente pieza y controles; **AMPLIAR / F2** recupera el diseño completo sin reiniciar la partida.

**ENCIMA / F3** activa «Siempre encima»: coloca la ventana sobre un vídeo y muévela a una esquina. **FIJADO / F3** desactiva esa opción. La pausa al perder el foco sigue funcionando.

La **siguiente pieza aparece inmediatamente encima del tablero en ambos modos**. En el completo se mantiene además la cola lateral de tres piezas. En el compacto, **TOP 20** abre los récords en una ventana aparte.

**FANTASMA ON/OFF** muestra u oculta la silueta de caída. Está junto a Ayuda en el modo completo y en la barra inferior del compacto. Se puede cambiar durante la partida y la preferencia se conserva al cerrar. Por defecto está activada; ocultarla no modifica la caída instantánea ni las reglas.

**El botón de altavoz, M o F4** silencia los efectos; el icono muestra una cruz cuando están silenciados y ondas cuando están activos. Pulsa otra vez para recuperarlos. **VOL** pausa la partida y abre una barra que puedes arrastrar, con botones − y + y ajuste mediante flechas. **Esc**, **Enter** o **X** cierran el panel y recuperan el estado anterior: una partida que ya estaba pausada sigue pausada. El volumen inicial es 35 % y solo afecta al juego. Hay efectos retro de giro, fijación, líneas, Tetris, inicio y fin de partida. Se generan localmente, sin archivos de sonido externos. El mezclador admite hasta ocho efectos simultáneos y prioriza las melodías sobre los giros; un giro no interrumpe una melodía de líneas. Si no hay salida de audio disponible, el juego sigue funcionando.

El volumen, silencio, modo de juego y opción de estar encima se conservan al cerrar la ventana. También se recuerda **el tamaño y la posición por separado para el diseño completo y compacto**. Al desconectar un monitor, la ventana se ajusta al área de trabajo disponible. Si se cierra maximizada o minimizada, se conserva su tamaño normal.

El icono está incorporado al `.exe` en siete resoluciones, de 16 a 256 píxeles.

## Modos de juego

Antes de empezar, pulsa el botón **MODO** o **F5** para elegir:

- **Clásico:** juega hasta llenar el tablero y compite por puntuación.
- **Sprint 40:** limpia al menos 40 líneas lo más rápido posible. El cronómetro muestra minutos, segundos y milisegundos; se detiene al pausar, perder el foco, abrir el volumen o consultar la clasificación. Al alcanzar el objetivo termina la partida. Una última limpieza múltiple puede superar 40 líneas.

Sprint conserva la progresión de velocidad y las reglas de movimiento del modo clásico. Solo los intentos completados entran en su clasificación por tiempo, del menor al mayor. Sus récords están separados de los puntos del modo clásico. No se puede cambiar de modo durante una partida ni mientras se editan las iniciales del resultado.

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
| Silenciar / activar audio | M o F4; también el botón de altavoz |
| Cambiar entre modo compacto y completo | F2 |
| Activar / desactivar «Siempre encima» | F3 |
| Elegir Clásico / Sprint 40 antes de empezar | F5 |
| Cerrar el resultado, tras guardar las iniciales | X o botón Salir |
| Abrir / cerrar Ayuda | F1; Esc para cerrar |
| Guardar iniciales al terminar | A–Z / 0–9, retroceso y Enter |

La partida se pausa al cambiar a otra ventana. Pulsa Esc o Continuar al volver.

**Ayuda / F1** muestra todos los controles dentro del juego; en la ventana compacta se abre con **?**. Abrirla pausa la partida, y cerrarla recupera el estado anterior.

Mientras escribes iniciales, **M escribe la letra M**; F4 sigue disponible para silenciar.

En pausa puedes pulsar **Terminar partida**. En Clásico se guarda la puntuación actual; un Sprint incompleto queda fuera de la clasificación. Al finalizar puedes volver a jugar con Enter o cerrar solo la pantalla de resultados con **Salir**: la aplicación permanece abierta y conserva el tablero y la puntuación. **X** hace lo mismo después de guardar las iniciales; durante su edición escribe la letra X. El botón Salir también funciona durante esa edición y guarda el nombre. Después puedes empezar otra partida con Enter o el botón Jugar/Nueva partida.

## Reglas

- Tablero de 10 × 20 con cuadrícula visible. La silueta indica la posición exacta de la caída instantánea.
- Vista de las próximas tres piezas. Reparto en bolsas de siete: cada bolsa contiene una pieza de cada tipo.
- Giros de estilo retro: I, S y Z alternan entre dos posiciones estables; T, J y L recorren cuatro orientaciones. Así, dos giros de una pieza simétrica recuperan exactamente sus casillas en espacio libre. Se mantienen las pruebas de desplazamiento junto a obstáculos; es una variante retro, no una implementación estricta de SRS.
- Al tocar el suelo dispones de **300 ms** para ajustar la pieza. Mover o girar correctamente renueva ese margen hasta **6 veces por pieza**. Un movimiento bloqueado o girar el cuadrado no lo renueva. La barra bajo el tablero muestra el tiempo restante.
- Espacio fija la pieza al instante, sin esperar ese margen.
- Limpiar 1 / 2 / 3 / 4 líneas da **100 / 300 / 500 / 800 × nivel** puntos. Descenso suave: 1 punto por casilla; caída instantánea: 2 puntos por casilla.
- Cada diez líneas aumenta el nivel y la velocidad. No se aplican bonificaciones de combos o T-spins.

## Récords locales

Se conservan las **20 mejores partidas de cada modo**, con iniciales, puntuación o tiempo, nivel, líneas y fecha. El panel lateral muestra las cinco primeras del modo seleccionado; **Ver clasificación** abre su tabla completa.

Al terminar una partida clásica o completar Sprint, el resultado se guarda automáticamente con las últimas iniciales. Escribe hasta tres caracteres y pulsa Enter para cambiarlas. Si cierras la ventana durante esa edición, también se intenta guardar el nombre. Los intentos Sprint incompletos no se registran como tiempos válidos.

Datos: **`%LOCALAPPDATA%\NeonStack\scores.json`**. Puedes copiar este archivo para trasladar tus récords. Las escrituras son atómicas y se conserva `scores.json.bak`. Si el archivo principal falta o está dañado, se intenta recuperar la copia; el archivo dañado se conserva al guardar de nuevo. Si no se puede escribir, la aplicación lo indica y mantiene los resultados en memoria durante la sesión. Al cerrar manualmente tras un error de guardado, permite cancelar el cierre o descartar explícitamente los cambios sin guardar.

## Compilar y verificar

Desde PowerShell, en la carpeta del proyecto:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\build.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File .\test.ps1
```

Los scripts usan el compilador incluido con .NET Framework y no descargan dependencias. `build.ps1` genera `dist/NeonStack.exe`. `test.ps1` compila y ejecuta las pruebas en `artifacts`.

Cierra el juego antes de recompilar: Windows bloquea los ejecutables en uso. Para compilar temporalmente otra copia, puedes usar `build.ps1 -ExecutableName NeonStack-preview.exe`.

Prueba de audio mediante el mismo mezclador y la salida nativa usados por el juego:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\audio-check.ps1 -Listen
```

Se oyen una melodía Tetris con giros simultáneos, un intervalo silenciado y otra melodía. Comprueba que la melodía no se corta y que no hay chasquidos. Sin `-Listen`, solo genera `artifacts/audio-mix.wav`, útil para revisión sin altavoces o en CI. La reproducción automática no sustituye la escucha humana.

Captura reproducible del mismo renderizador utilizado por la ventana, sin abrir una ventana interactiva:

```powershell
.\dist\NeonStack.exe --screenshot .\artifacts\preview.png playing
```

Modos disponibles: `ready`, `playing`, `paused`, `gameover`, `records`, `volume`, `compact`, `compact-ready`, `compact-paused`, `compact-gameover`, `compact-volume`. Puedes añadir ancho y alto al final, por ejemplo `compact 306 684`. Las capturas usan datos de demostración en memoria, no reproducen sonido y no alteran tus récords.

También están disponibles `records-filled`, `sprint-ready`, `sprint`, `sprint-records-filled`, `compact-sprint-ready`, `compact-sprint`, `help` y `compact-help`.

## Publicar releases en GitHub

El repositorio incluye [`.github/workflows/release.yml`](.github/workflows/release.yml). Al subir una etiqueta que empiece por **v**, el workflow ejecuta las pruebas en Windows, compila y crea una release con **NeonStack.exe** adjunto y notas generadas automáticamente.

Después de revisar y subir tus cambios, crea y sube una etiqueta nueva. Por ejemplo, sustituyendo `v1.1.0` por la versión que corresponda:

```powershell
git tag -a v1.1.0 -m "Neon Stack v1.1.0"
git push origin v1.1.0
```

Revisa la ejecución en **Actions** y la descarga en **Releases**. El ZIP de código fuente que genera GitHub contiene fuentes, no el juego compilado: para jugar se descarga el archivo `NeonStack.exe` adjunto.

`.gitignore` excluye `dist/`, `artifacts/`, ejecutables, paquetes locales de release, archivos de IDE y datos locales del juego. Se versionan las fuentes, scripts, documentación, icono y workflow; los ejecutables se distribuyen como assets de las releases. El workflow regenera el icono durante la compilación.

## Estructura

- `src/Game.cs`: reglas, colisiones, SRS, gravedad y fijación; independiente de la interfaz.
- `src/GameForm.cs` y `src/PixelFont.cs`: interfaz, dibujo con doble búfer y teclado.
- `src/ScoreStore.cs`: persistencia y recuperación.
- `src/WindowPlacement.cs`: restauración de tamaño y posición dentro del área de trabajo.
- `src/Audio.cs`, `src/AudioMixer.cs` y `src/WaveOutput.cs`: efectos, mezcla con prioridades y salida nativa.
- `src/AppIcon.cs` y `tools/IconBuilder.cs`: icono multirresolución integrado.
- `tests/Tests.cs`: pruebas de reglas, teclado, renderizado y archivos.
- `docs/plans`: diseño aprobado, plan y resultados de verificación.
- `skills/writing-plans/SKILL.md`: copia de la habilidad creada durante este proyecto e instalada en la carpeta personal de habilidades.

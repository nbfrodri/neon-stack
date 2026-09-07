# Ventana persistente y Sprint 40

Implementación autorizada: recordar tamaño y posición y añadir modo de 40 líneas con clasificación por tiempo.

1. `src/WindowPlacement.cs` nuevo: serializar límites normales por diseño; ajustarlos a un área de trabajo disponible. Mantener tamaños distintos de modo compacto y completo y no guardar los límites minimizados/maximizados como tamaño normal.
2. `src/ScoreStore.cs`: preferencias de ventana y modo, lista separada de Sprint con duración en milisegundos; migración tolerante con archivos antiguos. Orden ascendente por tiempo y máximo 20 entradas.
3. `src/Game.cs`: modo Sprint, tiempo activo y estado completado al alcanzar al menos 40 líneas. Las pausas no suman tiempo; una última limpieza múltiple puede superar 40. Conservar mecánica y progresión de velocidad actuales.
4. `src/GameForm.cs`: selector antes de jugar, tiempo y líneas restantes, pantalla de finalización y clasificación correspondiente en ambos diseños. Tiempo con reloj monotónico, actualizado antes de procesar las acciones para contar también caídas rápidas entre fotogramas.
5. Verificar persistencia, monitores desconectados, cambio de diseño, victoria exacta y por sobrepaso, pausa, intentos incompletos, clasificación por tiempo y compatibilidad. Compilar con `build.ps1`, ejecutar `test.ps1` y revisar capturas de ambos modos.

Los modos usan récords separados. Los intentos Sprint que terminan por colisión no entran en la clasificación por tiempo. No se cambia el modo durante una partida activa.

## Ampliaciones autorizadas durante la implementación

- Icono de altavoz, M/F4 para silencio y mezclador de hasta ocho voces; melodías con prioridad sobre giros.
- Prueba de audio reproducible con `audio-check.ps1 -Listen`. Usa el mismo mezclador y salida nativa del juego.
- Actualización de README y `.gitignore` respetando el remoto GitHub y el workflow de releases existentes. No se publicaron etiquetas, commits ni releases.
- Terminar partida desde pausa y Salir al finalizar; X sale una vez guardadas las iniciales. M y X siguen siendo letras mientras se edita el nombre.
- Panel Ayuda con todos los controles, accesible mediante F1 y un botón en ambos diseños; conserva la pausa anterior.

## Verificación

70 pruebas superadas y compilación optimizada correcta. Revisadas capturas de Sprint, clasificación por tiempo, finalización, ventana compacta, pausa, salida y Ayuda. Verificadas recuperación de posición con monitores desconectados, persistencia de ambos diseños, exclusión de pausas del cronómetro, clasificación separada, mezcla simultánea y prioridad de melodías.

La prueba nativa de audio se ejecutó con salida disponible y sin errores del dispositivo. Se solicitó confirmación auditiva al usuario, sin respuesta recibida al registrar este resultado; no se afirma una escucha humana validada. La prueba puede repetirse con el comando documentado.

La gestión de buffers de audio sigue los contratos de [waveOutPrepareHeader](https://learn.microsoft.com/en-us/windows/win32/api/mmeapi/nf-mmeapi-waveoutprepareheader) y [waveOutWrite](https://learn.microsoft.com/en-us/windows/win32/api/mmeapi/nf-mmeapi-waveoutwrite).

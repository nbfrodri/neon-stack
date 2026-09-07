# Sonido, icono y ventana compacta

Petición del usuario: sonido con botones de silencio y volumen, icono del ejecutable mejorado y modo minimalista para jugar en una ventana pequeña junto a un vídeo.

Implementación: efectos sintetizados localmente en `src/Audio.cs`, con volumen aplicado a las muestras, sin modificar el volumen de Windows. Controles de silencio y panel de volumen en ambos diseños. Preferencias en el archivo local existente, con valores compatibles con archivos anteriores.

Modo compacto en `GameForm.cs`: lienzo de 340 × 760, mínimo de ventana reducido, tablero y siguiente pieza legibles, controles accesibles y opción de permanecer encima de otras ventanas. Cambiar de modo conserva la partida. La clasificación usa un diálogo separado desde el modo compacto. Se mantiene la pausa automática al perder el foco.

Icono original dibujado con código en varios tamaños e incorporado como recurso del ejecutable, además del icono de la ventana. Sin paquetes descargados.

Verificación: pruebas de muestras/volumen, preferencias y compatibilidad, transiciones de modo sin reinicio, accesibilidad de controles, renderizado compacto y compilación con el icono. Capturas para revisar proporciones y legibilidad.

Petición adicional incorporada: mostrar la siguiente pieza encima del tablero en ambos diseños, conservando la cola lateral de tres en el completo.

Resultado: compilación correcta; 49 pruebas superadas. Revisadas las capturas de partida completa, partida compacta, inicio compacto, panel de volumen e icono de 256 píxeles. Los efectos WAV se validaron con SoundPlayer.Load y comprobaciones de amplitud, silencio y envolvente; no se realizó una escucha manual. La interacción se verificó con eventos automatizados de formulario.

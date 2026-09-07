# Plan de implementación

## Aceptación

Ejecutable Windows con tablero 10 × 20, casillas delimitadas, pieza fantasma exacta, próximas piezas, WASD/flechas, caída instantánea con espacio y margen de fijación de 500 ms. Hasta 15 renovaciones por movimientos válidos. Récords persistentes tras cerrar y abrir, con iniciales, puntuación, nivel y fecha. Pausa al perder el foco. Interfaz retro escalable y sin dependencias descargables.

## Tareas y comprobaciones

1. Completar `src/Game.cs` (existente): motor determinista con bolsas de siete, colisiones, rotaciones SRS, fantasma, puntuación y fijación. Crear `tests/Tests.cs` para verificar límites, caídas, giros junto a paredes y suelo, despeje de 1–4 líneas, pausa y agotamiento de renovaciones.
2. Crear `src/ScoreStore.cs`: registros JSON en LocalAppData, escrituras mediante archivo temporal y reemplazo atómico, respaldo y recuperación ante datos dañados. Probar en carpeta temporal independiente persistencia, orden, iniciales, límites y recuperación, sin modificar los récords reales.
3. Crear `src/PixelFont.cs`, `src/GameForm.cs` y `src/Program.cs`: dibujo con doble búfer, reloj monotónico, repetición de teclas independiente de Windows, pantalla de inicio, pausa, fin de partida, iniciales y clasificación. Probar secuencias de entrada y generar capturas con la misma función de dibujo de la ventana.
4. Crear `build.ps1`, `test.ps1`, `Jugar.cmd` y manifiesto: compilar con el compilador .NET Framework instalado. Generar `dist/NeonStack.exe` como ejecutable independiente. No requiere SDK moderno ni paquetes NuGet.
5. Ejecutar `powershell -NoProfile -ExecutionPolicy Bypass -File test.ps1` y `powershell -NoProfile -ExecutionPolicy Bypass -File build.ps1`; deben terminar con código 0. Generar y revisar capturas PNG de inicio, partida y tamaños alternativos. Corregir cualquier problema observado antes de repetir comprobaciones relevantes.
6. Crear `README.md` con ejecución, controles, reglas de puntuación, ubicación de datos y compilación. Entregar enlaces al ejecutable y a la habilidad creada.

## Estado del entorno

La habilidad writing-plans se creó, validó e instaló por petición del usuario y se aplicó a este plan. El compilador disponible es `C:/Windows/Microsoft.NET/Framework64/v4.0.30319/csc.exe`. La carpeta no contiene un repositorio Git, por lo que no se crea un commit. Los resultados efectivos de verificación se registrarán por separado de los pasos previstos.

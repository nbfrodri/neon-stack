# Verificación

- Compilación optimizada con advertencias tratadas como errores: correcta, sin paquetes externos.
- 39 pruebas automatizadas correctas: reglas de las siete piezas, SRS de I y T junto al suelo y paredes, bloqueo de giros, fantasma frente a caída real, puntuación y limpieza de 1–4 líneas, nivel, pausa y margen de fijación.
- Prueba con 20.000 acciones aleatorias: sin solapamientos de la pieza activa ni posiciones inválidas del fantasma.
- Persistencia verificada en directorios temporales aislados: escritura, recarga, orden, límite de 20 entradas, edición de iniciales, recuperación de respaldo, preservación del archivo dañado y error de escritura.
- Integración de teclado mediante eventos de Windows Forms: WASD y flechas, espacio, pausa sin repetición accidental, pérdida de foco, clasificación, fin de partida, guardado y reinicio.
- Renderizado a tamaños 1100 × 820 y 884 × 681 de inicio, partida, pausa, clasificación y fin de partida. Revisión visual de las capturas de inicio, partida, ventana reducida y fin de partida. Se suavizó la tipografía bitmap al reducir la ventana para evitar pérdida de trazos.
- La comprobación de interfaz se hizo con el renderizador real y eventos de formulario automatizados; no se realizó una partida manual en una ventana interactiva.
- `writing-plans`: validación con `skill-creator/scripts/quick_validate.py` correcta; instalada en `C:/Users/Phobos/.codex/skills/writing-plans/SKILL.md` con autorización de escritura fuera del espacio de trabajo.

Artefactos: `dist/NeonStack.exe`, `artifacts/Tests.exe` y capturas PNG en `artifacts`.

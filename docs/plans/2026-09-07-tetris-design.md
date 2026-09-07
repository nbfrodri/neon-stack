# NEON STACK — diseño aprobado

Aplicación nativa para Windows, implementada en C# y Windows Forms. Dibujado propio con doble búfer, estética arcade oscura, colores brillantes, tipografía de píxeles y tablero visible de 10 × 20. Pieza fantasma y vista de tres piezas siguientes.

Controles: A/D o izquierda/derecha para mover, W/arriba para girar, S/abajo para descenso suave, espacio para caída instantánea, Escape para pausar. Giro antihorario opcional con Z. Repetición lateral controlada por el juego. Pausa automática al perder el foco.

Fijación tras 500 ms en contacto con el suelo; movimientos y giros válidos permiten hasta 15 reinicios del margen por pieza. La caída instantánea fija inmediatamente. Rotaciones con pruebas de desplazamiento SRS y reparto de piezas en bolsas de siete.

Puntuación por líneas (100/300/500/800 × nivel), descenso suave y caída instantánea. Nivel nuevo cada diez líneas. Récords locales con iniciales, fecha, puntos y nivel. Guardado JSON atómico, copia de seguridad y recuperación ante archivos dañados.

Motor independiente de la interfaz y del guardado. Comprobaciones automatizadas de colisiones, giros, despeje de líneas, puntuación, margen de fijación, pausa y persistencia. Verificación visual mediante renderizado a PNG.

El usuario aprobó este diseño en la conversación. El entorno dispone del compilador de .NET Framework, pero no de un SDK de .NET moderno; se utiliza .NET Framework 4.x sin paquetes externos.

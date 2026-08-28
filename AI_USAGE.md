# Declaración de uso de inteligencia artificial

## Herramientas utilizadas

Se utilizó ChatGPT de OpenAI como herramienta de apoyo durante el desarrollo
de la prueba técnica.

## Tareas para las que se utilizaron

Se utilizó principalmente para:

- Revisar el código existente y analizar los requerimientos de la prueba.
- Apoyar la implementación del cliente HTTP asincrónico y su configuración
  mediante inyección de dependencias.
- Revisar alternativas para controlar concurrencia, caché y duplicidad de favoritos.
- Apoyar el diseño e implementación de las alertas meteorológicas por umbral.
- Revisar manejo de errores, seguridad por usuario y validaciones.
- Analizar errores encontrados durante las pruebas manuales.
- Apoyar la documentación final de las decisiones técnicas.

## Revisión personal

Todo cambio sugerido fue revisado antes de incorporarlo al proyecto.

Durante el desarrollo ejecuté repetidamente dotnet build y dotnet test.
También realicé pruebas manuales desde la interfaz, incluyendo creación,
evaluación, activación, desactivación y eliminación de alertas, además de la
actualización manual del clima.

Las sugerencias se adaptaron a la estructura existente del proyecto y se
corrigieron problemas detectados durante las pruebas manuales.

## Declaración

Confirmo que comprendo el código entregado y que puedo explicarlo, modificarlo
y diagnosticarlo durante una instancia posterior.

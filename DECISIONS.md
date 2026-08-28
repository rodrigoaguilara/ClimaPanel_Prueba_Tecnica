# Decisiones técnicas

## 1. Problemas principales identificados

Se identificaron problemas en el consumo HTTP, control de acceso a favoritos,
duplicidad de ciudades, manejo de caché, actualización manual y manejo de errores.
También se implementó la funcionalidad de alertas meteorológicas solicitada.

## 2. Integración HTTP, asincronía, cancelación y timeout

El cliente de Open-Meteo se configuró mediante HttpClient usando inyección de
dependencias. Las llamadas se realizan de forma asincrónica y reciben
CancellationToken.

Se agregó timeout para evitar que una llamada externa quede esperando
indefinidamente. También se controlan errores HTTP, timeout y respuestas JSON
inválidas, mostrando mensajes simples al usuario y registrando información
técnica mediante logging.

## 3. Integridad y concurrencia al crear favoritos

Se agregó un índice único en SQLite para la combinación UserId y LocationId.
De esta forma un usuario no puede registrar dos veces la misma ciudad, incluso
si se producen solicitudes concurrentes.

También se controla la excepción de base de datos para entregar un mensaje
entendible al usuario.

## 4. Separación de datos entre usuarios

Las consultas, actualizaciones y eliminaciones de favoritos verifican tanto el
identificador del recurso como el UserId actual.

Las alertas también verifican que la ciudad pertenezca al usuario antes de
permitir operaciones sobre ellas.

## 5. Estrategia de caché y actualización forzada

Se utiliza caché independiente por ciudad.

Para reducir llamadas simultáneas al proveedor se utiliza un bloqueo por ciudad.
Los datos obtenidos directamente desde Open-Meteo se identifican como LIVE,
los obtenidos desde caché como CACHE y el último dato disponible ante una falla
del proveedor como STALE.

La opción "Actualizar ahora" fuerza una nueva consulta del clima.

## 6. Diseño completo de alertas por umbral

Las alertas se almacenan en SQLite y están asociadas a un favorito.

Se implementaron las operaciones de crear, listar, activar/desactivar, eliminar
y evaluar alertas.

Las métricas disponibles son temperatura, humedad, precipitación y velocidad
del viento, utilizando los operadores mayor o igual y menor o igual.

Se validan los rangos definidos por la prueba y se limita a cinco alertas
activas por ciudad.

La evaluación guarda la fecha de evaluación, el estado de la alerta y la fecha
en que fue disparada.

Al eliminar un favorito sus alertas se eliminan mediante la relación configurada
con eliminación en cascada.

Las operaciones que modifican alertas utilizan POST y validación antiforgery.

## 7. Persistencia, consultas y paginación

La persistencia se realiza con Entity Framework Core y SQLite.

El listado de favoritos aplica filtro, ordenamiento, conteo y paginación antes
de materializar los resultados, evitando cargar registros innecesarios en memoria.

## 8. Pruebas agregadas

Se utilizaron las pruebas automatizadas incluidas en el proyecto durante el
desarrollo.

Antes de la entrega se ejecutaron:

dotnet build
dotnet test

El resultado final fue compilación sin errores ni advertencias y 5 pruebas
superadas.

También se realizaron pruebas manuales sobre creación, evaluación,
activación/desactivación y eliminación de alertas.

## 9. Limitaciones conocidas y trabajo pendiente

Como trabajo futuro se podrían ampliar las pruebas automatizadas para cubrir
más escenarios de concurrencia, fallas del proveedor externo y operaciones
sobre alertas.

# MBOS.Hub.BackToSchool (API)

API de **BackToSchool**: pedidos de textos, uniformes y materiales escolares de la temporada de
vuelta a clases (Educación Adventista · MBOS). Reemplaza la "hojita" de pedido: el promotor arma el
pedido en el celular, sale un número de ficha, almacén imprime la hoja y prepara los materiales, y
caja factura en su propio sistema.

Misma arquitectura que `MBOS.Hub.CheckInvoice` (.NET 10, PostgreSQL, capas core / Application /
Infrastructure / Web, JWT con refresh token, permisos `visita` / `trabajo`, menú por rol).

## Puesta en marcha (local)

1. PostgreSQL: crear la base y correr los scripts en orden:

   ```sql
   CREATE DATABASE "BackToSchool" ENCODING UTF8;
   ```

   ```bash
   psql -d BackToSchool -f Scripts/01_schema.sql
   psql -d BackToSchool -f Scripts/02_seed_security.sql
   psql -d BackToSchool -f Scripts/03_seed_catalog.sql
   ```

2. Crear `BackToSchool.Web/appsettings.Development.json` (no se sube al repositorio):

   ```json
   {
     "ConnectionStrings": { "DefaultConnection": "Host=localhost;Port=5432;Database=BackToSchool;Username=postgres;Password=..." },
     "Jwt": { "Key": "<base64 de 48 bytes aleatorios>" }
   }
   ```

3. `dotnet run --project BackToSchool.Web` → http://localhost:5027 (Swagger en `/swagger`).

## Usuarios de prueba (cambiar antes de producción)

| Usuario | Contraseña | Rol | Qué ve |
|---|---|---|---|
| `promotor1`, `promotor2` | `promotor123` | Promotor | Nuevo pedido, sus pedidos |
| `almacen` | `almacen123` | Almacén | Cola de almacén, pedidos del día |
| `caja` | `caja123` | Caja | Pedidos del día (buscar ficha) — solo lectura |
| `admin` | `admin123` | Administrador | Todo lo anterior + precios, listas por curso, colegios |
| `mbos` | `mbos123` | M-BOS | Todo + usuarios y roles |

## Dominio

| Tabla | Qué guarda |
|---|---|
| `school` | Unidades educativas (solo informativo en el pedido) |
| `course` | Cursos: Pre Inicial … 6to Secundaria |
| `product` | Catálogo. `section`: TEXT, NOTEBOOK, UNIFORM, OTHER. Uniformes: un producto por talla (`garment_name` + `size`). Código opcional |
| `course_product` | Lista de textos de cada curso; `is_default_selected = false` = opcional (agenda) |
| `sale_order` / `sale_order_line` | Pedido con su número de ficha. Las líneas escritas a mano tienen `product_id` nulo |
| `ticket_counter` | Último número de ficha del día (se incrementa de forma atómica: no se repite aunque 6 promotores envíen a la vez) |

**Estados del pedido:** `PENDING` (nuevo) → `PRINTED` (almacén imprimió la hoja) → `READY` (listo en
caja; aquí baja el stock) · `CANCELLED`. El promotor puede anular su pedido mientras siga pendiente.
"Agregar algo más" para el mismo papá = **pedido nuevo con otra ficha** (decisión del cliente).

**Precios:** el precio de cada línea del catálogo lo pone siempre el servidor; el celular solo
manda qué productos y cuántos. Las líneas escritas a mano llevan el precio que dicta el promotor.

## Scripts

- `Scripts/tools/generate_catalog_seed.py` vuelve a generar `03_seed_catalog.sql` desde los Excel del
  cliente (levantamiento de inventario + formulario de pedido). Imprime el cruce de nombres para revisar.
- `Scripts/99_reset_pedidos_de_prueba.sql` borra todos los pedidos y reinicia las fichas (solo pruebas).

## Endpoints principales

| Método | Ruta | Quién |
|---|---|---|
| GET | `/api/Catalog/order-form` | Todo lo que necesita la pantalla del promotor, en una llamada |
| POST | `/api/SaleOrders` | Crear pedido → devuelve el número de ficha |
| GET | `/api/SaleOrders?ticketDate=&status=&ticketNumber=&mine=` | Pedidos del día |
| GET | `/api/SaleOrders/{id}/pdf` | Hoja de almacén (al imprimirla el pedido pasa a "En almacén") |
| PUT | `/api/SaleOrders/{id}/status` | Cambiar estado / anular |
| PUT | `/api/Catalog/products/prices` | Guardar varios precios a la vez |
| PUT | `/api/Catalog/courses/{id}/products` | Guardar la lista de un curso |

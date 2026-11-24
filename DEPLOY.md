# Despliegue de BackToSchool

BackToSchool está en el mismo servidor que InvenTrack, FundTrack, ClockMetrics y TransactionMatching
(`148.230.73.226`, Ubuntu, Docker) y se abre en **https://backtoschool.iasdmbos.cloud**
(registro A `backtoschool` → `148.230.73.226`).

## Cómo está armado

| Pieza | Dónde | Detalle |
|---|---|---|
| Frontend | contenedor `backtoschool-frontend` | Angular (PWA) servido por nginx; pasa `/api` al backend. Puerto local `127.0.0.1:5121`. |
| Backend | contenedor `backtoschool-backend` | .NET 10, zona horaria `America/La_Paz`. Puerto local `127.0.0.1:5120`. Red Docker `backtoschool-net`. |
| Base de datos | PostgreSQL 17 del servidor | Base `backtoschool`, usuario `backtoschool`. El contenedor entra por `host.docker.internal`. |
| Configuración | `/root/backtoschool-backend.env` | Cadena de conexión, clave JWT y CORS. Solo root la puede leer. **No va a Git.** Si se cambia, correr `deploy.sh`. |
| Entrada pública | nginx del servidor, `/etc/nginx/sites-available/backtoschool.conf` | HTTPS en el 443 (http redirige a https) y pasa todo a `127.0.0.1:5121`. |
| Certificado | Let's Encrypt, `/etc/letsencrypt/live/backtoschool.iasdmbos.cloud` | Lo renueva solo certbot. Vence el 22/02/2027 si no se renovara. |
| Código | `/opt/backtoschool/backend` y `/opt/backtoschool/frontend` | Subido directo (todavía no hay repositorio en GitHub). |

Puertos locales del servidor: InvenTrack 5080-5081, FundTrack 5090-5091, Música 5092,
ClockMetrics 5100-5101, TransactionMatching 5110-5111, **BackToSchool 5120-5121**.

## Primera instalación (ya hecha el 24/11/2026)

Con el código en `/opt/backtoschool/{backend,frontend}`, como root:

```bash
bash /opt/backtoschool/backend/setup-server.sh
```

Crea la base y su usuario, corre `01_schema.sql`, `02_seed_security.sql` y `03_seed_catalog.sql`, crea la
configuración, el sitio de nginx, los contenedores y el certificado. Se puede volver a correr sin romper nada.

## Actualizar a una nueva versión

1. Desde la PC, empaquetar y subir el código (sin `bin`, `obj`, `node_modules`, `dist`, `.angular` ni
   `appsettings.Development.json`) y descomprimirlo sobre `/opt/backtoschool/backend` y
   `/opt/backtoschool/frontend`. Cuando el código esté en GitHub, basta con `git clone` una vez en esas
   carpetas: `deploy.sh` ya hace `git pull` si encuentra un repositorio.
2. Si hay cambios de base de datos, primero un respaldo y después el script:

   ```bash
   sudo -u postgres pg_dump -Fc backtoschool > /root/backtoschool_antes_$(date +%Y%m%d_%H%M).dump
   export PGPASSWORD=$(grep ^ConnectionStrings /root/backtoschool-backend.env | sed 's/.*Password=//')
   psql -h 127.0.0.1 -U backtoschool -d backtoschool -v ON_ERROR_STOP=1 -f /opt/backtoschool/backend/Scripts/<script>.sql
   ```

   `01_schema.sql` NO se vuelve a correr (crea las tablas desde cero); los cambios de tablas van en un
   script nuevo. `02` y `03` se pueden volver a correr (no duplican).
3. `bash /opt/backtoschool/backend/deploy.sh` (construye las imágenes y cambia los contenedores).
4. Revisar:

   ```bash
   docker ps | grep backtoschool
   docker logs --tail 30 backtoschool-backend
   curl -s -o /dev/null -w "%{http_code}\n" https://backtoschool.iasdmbos.cloud/
   ```

   El aviso `Cannot load library libgssapi_krb5.so.2` en el log es normal (librería opcional de Kerberos).

## Antes de la venta real

- Cambiar las contraseñas de los usuarios de prueba (`02_seed_security.sql`) o desactivarlos, y crear
  los usuarios reales de cada promotor desde Administración → Usuarios.
- Borrar los pedidos de prueba: `Scripts/99_reset_pedidos_de_prueba.sql` (borra TODOS los pedidos).

## Respaldos

```bash
sudo -u postgres pg_dump -Fc backtoschool > /root/backtoschool_$(date +%Y%m%d_%H%M).dump
```

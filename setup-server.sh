#!/bin/bash
# Instala BackToSchool por PRIMERA VEZ en el servidor (148.230.73.226), con el mismo esquema que
# TransactionMatching, FundTrack y ClockMetrics. Para actualizar después se usa deploy.sh.
#
# Uso, como root en el servidor:  bash /opt/backtoschool/backend/setup-server.sh
#
# Requisitos: el código ya está en /opt/backtoschool/backend y /opt/backtoschool/frontend, y el
# registro DNS A "backtoschool" → 148.230.73.226 ya existe.
# Se puede volver a correr: lo que ya existe (base, configuración, nginx, certificado) se salta.
set -euo pipefail

DOMAIN=backtoschool.iasdmbos.cloud
ROOT=/opt/backtoschool
ENV_FILE=/root/backtoschool-backend.env
DB=backtoschool
DB_USER=backtoschool
NGINX_SITE=/etc/nginx/sites-available/backtoschool.conf
FRONTEND_PORT=5121

if [ "$(id -u)" -ne 0 ]; then echo "Correr como root."; exit 1; fi
[ -d "$ROOT/backend" ] && [ -d "$ROOT/frontend" ] || { echo "Falta el código en $ROOT/backend y $ROOT/frontend."; exit 1; }

echo "== 1. Base de datos $DB"
if [ -f "$ENV_FILE" ]; then
  DB_PASS=$(grep ^ConnectionStrings "$ENV_FILE" | sed 's/.*Password=//')
  echo "   ya hay configuración ($ENV_FILE): se usa su contraseña"
else
  DB_PASS=$(openssl rand -hex 24)
fi

if sudo -u postgres psql -tAc "SELECT 1 FROM pg_roles WHERE rolname='$DB_USER'" | grep -q 1; then
  echo "   el usuario $DB_USER ya existe"
  sudo -u postgres psql -q -c "ALTER USER $DB_USER WITH PASSWORD '$DB_PASS';"
else
  sudo -u postgres psql -q -v ON_ERROR_STOP=1 -c "CREATE USER $DB_USER WITH PASSWORD '$DB_PASS';"
fi

if sudo -u postgres psql -tAc "SELECT 1 FROM pg_database WHERE datname='$DB'" | grep -q 1; then
  echo "   la base $DB ya existe: no se vuelven a crear las tablas"
  NEW_DB=0
else
  sudo -u postgres psql -q -v ON_ERROR_STOP=1 -c "CREATE DATABASE $DB OWNER $DB_USER ENCODING 'UTF8' TEMPLATE template0;"
  NEW_DB=1
fi

# Los contenedores entran por host.docker.internal desde una red de Docker (172.16.0.0/12).
HBA=$(sudo -u postgres psql -tAc "SHOW hba_file")
if ! grep -qE "^host\s+$DB\s+$DB_USER\s+172\.16\.0\.0/12" "$HBA"; then
  echo "host    $DB    $DB_USER    172.16.0.0/12    scram-sha-256" >> "$HBA"
  systemctl reload postgresql
  echo "   permiso agregado en $HBA para las redes de Docker"
fi

if [ "$NEW_DB" = 1 ]; then
  export PGPASSWORD="$DB_PASS"
  PSQL="psql -h 127.0.0.1 -U $DB_USER -d $DB -v ON_ERROR_STOP=1 -q"
  $PSQL -f "$ROOT/backend/Scripts/01_schema.sql"
  $PSQL -f "$ROOT/backend/Scripts/02_seed_security.sql"
  $PSQL -f "$ROOT/backend/Scripts/03_seed_catalog.sql"
  unset PGPASSWORD
  echo "   tablas, usuarios, menú y catálogo cargados"
fi

echo "== 2. Configuración del backend ($ENV_FILE)"
if [ ! -f "$ENV_FILE" ]; then
  JWT_KEY=$(openssl rand -base64 64 | tr -d '\n')
  umask 077
  cat > "$ENV_FILE" <<EOF
ConnectionStrings__DefaultConnection=Host=host.docker.internal;Port=5432;Database=$DB;Username=$DB_USER;Password=$DB_PASS
Jwt__Key=$JWT_KEY
Cors__AllowedOrigins__0=https://$DOMAIN
EOF
  chmod 600 "$ENV_FILE"
  echo "   creada (solo root la puede leer)"
else
  echo "   ya existe: no se toca"
fi

echo "== 3. nginx ($DOMAIN → 127.0.0.1:$FRONTEND_PORT)"
if [ ! -f "$NGINX_SITE" ]; then
  cat > "$NGINX_SITE" <<EOF
server {
    listen 80;
    server_name $DOMAIN;

    location / {
        proxy_pass http://127.0.0.1:$FRONTEND_PORT;
        proxy_set_header Host \$host;
        proxy_set_header X-Real-IP \$remote_addr;
        proxy_set_header X-Forwarded-For \$proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto \$scheme;
        proxy_read_timeout 120s;
    }
}
EOF
  ln -sf "$NGINX_SITE" /etc/nginx/sites-enabled/backtoschool.conf
  nginx -t
  systemctl reload nginx
  echo "   sitio creado"
else
  echo "   ya existe: no se toca"
fi

echo "== 4. Contenedores (deploy.sh)"
# deploy.sh revisa el sitio por https al final, que todavía puede no tener certificado.
bash "$ROOT/backend/deploy.sh" || true

echo "== 5. Certificado HTTPS"
if [ ! -d "/etc/letsencrypt/live/$DOMAIN" ]; then
  certbot --nginx -d "$DOMAIN" --non-interactive --agree-tos --redirect --register-unsafely-without-email || \
    certbot --nginx -d "$DOMAIN" --non-interactive --redirect
else
  echo "   ya existe"
fi

echo "== 6. Revisión"
sleep 3
docker ps --format "   {{.Names}}: {{.Status}}" | grep backtoschool || true
echo "   sitio: $(curl -s -o /dev/null -w "%{http_code}" https://$DOMAIN/)"
echo "   api:   $(curl -s -o /dev/null -w "%{http_code}" -X POST https://$DOMAIN/api/Auth/login -H 'Content-Type: application/json' -d '{"usernameOrEmail":"x","password":"x"}') (401 o 400 = el backend responde)"
echo
echo "Listo: https://$DOMAIN"

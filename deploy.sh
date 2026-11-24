#!/bin/bash
# Construye y cambia los contenedores de BackToSchool con el código que está en
# /opt/backtoschool/backend y /opt/backtoschool/frontend.
# Uso, en el servidor:  bash /opt/backtoschool/backend/deploy.sh
# No aplica cambios de base de datos: esos se corren antes, a mano (ver DEPLOY.md).
set -euo pipefail

ROOT=/opt/backtoschool
DOMAIN=backtoschool.iasdmbos.cloud

# Cuando el código esté en GitHub, se baja lo último antes de construir.
for part in backend frontend; do
  if [ -d "$ROOT/$part/.git" ]; then
    git -C "$ROOT/$part" pull --ff-only
    echo "   $part: $(git -C "$ROOT/$part" log --oneline -1)"
  fi
done

echo "== Construyendo las imágenes (la versión actual sigue funcionando mientras tanto)"
docker build -q -t backtoschool-backend:latest "$ROOT/backend" > /dev/null
docker build -q -t backtoschool-frontend:latest "$ROOT/frontend" > /dev/null

echo "== Cambiando los contenedores"
docker network inspect backtoschool-net > /dev/null 2>&1 || docker network create backtoschool-net > /dev/null
docker rm -f backtoschool-backend backtoschool-frontend > /dev/null 2>&1 || true

# El nombre "backtoschool-backend" es el que usa el nginx del frontend para llegar a /api.
docker run -d --name backtoschool-backend --network backtoschool-net --restart unless-stopped \
  --env-file /root/backtoschool-backend.env --add-host host.docker.internal:host-gateway \
  -e TZ=America/La_Paz \
  -p 127.0.0.1:5120:8080 backtoschool-backend:latest > /dev/null

docker run -d --name backtoschool-frontend --network backtoschool-net --restart unless-stopped \
  -p 127.0.0.1:5121:80 backtoschool-frontend:latest > /dev/null

docker image prune -f > /dev/null

echo "== Revisando"
sleep 8
docker ps --format "   {{.Names}}: {{.Status}}" | grep backtoschool
echo "   sitio: $(curl -s -o /dev/null -w "%{http_code}" https://$DOMAIN/)"
echo "Listo: https://$DOMAIN"

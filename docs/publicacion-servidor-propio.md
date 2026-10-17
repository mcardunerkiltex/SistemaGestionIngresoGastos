# Publicar en un servidor propio

Cada vez que algo entra a `main` y el CI pasa, GitHub construye la imagen, la sube a GitHub Container Registry
y por SSH actualiza el servidor. En el servidor corren tres contenedores con Docker Compose:

| Contenedor | Qué hace |
| --- | --- |
| `caddy` | Recibe el tráfico en 80/443 y saca solo el certificado HTTPS (Let's Encrypt). |
| `app` | La API .NET que también sirve la web. Aplica las migraciones al arrancar. |
| `db` | PostgreSQL con los datos en un volumen. |

Los archivos están en [`deploy/`](../deploy) y el workflow en [`.github/workflows/deploy.yml`](../.github/workflows/deploy.yml).

## Qué necesitás

- **Un servidor Linux** con IP pública: un VPS con Ubuntu 24.04, 1 vCPU y 1 GB de RAM alcanza
  (Hetzner, DigitalOcean, Contabo, Vultr, desde unos USD 5 por mes). También sirve una máquina propia si tenés IP
  fija o redirigís los puertos 80 y 443 del router.
- **Un dominio** (o subdominio) con un registro **A** que apunte a la IP del servidor, por ejemplo
  `gastos.tudominio.com`. Es necesario para el certificado HTTPS, y la app como PWA necesita HTTPS.

## 1. Preparar el servidor (una sola vez)

Entrá por SSH como root y ejecutá:

```bash
# Docker y el plugin compose
curl -fsSL https://get.docker.com | sh

# Usuario para los deploys, con permiso para usar Docker
adduser --disabled-password --gecos "" deploy
usermod -aG docker deploy
mkdir -p /opt/gastos && chown deploy:deploy /opt/gastos

# Firewall: solo SSH y web
ufw allow OpenSSH && ufw allow 80 && ufw allow 443/tcp && ufw allow 443/udp && ufw --force enable
```

## 2. Clave SSH para GitHub

En tu compu (no en el servidor) generá una clave solo para los deploys:

```bash
ssh-keygen -t ed25519 -f gastos_deploy -N "" -C "github-deploy"
```

Copiá la clave pública al servidor:

```bash
ssh root@IP_DEL_SERVIDOR "mkdir -p /home/deploy/.ssh && cat >> /home/deploy/.ssh/authorized_keys && chown -R deploy:deploy /home/deploy/.ssh && chmod 600 /home/deploy/.ssh/authorized_keys" < gastos_deploy.pub
```

Y sacá la huella del servidor (para que GitHub sepa que se conecta al servidor correcto):

```bash
ssh-keyscan -H IP_DEL_SERVIDOR
```

## 3. Variables y secretos en GitHub

En el repo → **Settings → Secrets and variables → Actions**:

| Tipo | Nombre | Valor |
| --- | --- | --- |
| Variable | `DEPLOY_HOST` | IP o nombre del servidor. **Mientras no exista, el deploy no corre.** |
| Variable | `DEPLOY_USER` | `deploy` (opcional, es el valor por defecto) |
| Variable | `DEPLOY_DIR` | `/opt/gastos` (opcional, es el valor por defecto) |
| Secret | `DEPLOY_SSH_KEY` | El contenido completo de `gastos_deploy` (la clave **privada**) |
| Secret | `DEPLOY_KNOWN_HOSTS` | La salida de `ssh-keyscan` del paso 2 |

El workflow usa el entorno `produccion`. Si querés aprobar cada deploy a mano, en **Settings → Environments →
produccion** activá **Required reviewers**.

## 4. Configuración de la app en el servidor

Como usuario `deploy`, creá `/opt/gastos/.env` a partir de [`deploy/.env.example`](../deploy/.env.example):

```bash
sudo -iu deploy
cat > /opt/gastos/.env <<EOT
DOMAIN=gastos.tudominio.com
APP_IMAGE=ghcr.io/mcardunerkiltex/sistemagestioningresogastos:latest
POSTGRES_PASSWORD=$(openssl rand -hex 24)
JWT_SIGNING_KEY=$(openssl rand -base64 48)
GOOGLE_CLIENT_ID=
EOT
chmod 600 /opt/gastos/.env
```

Guardá una copia de esas claves en tu gestor de contraseñas: sin `POSTGRES_PASSWORD` no se puede abrir la base.

## 5. Primer deploy

En GitHub → **Actions → Deploy → Run workflow**. El workflow construye la imagen, copia `docker-compose.yml`,
`Caddyfile` y `backup.sh` al servidor, levanta todo y espera a que `https://TU_DOMINIO/health` responda.

Desde ahí, cada merge a `main` con el CI en verde se publica solo.

## 6. Copias de la base

```bash
sudo -iu deploy
crontab -e
# agregar:
0 3 * * * /opt/gastos/backup.sh
```

Deja una copia por día en `/opt/gastos/backups` y borra las de más de 14 días. Conviene bajarlas de vez en cuando
a otro lado (por ejemplo con `scp` o `rclone` a un Drive).

Para restaurar una copia:

```bash
cd /opt/gastos
docker compose exec -T db pg_restore -U gastos -d gastos --clean < backups/gastos-AAAA-MM-DD.dump
```

## Comandos útiles en el servidor

```bash
cd /opt/gastos
docker compose ps                 # estado
docker compose logs -f app        # logs de la app
docker compose restart app        # reiniciar la app
docker compose pull app && docker compose up -d   # actualizar a mano
```

## Volver a una versión anterior

Cada imagen se publica también con el hash del commit. En `.env` cambiá `APP_IMAGE` a
`ghcr.io/mcardunerkiltex/sistemagestioningresogastos:<hash>` y ejecutá `docker compose up -d`.
Ojo: si esa versión es anterior a una migración de base, puede no funcionar.

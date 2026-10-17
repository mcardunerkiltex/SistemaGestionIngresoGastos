# Publicar en Railway

La app se publica como **un solo servicio**: la imagen del `Dockerfile` de la raíz compila la web Angular y la
sirve desde la misma API .NET. Así web y API comparten dominio (la cookie de sesión funciona sin CORS) y en
Railway solo hay dos piezas: la app y la base PostgreSQL.

Costo aproximado: plan Hobby de Railway, USD 5 por mes, que incluye el uso de una app chica como esta.

## 1. Crear el proyecto

1. Entrá a [railway.com](https://railway.com) con tu cuenta de GitHub y elegí el plan **Hobby**.
2. **New Project → Deploy from GitHub repo** y elegí `SistemaGestionIngresoGastos`.
   Railway lee `railway.json`, construye con el `Dockerfile` y usa `/health` para saber si arrancó bien.
   El primer deploy va a fallar porque todavía falta la base: es normal.
3. En el mismo proyecto: **+ Create → Database → PostgreSQL**.

## 2. Variables de la app

En el servicio de la app → **Variables**, agregá:

| Variable | Valor |
| --- | --- |
| `DATABASE_URL` | `${{Postgres.DATABASE_URL}}` (Railway la completa con la base del proyecto) |
| `Jwt__SigningKey` | Una clave larga y secreta. Generala con `openssl rand -base64 48` |
| `Google__ClientId` | Opcional: el Client ID de Google para el botón "Continuar con Google" |

Al guardar, Railway vuelve a desplegar. La API aplica las migraciones sola al arrancar
(`Database__MigrateOnStartup=true` ya viene en la imagen), así que la base queda creada en el primer arranque.

## 3. Dominio

Servicio de la app → **Settings → Networking → Generate Domain**. Queda algo como
`https://sistemagestion-production.up.railway.app`. Abrilo desde el celular y en el menú del navegador elegí
**Agregar a pantalla de inicio** para usarla como app.

Si querés un dominio propio, en la misma pantalla está **Custom Domain**.

## 4. Login con Google (opcional)

En [Google Cloud Console](https://console.cloud.google.com/apis/credentials), en tu Client ID de tipo
"Aplicación web", agregá el dominio de Railway en **Orígenes de JavaScript autorizados**. Después cargá
`Google__ClientId` en las variables (paso 2).

## 5. Actualizaciones

Cada merge a `main` despliega solo. Para que no despliegue si el CI falla, activá
**Settings → Deploy → Wait for CI** en el servicio de la app.

## Cosas a tener en cuenta

- **Copias de la base:** en el servicio de Postgres → **Backups** podés programar copias automáticas.
- **Logs:** servicio de la app → **Deployments → View logs**.
- **Probar la imagen localmente:**

  ```bash
  docker build -t gastos .
  docker run --rm -p 8080:8080 \
    -e DATABASE_URL=postgres://gastos:gastos@host.docker.internal:5432/gastos \
    -e Jwt__SigningKey=una-clave-de-prueba-de-al-menos-32-caracteres \
    gastos
  ```

# Poängjakten

Mobil webbapp för en 50-årsfest. Den första versionen består av en ASP.NET Core-backend som även serverar frontend-filerna.

## Endpoints

- `/` – mobilanpassad frontend
- `/api/hello` – enkelt API-anrop som visas i frontend
- `/health` – health check för Azure App Service

## Azure

Applikationen är avsedd för en liten Linux App Service-plan och ett Storage Account med Table Storage och Blob Storage. GitHub Actions loggar in i Azure via OIDC, så inget lösenord eller publish profile behöver lagras i GitHub.

Deployment sker automatiskt när kod pushas till `main`. Azure-inloggningen använder en
OIDC-credential som är begränsad till repot och branchen `main`. Azure-ID:n lagras som
repository secrets och App Service-namnet som en repository variable i GitHub Actions.

# Poängjakten

Mobil webbapp för en 50-årsfest. Den första versionen består av en ASP.NET Core-backend som även serverar frontend-filerna.

Deltagarregistret laddas från Azure Table Storage när processen startar. Därefter sker
alla läsningar från minnet medan nya och ändrade deltagare skrivs igenom till Table
Storage. Persistens, domänlogik och HTTP-endpoints är separerade så att delarna kan
utvecklas eller bytas utan att resten av appen behöver skrivas om.

## Endpoints

- `/` – mobilanpassad frontend
- `/api/hello` – enkelt API-anrop som visas i frontend
- `POST /api/participants/register` – registrerar eller återupptar en deltagare via namn
- `GET /api/participants/{id}` – hämtar en deltagare från minnesregistret
- `GET /api/participants` – listar deltagarna i poängordning
- `GET /api/admin/participants` – listar deltagare för administration (testläge)
- `DELETE /api/admin/participants/{id}` – tar bort en deltagare ur minne och lagring (testläge)
- `/api/admin-session` – skapar, kontrollerar och avslutar en serverlagrad adminsession
- `GET /api/challenges` – listar aktiva poänguppgifter för deltagarvyn
- `/api/admin/challenges` – skapa, ändra och radera poänguppgifter som admin

Det vanliga namnfältet används även för admininloggning. Om värdet matchar
`Admin__Secret` startas en adminsession och `Admin__DisplayName` visas; annars
registreras en vanlig deltagare.

Poänguppgifterna ligger i en egen `challenges`-tabell. De laddas till minnet vid
uppstart och alla adminändringar skrivs igenom till Table Storage innan minnet ändras.
- `/health` – health check för Azure App Service
- `POST /health/storage` – skriver, läser och raderar testdata i Table och Blob Storage

## Azure

Applikationen är avsedd för en liten Linux App Service-plan och ett Storage Account med Table Storage och Blob Storage. GitHub Actions loggar in i Azure via OIDC, så inget lösenord eller publish profile behöver lagras i GitHub.

Deployment sker automatiskt när kod pushas till `main`. Azure-inloggningen använder en
OIDC-credential som är begränsad till repot och branchen `main`. Azure-ID:n lagras som
repository secrets och App Service-namnet som en repository variable i GitHub Actions.

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
- `GET /api/photos` – listar bildmetadata från minnet
- `POST /api/photos` – laddar upp en klientkomprimerad visningsbild och tumnagel
- `GET /api/photos/{id}/image` – hämtar visningsbilden från den privata blobcontainern
- `GET /api/photos/{id}/thumbnail` – hämtar tumnageln
- `DELETE /api/admin/photos/{id}` – raderar bild, tumnagel och metadata som admin
- `/health` – health check för Azure App Service
- `POST /health/storage` – skriver, läser och raderar testdata i Table och Blob Storage

Det vanliga namnfältet används även för admininloggning. Om värdet matchar
`Admin__Secret` startas en adminsession och `Admin__DisplayName` visas; annars
registreras en vanlig deltagare.

Poänguppgifterna ligger i en egen `challenges`-tabell. De laddas till minnet vid
uppstart och alla adminändringar skrivs igenom till Table Storage innan minnet ändras.

Varje deltagares utförda uppgifter ligger i `challengecompletions`, partitionerade per
deltagare. Markeringar kan sättas och tas bort fritt. Totalpoängen beräknas från de
aktuella uppgiftspoängen, så en adminändring slår igenom utan migrering av lagrade summor.

Bilder komprimeras i webbläsaren till högst 2048 pixlar på längsta sidan och JPEG-kvalitet
84 %. En separat tumnagel på högst 480 pixlar skapas för galleriet. Båda sparas i den
privata blobcontainern, medan fotograf, blobnamn och uppladdningstid ligger i tabellen
`photos` och laddas till minnet när appen startar.

## Azure

Applikationen är avsedd för en liten Linux App Service-plan och ett Storage Account med Table Storage och Blob Storage. GitHub Actions loggar in i Azure via OIDC, så inget lösenord eller publish profile behöver lagras i GitHub.

Deployment sker automatiskt när kod pushas till `main`. Azure-inloggningen använder en
OIDC-credential som är begränsad till repot och branchen `main`. Azure-ID:n lagras som
repository secrets och App Service-namnet som en repository variable i GitHub Actions.

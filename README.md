# Poängjakten

Mobil webbapp för en 50-årsfest. Den första versionen består av en ASP.NET Core-backend som även serverar frontend-filerna.

Deltagarregistret laddas från Azure Table Storage när processen startar. Därefter sker
alla läsningar från minnet medan nya och ändrade deltagare skrivs igenom till Table
Storage. Persistens, domänlogik och HTTP-endpoints är separerade så att delarna kan
utvecklas eller bytas utan att resten av appen behöver skrivas om.

## Endpoints

- `/` – mobilanpassad frontend
- `/api/hello` – enkelt API-anrop som visas i frontend
- `POST /api/participants/login` – loggar in en förregistrerad deltagare med kod
- `GET /api/participants/{id}` – hämtar en deltagare från minnesregistret
- `GET /api/participants` – listar deltagarna i poängordning
- `GET /api/participants/{id}/challenge-summary` – listar deltagarens synliga utförda uppgifter
- `DELETE /api/participants/{participantId}/photos/{id}` – låter deltagaren radera en egen bild
- `GET /api/admin/participants` – listar deltagare för administration (testläge)
- `DELETE /api/admin/participants/{id}` – tar bort en deltagare ur minne och lagring (testläge)
- `/api/admin-session` – skapar, kontrollerar och avslutar en serverlagrad adminsession
- `GET /api/challenges` – listar aktiva poänguppgifter för deltagarvyn
- `GET /api/participants/{id}/table-leaderboard/{tableId}/challenge-summary` – listar bordets synliga utförda uppgifter
- `/api/admin/challenges` – skapa, ändra och radera poänguppgifter som admin
- `GET /api/photos` – listar bildmetadata från minnet
- `POST /api/photos` – laddar upp en klientkomprimerad visningsbild och tumnagel
- `GET /api/photos/{id}/image` – hämtar visningsbilden från den privata blobcontainern
- `GET /api/photos/{id}/thumbnail` – hämtar tumnageln
- `DELETE /api/admin/photos/{id}` – raderar bild, tumnagel och metadata som admin
- `GET /api/songs` – listar sånger från minnet i visningsordning
- `GET /api/songs/{id}/image` – hämtar en valfri sångbild från blobcontainern
- `/api/admin/songs` – skapa, ändra, sortera och radera sånger samt hantera bilder
- `/api/participants/{id}/song-requests` – lista, skapa och radera bordets låtönskemål
- `/api/admin/song-requests` – lista och radera alla låtönskemål som admin
- `/health` – health check för Azure App Service
- `POST /health/storage` – skriver, läser och raderar testdata i Table och Blob Storage

Det vanliga kodfältet används även för admininloggning. Om värdet matchar
`Admin__Secret` startas en adminsession och `Admin__DisplayName` visas; annars
loggas en förregistrerad deltagare in.

En deltagare kan sakna både ledtråd och bord. Då döljs de bordsbundna funktionerna,
men deltagaren kan fortfarande använda den individuella poängjakten, bilder, sånger
och Låtlista. Ett låtönskemål från en bordslös deltagare ägs av personen i stället
för ett bord.

Poänguppgifterna ligger i en egen `challenges`-tabell. De laddas till minnet vid
uppstart och alla adminändringar skrivs igenom till Table Storage innan minnet ändras.
En uppgift kan kopplas till ett feststeg via `UnlockStageId`; då filtreras den bort från
deltagarvyer och poängberäkning tills administratören låser upp steget.

Fasta specialfrågor ligger utanför det administrerbara uppgiftsregistret. Svaret på
`Hur många % Daniel är du?` lagras per deltagare i tabellen `specialanswers` och ger
heltalsdelen av procentsvaret delat med tio i poäng. Frågan och dess poäng aktiveras
av feststeget `Efter 100% Daniel`.

Varje deltagares utförda uppgifter ligger i `challengecompletions`, partitionerade per
deltagare. Markeringar kan sättas och tas bort fritt. Totalpoängen beräknas från de
aktuella uppgiftspoängen, så en adminändring slår igenom utan migrering av lagrade summor.
Samma minnesregister används för att visa hur många deltagare eller bord som har klarat
varje uppgift och för de utfällbara uppgiftslistorna i topplistorna. Uppgifter som tillhör
ett låst feststeg exponeras inte i dessa listor.

Bilder komprimeras i webbläsaren till högst 2048 pixlar på längsta sidan och JPEG-kvalitet
84 %. En separat tumnagel på högst 480 pixlar skapas för galleriet. Båda sparas i den
privata blobcontainern, medan fotograf, blobnamn och uppladdningstid ligger i tabellen
`photos` och laddas till minnet när appen startar.

Sångernas titel, melodi, text och visningsordning lagras i Table Storage-tabellen
`songs` och läses till minnet vid uppstart. Valfria illustrationer komprimeras i
webbläsaren och sparas under `songs/` i den privata blobcontainern. Källdokument som
PDF eller PowerPoint ingår inte i applikationsrepot.

Låtönskemål är en separat funktion från sånghäftet. Artist, låt, bord och skapare
lagras i tabellen `songrequests` och laddas till minnet vid uppstart. Deltagarna ser
hela kvällens lista, medan det egna bordets önskemål markeras och kan tas bort av
vilken bordskamrat som helst. Funktionen låses upp tillsammans med bordsplaceringen.

## Azure

Applikationen är avsedd för en liten Linux App Service-plan och ett Storage Account med Table Storage och Blob Storage. GitHub Actions loggar in i Azure via OIDC, så inget lösenord eller publish profile behöver lagras i GitHub.

Deployment sker automatiskt när kod pushas till `main`. Azure-inloggningen använder en
OIDC-credential som är begränsad till repot och branchen `main`. Azure-ID:n lagras som
repository secrets och App Service-namnet som en repository variable i GitHub Actions.

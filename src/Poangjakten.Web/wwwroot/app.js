const message = document.querySelector("#message");
const storageMessage = document.querySelector("#storage-message");
const tableStatus = document.querySelector("#table-status");
const blobStatus = document.querySelector("#blob-status");
const testStorageButton = document.querySelector("#test-storage");

async function loadGreeting() {
  try {
    const response = await fetch("/api/hello");
    if (!response.ok) throw new Error(`HTTP ${response.status}`);

    const result = await response.json();
    message.textContent = result.message;
  } catch {
    message.textContent = "Kunde inte nå servern.";
    message.classList.add("error");
  }
}

loadGreeting();

async function testStorage() {
  testStorageButton.disabled = true;
  storageMessage.textContent = "Testar skrivning, läsning och radering…";
  tableStatus.textContent = "◌ Table Storage";
  blobStatus.textContent = "◌ Blob Storage";

  try {
    const response = await fetch("/health/storage", { method: "POST" });
    const result = await response.json();

    tableStatus.textContent = `${result.tableStorage ? "✓" : "✕"} Table Storage`;
    blobStatus.textContent = `${result.blobStorage ? "✓" : "✕"} Blob Storage`;
    storageMessage.textContent = result.isHealthy
      ? "All lagring fungerar"
      : result.error || "Lagringstestet misslyckades";
  } catch {
    storageMessage.textContent = "Kunde inte köra lagringstestet.";
    tableStatus.textContent = "? Table Storage";
    blobStatus.textContent = "? Blob Storage";
  } finally {
    testStorageButton.disabled = false;
  }
}

testStorageButton.addEventListener("click", testStorage);

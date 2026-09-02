const message = document.querySelector("#message");

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


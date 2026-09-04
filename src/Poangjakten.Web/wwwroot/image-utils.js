function validateImage(file) {
  if (!file.type.startsWith("image/")) {
    throw new Error(`${file.name} är inte en bild.`);
  }
  if (file.size > 40 * 1024 * 1024) {
    throw new Error(`${file.name} är större än 40 MB.`);
  }
}

export async function compressPhoto(file) {
  validateImage(file);
  let decoded;
  try {
    decoded = await decodeImage(file);
    return {
      image: await renderJpeg(decoded.source, decoded.width, decoded.height, 2048, 0.84),
      thumbnail: await renderJpeg(decoded.source, decoded.width, decoded.height, 480, 0.74)
    };
  } catch {
    throw unreadableImageError(file);
  } finally {
    decoded?.dispose();
  }
}

export async function compressImage(file, maxEdge = 1600, quality = 0.86) {
  validateImage(file);
  let decoded;
  try {
    decoded = await decodeImage(file);
    return await renderJpeg(decoded.source, decoded.width, decoded.height, maxEdge, quality);
  } catch {
    throw unreadableImageError(file);
  } finally {
    decoded?.dispose();
  }
}

function unreadableImageError(file) {
  return new Error(`${file.name} kunde inte läsas. Prova JPEG, PNG eller HEIC från telefonens bildväljare.`);
}

async function decodeImage(file) {
  if ("createImageBitmap" in window) {
    try {
      const bitmap = await createImageBitmap(file, { imageOrientation: "from-image" });
      return { source: bitmap, width: bitmap.width, height: bitmap.height, dispose: () => bitmap.close() };
    } catch {
      // Fall back to the browser's image element decoder below.
    }
  }

  const url = URL.createObjectURL(file);
  const image = new Image();
  try {
    image.src = url;
    await image.decode();
    return {
      source: image,
      width: image.naturalWidth,
      height: image.naturalHeight,
      dispose: () => URL.revokeObjectURL(url)
    };
  } catch (error) {
    URL.revokeObjectURL(url);
    throw error;
  }
}

async function renderJpeg(source, sourceWidth, sourceHeight, maxEdge, quality) {
  const scale = Math.min(1, maxEdge / Math.max(sourceWidth, sourceHeight));
  const width = Math.max(1, Math.round(sourceWidth * scale));
  const height = Math.max(1, Math.round(sourceHeight * scale));
  const canvas = document.createElement("canvas");
  canvas.width = width;
  canvas.height = height;
  const context = canvas.getContext("2d", { alpha: false });
  context.fillStyle = "#fff";
  context.fillRect(0, 0, width, height);
  context.drawImage(source, 0, 0, width, height);

  return new Promise((resolve, reject) => {
    canvas.toBlob(
      blob => blob ? resolve(blob) : reject(new Error("Bilden kunde inte komprimeras.")),
      "image/jpeg",
      quality);
  });
}

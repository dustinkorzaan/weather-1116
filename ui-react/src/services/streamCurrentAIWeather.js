function resolveApiBaseUrl() {
  const configuredBaseUrl = import.meta.env.VITE_API_DOTNET_URL?.replace(/\/$/, '');
  if (configuredBaseUrl) {
    return configuredBaseUrl;
  }

  if (typeof window !== 'undefined' && window.location?.origin) {
    return window.location.origin;
  }

  return '';
}

function parseSseChunk(chunk, onUpdate) {
  const lines = chunk.split('\n');
  for (const line of lines) {
    if (!line.startsWith('data: ')) {
      continue;
    }

    const update = JSON.parse(line.slice(6));
    onUpdate(update);

    if (update.type === 'error') {
      throw new Error(update.message || 'Unable to load AI weather.');
    }
  }
}

export async function streamCurrentAIWeather(location, { onUpdate, signal }) {
  const baseUrl = resolveApiBaseUrl();
  const url = `${baseUrl}/AIWeather/Current/stream?location=${encodeURIComponent(location || 'Nashville, TN')}`;
  const response = await fetch(url, {
    signal,
    headers: { Accept: 'text/event-stream' },
  });

  if (!response.ok || !response.body) {
    throw new Error('Unable to load AI weather.');
  }

  const reader = response.body.getReader();
  const decoder = new TextDecoder();
  let buffer = '';

  while (true) {
    const { done, value } = await reader.read();
    if (done) {
      break;
    }

    buffer += decoder.decode(value, { stream: true });
    const parts = buffer.split('\n\n');
    buffer = parts.pop() ?? '';

    for (const part of parts) {
      if (part.trim()) {
        parseSseChunk(part, onUpdate);
      }
    }
  }

  if (buffer.trim()) {
    parseSseChunk(buffer, onUpdate);
  }
}

import { resolveApiBaseUrl } from '../services/apiBaseUrl';

export async function streamChatMessage({ endpoint, sessionId, message, gates, onEvent }) {
  const body = gates
    ? {
        sessionId,
        message,
        enableMaxLengthGate: gates.maxLength,
        enableRuleInputGate: gates.ruleInput,
        enableLlmInputGate: gates.llmInput,
        enableSystemPromptGuard: gates.systemPrompt,
        enableLlmOutputGate: gates.llmOutput,
      }
    : { sessionId, message };

  const response = await fetch(`${resolveApiBaseUrl()}${endpoint}`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  });

  if (!response.ok || !response.body) {
    throw new Error(`Chat request failed (${response.status})`);
  }

  const reader = response.body.getReader();
  const decoder = new TextDecoder();
  let buffer = '';

  while (true) {
    const { done, value } = await reader.read();
    if (done) break;

    buffer += decoder.decode(value, { stream: true });
    const parts = buffer.split('\n\n');
    buffer = parts.pop() ?? '';

    for (const part of parts) {
      const line = part.trim();
      if (!line.startsWith('data:')) continue;
      onEvent(JSON.parse(line.slice(5).trim()));
    }
  }
}
